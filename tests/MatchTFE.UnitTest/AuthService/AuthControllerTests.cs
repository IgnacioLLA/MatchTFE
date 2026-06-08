using AuthService.Controllers;
using AuthService.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TFELibrary.Shared;

namespace MatchTFE.UnitTest.AuthService;

[TestClass]
public class AuthControllerTests
{
    private Mock<IAuthService> _serviceMock = null!;
    private AuthController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _serviceMock = new Mock<IAuthService>();
        _serviceMock.Setup(s => s.TokenLifetime).Returns(15);
        _controller = new AuthController(_serviceMock.Object, Mock.Of<ILogger<AuthController>>());
        SetNoUserClaims();
    }

    private void SetUserClaims(string userId)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };
    }

    private void SetNoUserClaims()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };
    }

    private void SetCookies(string accessToken, string refreshToken)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append("Cookie", $"AccessToken={accessToken}; RefreshToken={refreshToken}");
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    private static string CreateTestJwt(string userId)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-jwt-secret-at-least-32-chars-ok!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        return handler.WriteToken(handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Sub, userId) }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = creds
        }));
    }

    // -------------------------------------------------------------------------
    // Login
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Login_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        _controller.ModelState.AddModelError("Email", "Required");

        var result = await _controller.Login(new LoginRequestDto());

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task Login_WhenCredentialsInvalid_ReturnsUnauthorized()
    {
        _serviceMock.Setup(s => s.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync((new LoginResponseDto { Error = new OperationResult(false, "Invalid credentials.") }, null));

        var result = await _controller.Login(new LoginRequestDto { Email = "x@x.com", Password = "wrong" });

        Assert.IsInstanceOfType(result, typeof(UnauthorizedObjectResult));
    }

    [TestMethod]
    public async Task Login_WhenCredentialsValid_ReturnsOk()
    {
        _serviceMock.Setup(s => s.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync((new LoginResponseDto
            {
                Error = new OperationResult(true, string.Empty),
                FirstName = "Test",
                LastName = "User"
            }, null));

        var result = await _controller.Login(new LoginRequestDto { Email = "x@x.com", Password = "pass" });

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    // -------------------------------------------------------------------------
    // Register
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Register_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        _controller.ModelState.AddModelError("Email", "Required");

        var result = await _controller.Register(new RegisterRequestDto());

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task Register_WhenDuplicateEmail_ReturnsConflict()
    {
        _serviceMock.Setup(s => s.RegisterAsync(It.IsAny<RegisterRequestDto>()))
            .ReturnsAsync((new RegisterResponseDto
            {
                Error = new OperationResult(false, "Email already taken.", "DuplicateEmail")
            }, null));

        var result = await _controller.Register(new RegisterRequestDto { Email = "dup@test.com" });

        Assert.IsInstanceOfType(result, typeof(ConflictObjectResult));
    }

    [TestMethod]
    public async Task Register_WhenGenericError_ReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.RegisterAsync(It.IsAny<RegisterRequestDto>()))
            .ReturnsAsync((new RegisterResponseDto
            {
                Error = new OperationResult(false, "Could not create profile.")
            }, null));

        var result = await _controller.Register(new RegisterRequestDto { Email = "x@x.com" });

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task Register_WhenSuccess_ReturnsOk()
    {
        _serviceMock.Setup(s => s.RegisterAsync(It.IsAny<RegisterRequestDto>()))
            .ReturnsAsync((new RegisterResponseDto
            {
                Error = new OperationResult(true, string.Empty)
            }, null));

        var result = await _controller.Register(new RegisterRequestDto { Email = "x@x.com", Password = "Abc@1234", FirstName = "Test", LastName = "User" });

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    // -------------------------------------------------------------------------
    // RefreshToken
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task RefreshToken_WhenCookiesAreMissing_ReturnsUnauthorized()
    {
        var result = await _controller.RefreshToken();

        Assert.IsInstanceOfType(result, typeof(UnauthorizedObjectResult));
    }

    [TestMethod]
    public async Task RefreshToken_WhenAccessTokenIsMalformed_ReturnsUnauthorized()
    {
        SetCookies("not-a-valid-jwt-token", "some-refresh-token");

        var result = await _controller.RefreshToken();

        Assert.IsInstanceOfType(result, typeof(UnauthorizedObjectResult));
    }

    [TestMethod]
    public async Task RefreshToken_WhenServiceFails_ReturnsUnauthorized()
    {
        SetCookies(CreateTestJwt("user-1"), "refresh-token");
        _serviceMock.Setup(s => s.RefreshTokenAsync(It.IsAny<RefreshTokenRequestDto>()))
            .ReturnsAsync((new RefreshTokenResponseDto { Error = new OperationResult(false, "Token revocado.") }, null));

        var result = await _controller.RefreshToken();

        Assert.IsInstanceOfType(result, typeof(UnauthorizedObjectResult));
    }

    [TestMethod]
    public async Task RefreshToken_WhenSuccess_ReturnsOk()
    {
        SetCookies(CreateTestJwt("user-1"), "refresh-token");
        _serviceMock.Setup(s => s.RefreshTokenAsync(It.IsAny<RefreshTokenRequestDto>()))
            .ReturnsAsync((new RefreshTokenResponseDto { Error = new OperationResult(true, string.Empty) }, null));

        var result = await _controller.RefreshToken();

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    // -------------------------------------------------------------------------
    // Logout
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Logout_WhenNoClaimPresent_ReturnsUnauthorized()
    {
        SetNoUserClaims();

        var result = await _controller.Logout();

        Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task Logout_WhenServiceReturnsFalse_ReturnsBadRequest()
    {
        SetUserClaims("user-1");
        _serviceMock.Setup(s => s.LogoutAsync("user-1")).ReturnsAsync(false);

        var result = await _controller.Logout();

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task Logout_WhenSuccess_ReturnsOk()
    {
        SetUserClaims("user-1");
        _serviceMock.Setup(s => s.LogoutAsync("user-1")).ReturnsAsync(true);

        var result = await _controller.Logout();

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    // -------------------------------------------------------------------------
    // ChangeRole
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task ChangeRole_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        SetUserClaims("user-1");
        _controller.ModelState.AddModelError("Email", "Required");

        var result = await _controller.ChangeRole(new UserRoleUpdateRequest());

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task ChangeRole_WhenNoClaimPresent_ReturnsUnauthorized()
    {
        SetNoUserClaims();

        var result = await _controller.ChangeRole(new UserRoleUpdateRequest { Email = "x@x.com", NewRole = RoleType.Teacher });

        Assert.IsInstanceOfType(result, typeof(UnauthorizedObjectResult));
    }

    [TestMethod]
    public async Task ChangeRole_WhenServiceFails_ReturnsBadRequest()
    {
        SetUserClaims("user-1");
        _serviceMock.Setup(s => s.ChangeUserRoleAsync(It.IsAny<UserRoleUpdateRequest>(), "user-1"))
            .ReturnsAsync(new UserRoleUpdateResponse { Error = new OperationResult(false, "User not found.") });

        var result = await _controller.ChangeRole(new UserRoleUpdateRequest { Email = "x@x.com", NewRole = RoleType.Teacher });

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task ChangeRole_WhenSuccess_ReturnsOk()
    {
        SetUserClaims("user-1");
        _serviceMock.Setup(s => s.ChangeUserRoleAsync(It.IsAny<UserRoleUpdateRequest>(), "user-1"))
            .ReturnsAsync(new UserRoleUpdateResponse { Error = new OperationResult(true, "Role updated to Teacher.") });

        var result = await _controller.ChangeRole(new UserRoleUpdateRequest { Email = "x@x.com", NewRole = RoleType.Teacher });

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    // -------------------------------------------------------------------------
    // ExecuteBulkAction
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task ExecuteBulkAction_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        SetUserClaims("user-1");
        _controller.ModelState.AddModelError("UserIds", "Required");

        var result = await _controller.ExecuteBulkAction(new BulkUserActionRequest());

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task ExecuteBulkAction_WhenNoClaimPresent_ReturnsUnauthorized()
    {
        SetNoUserClaims();

        var result = await _controller.ExecuteBulkAction(new BulkUserActionRequest
        {
            UserIds = new List<string> { "user-2" },
            Action = BulkUserActionType.Delete
        });

        Assert.IsInstanceOfType(result, typeof(UnauthorizedObjectResult));
    }

    [TestMethod]
    public async Task ExecuteBulkAction_WhenServiceFails_ReturnsBadRequest()
    {
        SetUserClaims("user-1");
        _serviceMock.Setup(s => s.ExecuteBulkActionAsync(It.IsAny<BulkUserActionRequest>(), "user-1"))
            .ReturnsAsync(new BulkUserActionResponse { Error = new OperationResult(false, "You cannot perform actions on yourself.") });

        var result = await _controller.ExecuteBulkAction(new BulkUserActionRequest
        {
            UserIds = new List<string> { "user-1" },
            Action = BulkUserActionType.Delete
        });

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task ExecuteBulkAction_WhenSuccess_ReturnsOk()
    {
        SetUserClaims("user-1");
        _serviceMock.Setup(s => s.ExecuteBulkActionAsync(It.IsAny<BulkUserActionRequest>(), "user-1"))
            .ReturnsAsync(new BulkUserActionResponse { Error = new OperationResult(true, "Deleted 1 user(s)."), AffectedCount = 1 });

        var result = await _controller.ExecuteBulkAction(new BulkUserActionRequest
        {
            UserIds = new List<string> { "user-2" },
            Action = BulkUserActionType.Delete
        });

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    // -------------------------------------------------------------------------
    // BulkImportUsers
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task BulkImportUsers_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        _controller.ModelState.AddModelError("FileContent", "Required");

        var result = await _controller.BulkImportUsers(new BulkUserImportRequest());

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task BulkImportUsers_WhenServiceReturnsParseError_ReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.BulkImportUsersAsync(It.IsAny<BulkUserImportRequest>()))
            .ReturnsAsync(new BulkUserImportResponse
            {
                Error = new OperationResult(false, "Row 2: 'role' is invalid.", "ParseError")
            });

        var result = await _controller.BulkImportUsers(new BulkUserImportRequest
        {
            FileName = "users.csv",
            FileContent = new byte[] { 1, 2, 3 }
        });

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        var body = ((BadRequestObjectResult)result).Value as BulkUserImportResponse;
        Assert.IsNotNull(body);
        Assert.IsFalse(body.Error.IsSuccess);
        Assert.AreEqual(0, body.CreatedCount);
    }

    [TestMethod]
    public async Task BulkImportUsers_WhenImportSucceeds_ReturnsOkWithCounts()
    {
        _serviceMock.Setup(s => s.BulkImportUsersAsync(It.IsAny<BulkUserImportRequest>()))
            .ReturnsAsync(new BulkUserImportResponse
            {
                Error = new OperationResult(true, "Import completed successfully."),
                CreatedCount = 3,
                SkippedCount = 1
            });

        var result = await _controller.BulkImportUsers(new BulkUserImportRequest
        {
            FileName = "users.csv",
            FileContent = new byte[] { 1, 2, 3 }
        });

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var body = ((OkObjectResult)result).Value as BulkUserImportResponse;
        Assert.IsNotNull(body);
        Assert.IsTrue(body.Error.IsSuccess);
        Assert.AreEqual(3, body.CreatedCount);
        Assert.AreEqual(1, body.SkippedCount);
    }

    [TestMethod]
    public async Task BulkImportUsers_WhenAllUsersAlreadyExist_ReturnsOkWithZeroCreated()
    {
        _serviceMock.Setup(s => s.BulkImportUsersAsync(It.IsAny<BulkUserImportRequest>()))
            .ReturnsAsync(new BulkUserImportResponse
            {
                Error = new OperationResult(true, "Import completed successfully."),
                CreatedCount = 0,
                SkippedCount = 2
            });

        var result = await _controller.BulkImportUsers(new BulkUserImportRequest
        {
            FileName = "users.csv",
            FileContent = new byte[] { 1, 2, 3 }
        });

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var body = ((OkObjectResult)result).Value as BulkUserImportResponse;
        Assert.IsNotNull(body);
        Assert.IsTrue(body.Error.IsSuccess);
        Assert.AreEqual(0, body.CreatedCount);
        Assert.AreEqual(2, body.SkippedCount);
    }

    // -------------------------------------------------------------------------
    // ChangeUserPassword
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task ChangeUserPassword_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        SetUserClaims("admin-1");
        _controller.ModelState.AddModelError("Email", "Required");

        var result = await _controller.ChangeUserPassword(new AdminPasswordChangeRequest());

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task ChangeUserPassword_WhenNoClaimPresent_ReturnsUnauthorized()
    {
        SetNoUserClaims();

        var result = await _controller.ChangeUserPassword(
            new AdminPasswordChangeRequest { Email = "u@x.com", NewPassword = "abc123", ConfirmPassword = "abc123" });

        Assert.IsInstanceOfType(result, typeof(UnauthorizedObjectResult));
    }

    [TestMethod]
    public async Task ChangeUserPassword_WhenServiceFails_ReturnsBadRequest()
    {
        SetUserClaims("admin-1");
        _serviceMock.Setup(s => s.ChangeUserPasswordAsync(It.IsAny<AdminPasswordChangeRequest>(), "admin-1"))
            .ReturnsAsync(new AdminPasswordChangeResponse { Error = new OperationResult(false, "User not found.") });

        var result = await _controller.ChangeUserPassword(
            new AdminPasswordChangeRequest { Email = "u@x.com", NewPassword = "abc123", ConfirmPassword = "abc123" });

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task ChangeUserPassword_WhenSuccess_ReturnsOk()
    {
        SetUserClaims("admin-1");
        _serviceMock.Setup(s => s.ChangeUserPasswordAsync(It.IsAny<AdminPasswordChangeRequest>(), "admin-1"))
            .ReturnsAsync(new AdminPasswordChangeResponse { Error = new OperationResult(true, "Password updated successfully.") });

        var result = await _controller.ChangeUserPassword(
            new AdminPasswordChangeRequest { Email = "u@x.com", NewPassword = "abc123", ConfirmPassword = "abc123" });

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }
}
