using AuthService.Data;
using AuthService.Repositories;
using AuthService.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Moq.Protected;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using TFELibrary.Shared;

namespace MatchTFE.UnitTest.AuthService;

[TestClass]
public class AuthServiceTests
{
    private Mock<IAuthRepository> _repositoryMock = null!;
    private Mock<IConfiguration> _configMock = null!;
    private Mock<IHttpClientFactory> _httpClientFactoryMock = null!;
    private Mock<ILogger<global::AuthService.Service.AuthService>> _loggerMock = null!;
    private Mock<IHttpContextAccessor> _httpContextAccessorMock = null!;
    private global::AuthService.Service.AuthService _service = null!;

    private const string JwtSecret = "test-jwt-secret-at-least-32-chars-ok!";
    private const string JwtRefreshSecret = "test-refresh-secret-at-32-chars-ok!";
    private const string JwtIssuer = "test-issuer";
    private const string JwtAudience = "test-audience";

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IAuthRepository>();
        _loggerMock = new Mock<ILogger<global::AuthService.Service.AuthService>>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();

        SetupJwtConfig();
        SetupHttpClient(HttpStatusCode.OK);

        _service = new global::AuthService.Service.AuthService(
            _repositoryMock.Object,
            _configMock.Object,
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            _httpContextAccessorMock.Object);
    }

    private void SetupJwtConfig()
    {
        var jwtSectionMock = new Mock<IConfigurationSection>();
        jwtSectionMock.Setup(s => s["Secret"]).Returns(JwtSecret);
        jwtSectionMock.Setup(s => s["RefreshSecret"]).Returns(JwtRefreshSecret);
        jwtSectionMock.Setup(s => s["Issuer"]).Returns(JwtIssuer);
        jwtSectionMock.Setup(s => s["Audience"]).Returns(JwtAudience);

        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c.GetSection("JwtSettings")).Returns(jwtSectionMock.Object);
    }

    private void SetupHttpClient(HttpStatusCode statusCode, string? jsonContent = null)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var responseMessage = new HttpResponseMessage(statusCode);
        if (jsonContent != null)
            responseMessage.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        var client = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://localhost/") };
        _httpClientFactoryMock.Setup(f => f.CreateClient("UserServiceClient")).Returns(client);
    }

    private string CreateValidRefreshToken()
    {
        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtRefreshSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        return handler.WriteToken(handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = JwtIssuer,
            Audience = JwtAudience,
            Expires = DateTime.UtcNow.AddMinutes(25),
            SigningCredentials = creds
        }));
    }

    private void SetupSuccessfulTokenGeneration(MatchUser user)
    {
        _repositoryMock.Setup(r => r.GetUserRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
        _repositoryMock.Setup(r => r.SaveRefreshTokenAsync(user, It.IsAny<string>())).Returns(Task.CompletedTask);
    }

    // -------------------------------------------------------------------------
    // LoginAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task LoginAsync_WhenRequestIsNull_ReturnsError()
    {
        var (result, _) = await _service.LoginAsync(null!);

        Assert.IsFalse(result.AuthData.IsSuccess);
        Assert.AreEqual("Email and password are required.", result.AuthData.Message);
    }

    [TestMethod]
    public async Task LoginAsync_WhenEmailIsBlank_ReturnsError()
    {
        var (result, _) = await _service.LoginAsync(new LoginRequestDto { Email = "   ", Password = "pass" });

        Assert.IsFalse(result.AuthData.IsSuccess);
    }

    [TestMethod]
    public async Task LoginAsync_WhenUserNotFound_ReturnsError()
    {
        _repositoryMock.Setup(r => r.GetUserByEmailAsync("x@x.com")).ReturnsAsync((MatchUser?)null);

        var (result, _) = await _service.LoginAsync(new LoginRequestDto { Email = "x@x.com", Password = "pass" });

        Assert.IsFalse(result.AuthData.IsSuccess);
        Assert.AreEqual("Invalid credentials.", result.AuthData.Message);
    }

    [TestMethod]
    public async Task LoginAsync_WhenPasswordIsWrong_ReturnsError()
    {
        var user = new MatchUser { Id = "user-1", Email = "x@x.com" };
        _repositoryMock.Setup(r => r.GetUserByEmailAsync("x@x.com")).ReturnsAsync(user);
        _repositoryMock.Setup(r => r.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);

        var (result, _) = await _service.LoginAsync(new LoginRequestDto { Email = "x@x.com", Password = "wrong" });

        Assert.IsFalse(result.AuthData.IsSuccess);
    }

    [TestMethod]
    public async Task LoginAsync_WhenCredentialsValid_ReturnsSuccessWithTokens()
    {
        var user = new MatchUser { Id = "user-1", Name = "Test", Surname = "User", Email = "x@x.com" };
        _repositoryMock.Setup(r => r.GetUserByEmailAsync("x@x.com")).ReturnsAsync(user);
        _repositoryMock.Setup(r => r.CheckPasswordAsync(user, "Abc@1234")).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.IsLockedOutAsync(user)).ReturnsAsync(false);
        SetupSuccessfulTokenGeneration(user);

        var (result, tokens) = await _service.LoginAsync(new LoginRequestDto { Email = "x@x.com", Password = "Abc@1234" });

        Assert.IsTrue(result.AuthData.IsSuccess);
        Assert.IsNotNull(tokens);
        Assert.IsFalse(string.IsNullOrEmpty(tokens.AccessToken));
        Assert.IsFalse(string.IsNullOrEmpty(tokens.RefreshToken));
        Assert.AreEqual("Test", result.FirstName);
    }

    [TestMethod]
    public async Task LoginAsync_WhenUserIsLockedOut_ReturnsAccountSuspendedError()
    {
        var user = new MatchUser { Id = "user-1", Email = "x@x.com" };
        _repositoryMock.Setup(r => r.GetUserByEmailAsync("x@x.com")).ReturnsAsync(user);
        _repositoryMock.Setup(r => r.CheckPasswordAsync(user, "Abc@1234")).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.IsLockedOutAsync(user)).ReturnsAsync(true);

        var (result, _) = await _service.LoginAsync(new LoginRequestDto { Email = "x@x.com", Password = "Abc@1234" });

        Assert.IsFalse(result.AuthData.IsSuccess);
        Assert.AreEqual("Account suspended.", result.AuthData.Message);
    }

    [TestMethod]
    public async Task LoginAsync_WhenUserIsLockedOut_DoesNotGenerateTokens()
    {
        var user = new MatchUser { Id = "user-1", Email = "x@x.com" };
        _repositoryMock.Setup(r => r.GetUserByEmailAsync("x@x.com")).ReturnsAsync(user);
        _repositoryMock.Setup(r => r.CheckPasswordAsync(user, "Abc@1234")).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.IsLockedOutAsync(user)).ReturnsAsync(true);

        var (_, tokens) = await _service.LoginAsync(new LoginRequestDto { Email = "x@x.com", Password = "Abc@1234" });

        Assert.IsNull(tokens);
    }

    // -------------------------------------------------------------------------
    // RefreshTokenAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task RefreshTokenAsync_WhenRequestIsNull_ReturnsError()
    {
        var (result, _) = await _service.RefreshTokenAsync(null!);

        Assert.IsFalse(result.AuthData.IsSuccess);
        Assert.AreEqual("Invalid refresh token request.", result.AuthData.Message);
    }

    [TestMethod]
    public async Task RefreshTokenAsync_WhenRefreshTokenIsInvalid_ReturnsError()
    {
        var (result, _) = await _service.RefreshTokenAsync(new RefreshTokenRequestDto
        {
            UserId = "user-1",
            RefreshToken = "not-a-valid-jwt"
        });

        Assert.IsFalse(result.AuthData.IsSuccess);
    }

    [TestMethod]
    public async Task RefreshTokenAsync_WhenUserNotFound_ReturnsError()
    {
        _repositoryMock.Setup(r => r.GetUserByIdAsync("user-99")).ReturnsAsync((MatchUser?)null);

        var (result, _) = await _service.RefreshTokenAsync(new RefreshTokenRequestDto
        {
            UserId = "user-99",
            RefreshToken = CreateValidRefreshToken()
        });

        Assert.IsFalse(result.AuthData.IsSuccess);
        Assert.AreEqual("User not found.", result.AuthData.Message);
    }

    [TestMethod]
    public async Task RefreshTokenAsync_WhenStoredTokenMismatch_ReturnsError()
    {
        var user = new MatchUser { Id = "user-1" };
        var validToken = CreateValidRefreshToken();
        _repositoryMock.Setup(r => r.GetUserByIdAsync("user-1")).ReturnsAsync(user);
        _repositoryMock.Setup(r => r.GetRefreshTokenAsync(user)).ReturnsAsync("a-different-token");

        var (result, _) = await _service.RefreshTokenAsync(new RefreshTokenRequestDto
        {
            UserId = "user-1",
            RefreshToken = validToken
        });

        Assert.IsFalse(result.AuthData.IsSuccess);
        Assert.AreEqual("Token revocado o reemplazado.", result.AuthData.Message);
    }

    [TestMethod]
    public async Task RefreshTokenAsync_WhenValid_ReturnsNewTokens()
    {
        var user = new MatchUser { Id = "user-1" };
        var validToken = CreateValidRefreshToken();
        _repositoryMock.Setup(r => r.GetUserByIdAsync("user-1")).ReturnsAsync(user);
        _repositoryMock.Setup(r => r.GetRefreshTokenAsync(user)).ReturnsAsync(validToken);
        SetupSuccessfulTokenGeneration(user);

        var (result, tokens) = await _service.RefreshTokenAsync(new RefreshTokenRequestDto
        {
            UserId = "user-1",
            RefreshToken = validToken
        });

        Assert.IsTrue(result.AuthData.IsSuccess);
        Assert.IsNotNull(tokens);
        Assert.IsFalse(string.IsNullOrEmpty(tokens.AccessToken));
    }

    // -------------------------------------------------------------------------
    // LogoutAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task LogoutAsync_WhenUserNotFound_ReturnsFalse()
    {
        _repositoryMock.Setup(r => r.GetUserByIdAsync("user-99")).ReturnsAsync((MatchUser?)null);

        var result = await _service.LogoutAsync("user-99");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task LogoutAsync_WhenUserExists_ReturnsTrue()
    {
        var user = new MatchUser { Id = "user-1" };
        _repositoryMock.Setup(r => r.GetUserByIdAsync("user-1")).ReturnsAsync(user);
        _repositoryMock.Setup(r => r.RemoveRefreshTokenAsync(user)).Returns(Task.CompletedTask);

        var result = await _service.LogoutAsync("user-1");

        Assert.IsTrue(result);
        _repositoryMock.Verify(r => r.RemoveRefreshTokenAsync(user), Times.Once);
    }

    // -------------------------------------------------------------------------
    // RegisterAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task RegisterAsync_WhenRequestIsNull_ReturnsError()
    {
        var (result, _) = await _service.RegisterAsync(null!);

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.IsFalse(result.AuthData.IsSuccess);
    }

    [TestMethod]
    public async Task RegisterAsync_WhenIdentityFailsDuplicateEmail_ReturnsDuplicateEmailError()
    {
        _repositoryMock.Setup(r => r.CreateUserAsync(It.IsAny<MatchUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "DuplicateUserName", Description = "Email 'x@x.com' is already taken." }));

        var (result, _) = await _service.RegisterAsync(new RegisterRequestDto { Email = "x@x.com", Password = "Abc@1234", FirstName = "Test", LastName = "User" });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("DuplicateEmail", result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task RegisterAsync_WhenIdentityFailsOtherError_ReturnsErrorWithoutCode()
    {
        _repositoryMock.Setup(r => r.CreateUserAsync(It.IsAny<MatchUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordTooShort", Description = "Password too short." }));

        var (result, _) = await _service.RegisterAsync(new RegisterRequestDto { Email = "x@x.com", Password = "123", FirstName = "Test", LastName = "User" });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.IsNull(result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task RegisterAsync_WhenUserServiceFails_DeletesUserAndReturnsError()
    {
        _repositoryMock.Setup(r => r.CreateUserAsync(It.IsAny<MatchUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        _repositoryMock.Setup(r => r.AddToRoleAsync(It.IsAny<MatchUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        _repositoryMock.Setup(r => r.GetUserRolesAsync(It.IsAny<MatchUser>())).ReturnsAsync(new List<string> { "User" });
        _repositoryMock.Setup(r => r.SaveRefreshTokenAsync(It.IsAny<MatchUser>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _repositoryMock.Setup(r => r.DeleteUserAsync(It.IsAny<MatchUser>())).ReturnsAsync(IdentityResult.Success);

        SetupHttpClient(HttpStatusCode.BadRequest,
            """{"Error":{"IsSuccess":false,"Message":"Profile creation failed.","ErrorCode":null},"UserId":null}""");

        var (result, _) = await _service.RegisterAsync(new RegisterRequestDto { Email = "x@x.com", Password = "Abc@1234", FirstName = "Test", LastName = "User" });

        Assert.IsFalse(result.Error.IsSuccess);
        _repositoryMock.Verify(r => r.DeleteUserAsync(It.IsAny<MatchUser>()), Times.Once);
    }

    [TestMethod]
    public async Task RegisterAsync_WhenAllSucceeds_ReturnsSuccess()
    {
        _repositoryMock.Setup(r => r.CreateUserAsync(It.IsAny<MatchUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        _repositoryMock.Setup(r => r.AddToRoleAsync(It.IsAny<MatchUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        _repositoryMock.Setup(r => r.GetUserRolesAsync(It.IsAny<MatchUser>())).ReturnsAsync(new List<string> { "User" });
        _repositoryMock.Setup(r => r.SaveRefreshTokenAsync(It.IsAny<MatchUser>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var (result, tokens) = await _service.RegisterAsync(new RegisterRequestDto { Email = "x@x.com", Password = "Abc@1234", FirstName = "Test", LastName = "User" });

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.IsTrue(result.AuthData.IsSuccess);
        Assert.IsNotNull(tokens);
        Assert.IsFalse(string.IsNullOrEmpty(tokens.AccessToken));
    }

    // -------------------------------------------------------------------------
    // ExecuteBulkActionAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task ExecuteBulkActionAsync_WhenUserIdsIsEmpty_ReturnsError()
    {
        var result = await _service.ExecuteBulkActionAsync(
            new BulkUserActionRequest { UserIds = new List<string>(), Action = BulkUserActionType.Delete },
            "admin-1");

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("Request must include at least one user ID.", result.Error.Message);
    }

    [TestMethod]
    public async Task ExecuteBulkActionAsync_WhenContainsCurrentUser_ReturnsError()
    {
        var result = await _service.ExecuteBulkActionAsync(
            new BulkUserActionRequest { UserIds = new List<string> { "admin-1", "user-2" }, Action = BulkUserActionType.Delete },
            "admin-1");

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("You cannot perform actions on yourself.", result.Error.Message);
    }

    [TestMethod]
    public async Task ExecuteBulkActionAsync_WhenSuspendContainsCurrentUser_ReturnsError()
    {
        var result = await _service.ExecuteBulkActionAsync(
            new BulkUserActionRequest { UserIds = new List<string> { "admin-1", "user-2" }, Action = BulkUserActionType.Suspend },
            "admin-1");

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("You cannot perform actions on yourself.", result.Error.Message);
    }

    [TestMethod]
    public async Task ExecuteBulkActionAsync_WhenUnsuspendContainsCurrentUser_ReturnsError()
    {
        var result = await _service.ExecuteBulkActionAsync(
            new BulkUserActionRequest { UserIds = new List<string> { "admin-1", "user-2" }, Action = BulkUserActionType.Unsuspend },
            "admin-1");

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("You cannot perform actions on yourself.", result.Error.Message);
    }

    [TestMethod]
    public async Task ExecuteBulkActionAsync_WhenDeleteAction_ReturnsAffectedCount()
    {
        var user2 = new MatchUser { Id = "user-2" };
        var user3 = new MatchUser { Id = "user-3" };
        _repositoryMock.Setup(r => r.GetUsersByIdsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new List<MatchUser> { user2, user3 });
        _repositoryMock.Setup(r => r.DeleteUserAsync(It.IsAny<MatchUser>())).ReturnsAsync(IdentityResult.Success);

        var result = await _service.ExecuteBulkActionAsync(
            new BulkUserActionRequest { UserIds = new List<string> { "user-2", "user-3" }, Action = BulkUserActionType.Delete },
            "admin-1");

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.AreEqual(2, result.AffectedCount);
    }

    [TestMethod]
    public async Task ExecuteBulkActionAsync_WhenUnsupportedAction_ReturnsError()
    {
        var result = await _service.ExecuteBulkActionAsync(
            new BulkUserActionRequest { UserIds = new List<string> { "user-2" }, Action = (BulkUserActionType)999 },
            "admin-1");

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("Action not supported.", result.Error.Message);
    }

    [TestMethod]
    public async Task ExecuteBulkActionAsync_WhenSuspend_CallsLockUserForEachSelectedUser()
    {
        var user2 = new MatchUser { Id = "user-2" };
        var user3 = new MatchUser { Id = "user-3" };
        _repositoryMock.Setup(r => r.GetUsersByIdsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new List<MatchUser> { user2, user3 });
        _repositoryMock.Setup(r => r.LockUserAsync(It.IsAny<MatchUser>())).Returns(Task.CompletedTask);

        await _service.ExecuteBulkActionAsync(
            new BulkUserActionRequest { UserIds = new List<string> { "user-2", "user-3" }, Action = BulkUserActionType.Suspend },
            "admin-1");

        _repositoryMock.Verify(r => r.LockUserAsync(It.IsAny<MatchUser>()), Times.Exactly(2));
    }

    [TestMethod]
    public async Task ExecuteBulkActionAsync_WhenSuspend_ReturnsCorrectAffectedCount()
    {
        var user2 = new MatchUser { Id = "user-2" };
        var user3 = new MatchUser { Id = "user-3" };
        _repositoryMock.Setup(r => r.GetUsersByIdsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new List<MatchUser> { user2, user3 });
        _repositoryMock.Setup(r => r.LockUserAsync(It.IsAny<MatchUser>())).Returns(Task.CompletedTask);

        var result = await _service.ExecuteBulkActionAsync(
            new BulkUserActionRequest { UserIds = new List<string> { "user-2", "user-3" }, Action = BulkUserActionType.Suspend },
            "admin-1");

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.AreEqual(2, result.AffectedCount);
    }

    [TestMethod]
    public async Task ExecuteBulkActionAsync_WhenUnsuspend_CallsUnlockUserForEachSelectedUser()
    {
        var user2 = new MatchUser { Id = "user-2" };
        _repositoryMock.Setup(r => r.GetUsersByIdsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new List<MatchUser> { user2 });
        _repositoryMock.Setup(r => r.UnlockUserAsync(It.IsAny<MatchUser>())).Returns(Task.CompletedTask);

        await _service.ExecuteBulkActionAsync(
            new BulkUserActionRequest { UserIds = new List<string> { "user-2" }, Action = BulkUserActionType.Unsuspend },
            "admin-1");

        _repositoryMock.Verify(r => r.UnlockUserAsync(It.IsAny<MatchUser>()), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteBulkActionAsync_WhenUnsuspend_ReturnsCorrectAffectedCount()
    {
        var user2 = new MatchUser { Id = "user-2" };
        _repositoryMock.Setup(r => r.GetUsersByIdsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new List<MatchUser> { user2 });
        _repositoryMock.Setup(r => r.UnlockUserAsync(It.IsAny<MatchUser>())).Returns(Task.CompletedTask);

        var result = await _service.ExecuteBulkActionAsync(
            new BulkUserActionRequest { UserIds = new List<string> { "user-2" }, Action = BulkUserActionType.Unsuspend },
            "admin-1");

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.AreEqual(1, result.AffectedCount);
    }

    // -------------------------------------------------------------------------
    // BulkImportUsersAsync
    // -------------------------------------------------------------------------

    private static byte[] CsvBytes(params string[] lines)
        => System.Text.Encoding.UTF8.GetBytes(string.Join("\n", lines));

    [TestMethod]
    public async Task BulkImportUsersAsync_WhenFileIsEmpty_ReturnsParseError()
    {
        var result = await _service.BulkImportUsersAsync(
            new BulkUserImportRequest { FileName = "f.csv", FileContent = Array.Empty<byte>() });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("ParseError", result.Error.ErrorCode);
        Assert.AreEqual(0, result.CreatedCount);
    }

    [TestMethod]
    public async Task BulkImportUsersAsync_WhenRowHasTooFewFields_ReturnsParseErrorAndCreatesNothing()
    {
        var content = CsvBytes("a@a.com,Juan,García");   // only 3 fields, row 1

        var result = await _service.BulkImportUsersAsync(
            new BulkUserImportRequest { FileName = "f.csv", FileContent = content });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("ParseError", result.Error.ErrorCode);
        Assert.IsTrue(result.Error.Message.Contains("1"));
        _repositoryMock.Verify(r => r.CreateUserAsync(It.IsAny<MatchUser>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task BulkImportUsersAsync_WhenRoleIsAdmin_ReturnsParseErrorAndCreatesNothing()
    {
        var content = CsvBytes("a@a.com,Juan,García,Pass123!,Admin");

        var result = await _service.BulkImportUsersAsync(
            new BulkUserImportRequest { FileName = "f.csv", FileContent = content });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("ParseError", result.Error.ErrorCode);
        _repositoryMock.Verify(r => r.CreateUserAsync(It.IsAny<MatchUser>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task BulkImportUsersAsync_WhenSecondRowIsInvalid_AbortsWithoutCreatingFirstValidRow()
    {
        // Row 1 valid, row 2 has empty firstName — parser aborts on row 2, row 1 is not created
        var content = CsvBytes(
            "ok@a.com,Juan,García,Pass123!,Student",
            "bad@a.com,,García,Pass123!,Teacher");

        var result = await _service.BulkImportUsersAsync(
            new BulkUserImportRequest { FileName = "f.csv", FileContent = content });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.IsTrue(result.Error.Message.Contains("2"));
        _repositoryMock.Verify(r => r.CreateUserAsync(It.IsAny<MatchUser>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task BulkImportUsersAsync_WhenAllUsersAreNew_CreatesAllAndReturnsCreatedCount()
    {
        var content = CsvBytes(
            "a@a.com,Juan,García,Pass123!,Student",
            "b@b.com,María,López,Pass456!,Teacher");

        _repositoryMock.Setup(r => r.GetUserByEmailAsync("a@a.com")).ReturnsAsync((MatchUser?)null);
        _repositoryMock.Setup(r => r.GetUserByEmailAsync("b@b.com")).ReturnsAsync((MatchUser?)null);
        _repositoryMock.Setup(r => r.CreateUserAsync(It.IsAny<MatchUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _repositoryMock.Setup(r => r.AddToRoleAsync(It.IsAny<MatchUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _repositoryMock.Setup(r => r.GetUserRolesAsync(It.IsAny<MatchUser>()))
            .ReturnsAsync(new List<string> { "User" });
        _repositoryMock.Setup(r => r.SaveRefreshTokenAsync(It.IsAny<MatchUser>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        SetupHttpClient(HttpStatusCode.OK);

        var result = await _service.BulkImportUsersAsync(
            new BulkUserImportRequest { FileName = "f.csv", FileContent = content });

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.AreEqual(2, result.CreatedCount);
        Assert.AreEqual(0, result.SkippedCount);
    }

    [TestMethod]
    public async Task BulkImportUsersAsync_WhenUserAlreadyExists_SkipsExistingAndCreatesNew()
    {
        var content = CsvBytes(
            "existing@a.com,Ana,Ruiz,Pass123!,Student",
            "new@b.com,Carlos,Martínez,Pass456!,Teacher");

        var existingUser = new MatchUser { Id = "existing-id", Email = "existing@a.com" };

        _repositoryMock.Setup(r => r.GetUserByEmailAsync("existing@a.com")).ReturnsAsync(existingUser);
        _repositoryMock.Setup(r => r.GetUserByEmailAsync("new@b.com")).ReturnsAsync((MatchUser?)null);
        _repositoryMock.Setup(r => r.CreateUserAsync(It.IsAny<MatchUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _repositoryMock.Setup(r => r.AddToRoleAsync(It.IsAny<MatchUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _repositoryMock.Setup(r => r.GetUserRolesAsync(It.IsAny<MatchUser>()))
            .ReturnsAsync(new List<string> { "User" });
        _repositoryMock.Setup(r => r.SaveRefreshTokenAsync(It.IsAny<MatchUser>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        SetupHttpClient(HttpStatusCode.OK);

        var result = await _service.BulkImportUsersAsync(
            new BulkUserImportRequest { FileName = "f.csv", FileContent = content });

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.AreEqual(1, result.CreatedCount);
        Assert.AreEqual(1, result.SkippedCount);
        _repositoryMock.Verify(r => r.CreateUserAsync(It.IsAny<MatchUser>(), It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task BulkImportUsersAsync_WhenAllUsersAlreadyExist_ReturnsZeroCreated()
    {
        var content = CsvBytes("a@a.com,Juan,García,Pass123!,Student");

        _repositoryMock.Setup(r => r.GetUserByEmailAsync("a@a.com"))
            .ReturnsAsync(new MatchUser { Id = "id-a", Email = "a@a.com" });

        var result = await _service.BulkImportUsersAsync(
            new BulkUserImportRequest { FileName = "f.csv", FileContent = content });

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.AreEqual(0, result.CreatedCount);
        Assert.AreEqual(1, result.SkippedCount);
        _repositoryMock.Verify(r => r.CreateUserAsync(It.IsAny<MatchUser>(), It.IsAny<string>()), Times.Never);
    }

    // --- CsvHelper-specific scenarios ---

    [TestMethod]
    public async Task BulkImportUsersAsync_WhenRoleIsAllUpperCase_ParsesCorrectlyAndCreatesUser()
    {
        var content = CsvBytes("a@a.com,Juan,García,Pass123!,STUDENT");

        _repositoryMock.Setup(r => r.GetUserByEmailAsync("a@a.com")).ReturnsAsync((MatchUser?)null);
        _repositoryMock.Setup(r => r.CreateUserAsync(It.IsAny<MatchUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _repositoryMock.Setup(r => r.AddToRoleAsync(It.IsAny<MatchUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _repositoryMock.Setup(r => r.GetUserRolesAsync(It.IsAny<MatchUser>()))
            .ReturnsAsync(new List<string> { "User" });
        _repositoryMock.Setup(r => r.SaveRefreshTokenAsync(It.IsAny<MatchUser>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        SetupHttpClient(HttpStatusCode.OK);

        var result = await _service.BulkImportUsersAsync(
            new BulkUserImportRequest { FileName = "f.csv", FileContent = content });

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.AreEqual(1, result.CreatedCount);
    }

    [TestMethod]
    public async Task BulkImportUsersAsync_WhenFileHasUtf8Bom_ParsesCorrectlyAndCreatesUser()
    {
        // Some editors (e.g. Excel, Notepad on Windows) save UTF-8 files with a BOM prefix
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var body = System.Text.Encoding.UTF8.GetBytes("a@a.com,Juan,García,Pass123!,Student");
        var content = bom.Concat(body).ToArray();

        _repositoryMock.Setup(r => r.GetUserByEmailAsync("a@a.com")).ReturnsAsync((MatchUser?)null);
        _repositoryMock.Setup(r => r.CreateUserAsync(It.IsAny<MatchUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _repositoryMock.Setup(r => r.AddToRoleAsync(It.IsAny<MatchUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _repositoryMock.Setup(r => r.GetUserRolesAsync(It.IsAny<MatchUser>()))
            .ReturnsAsync(new List<string> { "User" });
        _repositoryMock.Setup(r => r.SaveRefreshTokenAsync(It.IsAny<MatchUser>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        SetupHttpClient(HttpStatusCode.OK);

        var result = await _service.BulkImportUsersAsync(
            new BulkUserImportRequest { FileName = "f.csv", FileContent = content });

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.AreEqual(1, result.CreatedCount);
    }

    [TestMethod]
    public async Task BulkImportUsersAsync_WhenLastNameContainsCommaInsideQuotes_ParsesCorrectlyAndCreatesUser()
    {
        // CsvHelper supports quoted fields — our previous manual parser did not
        var content = CsvBytes("a@a.com,Juan,\"García, Jr.\",Pass123!,Student");

        MatchUser? capturedUser = null;
        _repositoryMock.Setup(r => r.GetUserByEmailAsync("a@a.com")).ReturnsAsync((MatchUser?)null);
        _repositoryMock.Setup(r => r.CreateUserAsync(It.IsAny<MatchUser>(), It.IsAny<string>()))
            .Callback<MatchUser, string>((u, _) => capturedUser = u)
            .ReturnsAsync(IdentityResult.Success);
        _repositoryMock.Setup(r => r.AddToRoleAsync(It.IsAny<MatchUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _repositoryMock.Setup(r => r.GetUserRolesAsync(It.IsAny<MatchUser>()))
            .ReturnsAsync(new List<string> { "User" });
        _repositoryMock.Setup(r => r.SaveRefreshTokenAsync(It.IsAny<MatchUser>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        SetupHttpClient(HttpStatusCode.OK);

        var result = await _service.BulkImportUsersAsync(
            new BulkUserImportRequest { FileName = "f.csv", FileContent = content });

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.AreEqual(1, result.CreatedCount);
        Assert.AreEqual("García, Jr.", capturedUser?.Surname);
    }

    [TestMethod]
    public async Task BulkImportUsersAsync_WhenProfileCreationFails_DeletesUserAndDoesNotCount()
    {
        var content = CsvBytes("a@a.com,Juan,García,Pass123!,Student");

        _repositoryMock.Setup(r => r.GetUserByEmailAsync("a@a.com")).ReturnsAsync((MatchUser?)null);
        _repositoryMock.Setup(r => r.CreateUserAsync(It.IsAny<MatchUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _repositoryMock.Setup(r => r.AddToRoleAsync(It.IsAny<MatchUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _repositoryMock.Setup(r => r.GetUserRolesAsync(It.IsAny<MatchUser>()))
            .ReturnsAsync(new List<string> { "User" });
        _repositoryMock.Setup(r => r.SaveRefreshTokenAsync(It.IsAny<MatchUser>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _repositoryMock.Setup(r => r.DeleteUserAsync(It.IsAny<MatchUser>()))
            .ReturnsAsync(IdentityResult.Success);
        SetupHttpClient(HttpStatusCode.InternalServerError);

        var result = await _service.BulkImportUsersAsync(
            new BulkUserImportRequest { FileName = "f.csv", FileContent = content });

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.AreEqual(0, result.CreatedCount);
        _repositoryMock.Verify(r => r.DeleteUserAsync(It.IsAny<MatchUser>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // ChangeUserPasswordAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task ChangeUserPasswordAsync_WhenUserNotFound_ReturnsError()
    {
        _repositoryMock.Setup(r => r.GetUserByEmailAsync("nf@x.com")).ReturnsAsync((MatchUser?)null);

        var result = await _service.ChangeUserPasswordAsync(
            new AdminPasswordChangeRequest { Email = "nf@x.com", NewPassword = "abc123", ConfirmPassword = "abc123" },
            "admin-1");

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("UserNotFound", result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task ChangeUserPasswordAsync_WhenPasswordsDoNotMatch_ReturnsError()
    {
        var user = new MatchUser { Id = "user-2", Email = "u@x.com" };
        _repositoryMock.Setup(r => r.GetUserByEmailAsync("u@x.com")).ReturnsAsync(user);

        var result = await _service.ChangeUserPasswordAsync(
            new AdminPasswordChangeRequest { Email = "u@x.com", NewPassword = "abc123", ConfirmPassword = "different" },
            "admin-1");

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("PasswordMismatch", result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task ChangeUserPasswordAsync_WhenIdentityFails_ReturnsError()
    {
        var user = new MatchUser { Id = "user-2", Email = "u@x.com" };
        _repositoryMock.Setup(r => r.GetUserByEmailAsync("u@x.com")).ReturnsAsync(user);
        _repositoryMock.Setup(r => r.ResetPasswordDirectlyAsync(user, "abc123"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak." }));

        var result = await _service.ChangeUserPasswordAsync(
            new AdminPasswordChangeRequest { Email = "u@x.com", NewPassword = "abc123", ConfirmPassword = "abc123" },
            "admin-1");

        Assert.IsFalse(result.Error.IsSuccess);
    }

    [TestMethod]
    public async Task ChangeUserPasswordAsync_WhenSuccess_ReturnsSuccess()
    {
        var user = new MatchUser { Id = "user-2", Email = "u@x.com" };
        _repositoryMock.Setup(r => r.GetUserByEmailAsync("u@x.com")).ReturnsAsync(user);
        _repositoryMock.Setup(r => r.ResetPasswordDirectlyAsync(user, "Abc@1234"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _service.ChangeUserPasswordAsync(
            new AdminPasswordChangeRequest { Email = "u@x.com", NewPassword = "Abc@1234", ConfirmPassword = "Abc@1234" },
            "admin-1");

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.AreEqual("Password updated successfully.", result.Error.Message);
        _repositoryMock.Verify(r => r.ResetPasswordDirectlyAsync(user, "Abc@1234"), Times.Once);
    }
}
