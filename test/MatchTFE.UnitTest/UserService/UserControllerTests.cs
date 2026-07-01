using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using TFELibrary.Shared;
using UserService.Controllers;
using UserService.Service;

namespace MatchTFE.UnitTest.UserService;

[TestClass]
public class UserControllerTests
{
    private Mock<IUserService> _serviceMock = null!;
    private UserController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _serviceMock = new Mock<IUserService>();
        _controller = new UserController(_serviceMock.Object);
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

    // -------------------------------------------------------------------------
    // GetCurrentProfile
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task GetCurrentProfile_WhenNoClaimPresent_ReturnsUnauthorized()
    {
        SetNoUserClaims();

        var result = await _controller.GetCurrentProfile();

        Assert.IsInstanceOfType(result.Result, typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task GetCurrentProfile_WhenServiceReturnsNotFound_ReturnsNotFound()
    {
        SetUserClaims("user-1");
        _serviceMock.Setup(s => s.GetProfileByUserIdAsync("user-1"))
            .ReturnsAsync(new ProfileResponse(new OperationResult(false, "User not found.", "UserNotFound")));

        var result = await _controller.GetCurrentProfile();

        Assert.IsInstanceOfType(result.Result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task GetCurrentProfile_WhenServiceReturnsProfile_ReturnsOk()
    {
        SetUserClaims("user-1");
        var profile = new ProfileResponse(new OperationResult(true, string.Empty), new ProfileDto { FirstName = "Ana" });
        _serviceMock.Setup(s => s.GetProfileByUserIdAsync("user-1")).ReturnsAsync(profile);

        var result = await _controller.GetCurrentProfile();

        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        Assert.AreEqual(profile, ok.Value);
    }

    // -------------------------------------------------------------------------
    // GetProfileById
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task GetProfileById_WhenUserIdIsWhitespace_ReturnsBadRequest()
    {
        var result = await _controller.GetProfileById("   ");

        Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task GetProfileById_WhenServiceReturnsNotFound_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.GetProfileByUserIdAsync("user-99"))
            .ReturnsAsync(new ProfileResponse(new OperationResult(false, "User not found.", "UserNotFound")));

        var result = await _controller.GetProfileById("user-99");

        Assert.IsInstanceOfType(result.Result, typeof(NotFoundObjectResult));
    }

    [TestMethod]
    public async Task GetProfileById_WhenServiceReturnsProfile_ReturnsOk()
    {
        var profile = new ProfileResponse(new OperationResult(true, string.Empty), new ProfileDto { FirstName = "Luis" });
        _serviceMock.Setup(s => s.GetProfileByUserIdAsync("user-1")).ReturnsAsync(profile);

        var result = await _controller.GetProfileById("user-1");

        Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
    }

    // -------------------------------------------------------------------------
    // CreateInitialProfile
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task CreateInitialProfile_WhenRequestIsNull_ReturnsBadRequest()
    {
        SetUserClaims("user-1");

        var result = await _controller.CreateInitialProfile(null!);

        Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task CreateInitialProfile_WhenNoClaimPresent_ReturnsUnauthorized()
    {
        SetNoUserClaims();
        var request = new ProfileCreationRequest("user-1", new ProfileDto());

        var result = await _controller.CreateInitialProfile(request);

        Assert.IsInstanceOfType(result.Result, typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task CreateInitialProfile_WhenClaimDoesNotMatchRequestUserId_ReturnsUnauthorized()
    {
        SetUserClaims("user-different");
        var request = new ProfileCreationRequest("user-1", new ProfileDto());

        var result = await _controller.CreateInitialProfile(request);

        Assert.IsInstanceOfType(result.Result, typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task CreateInitialProfile_WhenServiceReturnsDuplicateEmail_ReturnsConflict()
    {
        SetUserClaims("user-1");
        var request = new ProfileCreationRequest("user-1", new ProfileDto { Email = "dup@test.com" });
        var serviceResponse = new ProfileCreationResponse(new OperationResult(false, "Email already exists.", "DuplicateEmail"));
        _serviceMock.Setup(s => s.CreateProfileAsync(request)).ReturnsAsync(serviceResponse);

        var result = await _controller.CreateInitialProfile(request);

        Assert.IsInstanceOfType(result.Result, typeof(ConflictObjectResult));
    }

    [TestMethod]
    public async Task CreateInitialProfile_WhenServiceReturnsDuplicateUserProfile_ReturnsConflict()
    {
        SetUserClaims("user-1");
        var request = new ProfileCreationRequest("user-1", new ProfileDto { Email = "test@test.com" });
        var serviceResponse = new ProfileCreationResponse(new OperationResult(false, "Profile already exists.", "DuplicateUserProfile"));
        _serviceMock.Setup(s => s.CreateProfileAsync(request)).ReturnsAsync(serviceResponse);

        var result = await _controller.CreateInitialProfile(request);

        Assert.IsInstanceOfType(result.Result, typeof(ConflictObjectResult));
    }

    [TestMethod]
    public async Task CreateInitialProfile_WhenServiceReturnsSuccess_ReturnsCreated()
    {
        SetUserClaims("user-1");
        var request = new ProfileCreationRequest("user-1", new ProfileDto { Email = "test@test.com" });
        var serviceResponse = new ProfileCreationResponse(new OperationResult(true, "Profile created successfully."), "user-1");
        _serviceMock.Setup(s => s.CreateProfileAsync(request)).ReturnsAsync(serviceResponse);

        var result = await _controller.CreateInitialProfile(request);

        Assert.IsInstanceOfType(result.Result, typeof(CreatedAtActionResult));
    }

    // -------------------------------------------------------------------------
    // UpdateProfile
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task UpdateProfile_WhenNoClaimPresent_ReturnsUnauthorized()
    {
        SetNoUserClaims();

        var result = await _controller.UpdateProfile(new ProfileUpdateRequest(new ProfileDto()));

        Assert.IsInstanceOfType(result.Result, typeof(UnauthorizedObjectResult));
    }

    [TestMethod]
    public async Task UpdateProfile_WhenServiceReturnsUserNotFound_ReturnsNotFound()
    {
        SetUserClaims("user-1");
        var serviceResponse = new ProfileUpdateResponse(new OperationResult(false, "User not found.", "UserNotFound"));
        _serviceMock.Setup(s => s.UpdateProfileAsync("user-1", It.IsAny<ProfileUpdateRequest>())).ReturnsAsync(serviceResponse);

        var result = await _controller.UpdateProfile(new ProfileUpdateRequest(new ProfileDto()));

        Assert.IsInstanceOfType(result.Result, typeof(NotFoundObjectResult));
    }

    [TestMethod]
    public async Task UpdateProfile_WhenServiceReturnsGenericFailure_ReturnsBadRequest()
    {
        SetUserClaims("user-1");
        var serviceResponse = new ProfileUpdateResponse(new OperationResult(false, "Validation failed.", "ValidationError"));
        _serviceMock.Setup(s => s.UpdateProfileAsync("user-1", It.IsAny<ProfileUpdateRequest>())).ReturnsAsync(serviceResponse);

        var result = await _controller.UpdateProfile(new ProfileUpdateRequest(new ProfileDto()));

        Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task UpdateProfile_WhenServiceReturnsSuccess_ReturnsOk()
    {
        SetUserClaims("user-1");
        var updatedProfile = new ProfileDto { FirstName = "Ana" };
        var serviceResponse = new ProfileUpdateResponse(new OperationResult(true, "Profile updated successfully."), updatedProfile);
        _serviceMock.Setup(s => s.UpdateProfileAsync("user-1", It.IsAny<ProfileUpdateRequest>())).ReturnsAsync(serviceResponse);

        var result = await _controller.UpdateProfile(new ProfileUpdateRequest(updatedProfile));

        Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
    }

    // -------------------------------------------------------------------------
    // GetInterestedCandidates
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task GetInterestedCandidates_WhenNoClaimPresent_ReturnsUnauthorized()
    {
        SetNoUserClaims();

        var result = await _controller.GetInterestedCandidates(1);

        Assert.IsInstanceOfType(result.Result, typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task GetInterestedCandidates_WhenClaimPresent_ReturnsOk()
    {
        SetUserClaims("user-1");
        var serviceResponse = new ProfileByTfeInterestResponse(new OperationResult(true, string.Empty), new List<TfeCandidateDto>());
        _serviceMock.Setup(s => s.GetProfileByTfeInterestAsync(It.IsAny<ProfileByTfeInterestRequest>())).ReturnsAsync(serviceResponse);

        var result = await _controller.GetInterestedCandidates(1);

        Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
    }

    // -------------------------------------------------------------------------
    // ChangeRole
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task ChangeRole_WhenUserIdIsWhitespace_ReturnsBadRequest()
    {
        var result = await _controller.ChangeRole("   ", new ChangeRoleRequest(RoleType.Teacher));

        Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task ChangeRole_WhenRequestIsNull_ReturnsBadRequest()
    {
        var result = await _controller.ChangeRole("user-1", null!);

        Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task ChangeRole_WhenRoleIsInvalidEnumValue_ReturnsBadRequest()
    {
        var result = await _controller.ChangeRole("user-1", new ChangeRoleRequest((RoleType)99));

        Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task ChangeRole_WhenServiceReturnsUserNotFound_ReturnsNotFound()
    {
        var serviceResponse = new RoleUpdateResponse(new OperationResult(false, "User not found.", "UserNotFound"));
        _serviceMock.Setup(s => s.UpdateUserRoleAsync("user-99", RoleType.Teacher)).ReturnsAsync(serviceResponse);

        var result = await _controller.ChangeRole("user-99", new ChangeRoleRequest(RoleType.Teacher));

        Assert.IsInstanceOfType(result.Result, typeof(NotFoundObjectResult));
    }

    [TestMethod]
    public async Task ChangeRole_WhenServiceReturnsSuccess_ReturnsOk()
    {
        var serviceResponse = new RoleUpdateResponse(new OperationResult(true, "User role updated successfully."));
        _serviceMock.Setup(s => s.UpdateUserRoleAsync("user-1", RoleType.Admin)).ReturnsAsync(serviceResponse);

        var result = await _controller.ChangeRole("user-1", new ChangeRoleRequest(RoleType.Admin));

        Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
    }

    // -------------------------------------------------------------------------
    // GetAllProfiles
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task GetAllProfiles_WhenCalled_ReturnsOkWithProfiles()
    {
        var profiles = new List<ProfileDto>
        {
            new ProfileDto { FirstName = "Ana" },
            new ProfileDto { FirstName = "Luis" }
        };
        var serviceResponse = new GetAllProfilesResponse(new OperationResult(true, string.Empty), profiles);
        _serviceMock.Setup(s => s.GetAllProfilesAsync(It.IsAny<GetAllProfilesRequest>())).ReturnsAsync(serviceResponse);

        var result = await _controller.GetAllProfiles();

        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        var body = ok.Value as GetAllProfilesResponse;
        Assert.IsNotNull(body);
        Assert.AreEqual(2, body.Profiles.Count);
    }

    // -------------------------------------------------------------------------
    // DeleteProfile
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task DeleteProfile_WhenUserIdIsWhitespace_ReturnsBadRequest()
    {
        var result = await _controller.DeleteProfile("   ");

        Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task DeleteProfile_WhenServiceReturnsUserNotFound_ReturnsNotFound()
    {
        var serviceResponse = new DeleteProfileResponse(new OperationResult(false, "User not found.", "UserNotFound"));
        _serviceMock.Setup(s => s.DeleteProfileAsync("user-99")).ReturnsAsync(serviceResponse);

        var result = await _controller.DeleteProfile("user-99");

        Assert.IsInstanceOfType(result.Result, typeof(NotFoundObjectResult));
    }

    [TestMethod]
    public async Task DeleteProfile_WhenServiceReturnsSuccess_ReturnsOk()
    {
        var serviceResponse = new DeleteProfileResponse(new OperationResult(true, "User profile deleted successfully."));
        _serviceMock.Setup(s => s.DeleteProfileAsync("user-1")).ReturnsAsync(serviceResponse);

        var result = await _controller.DeleteProfile("user-1");

        Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
    }
}
