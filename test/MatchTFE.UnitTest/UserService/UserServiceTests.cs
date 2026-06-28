using Microsoft.Extensions.Logging;
using Moq;
using TFELibrary.Data;
using TFELibrary.Shared;
using UserService.Repositories;

namespace MatchTFE.UnitTest.UserService;

[TestClass]
public class UserServiceTests
{
    private Mock<IUserProfileRepository> _repositoryMock = null!;
    private Mock<ILogger<global::UserService.Service.UserService>> _loggerMock = null!;
    private global::UserService.Service.UserService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IUserProfileRepository>();
        _loggerMock = new Mock<ILogger<global::UserService.Service.UserService>>();
        _service = new global::UserService.Service.UserService(_repositoryMock.Object, _loggerMock.Object);
    }

    // -------------------------------------------------------------------------
    // GetProfileByUserIdAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    [DataRow(null)]
    [DataRow("   ")]
    public async Task GetProfileByUserIdAsync_WhenUserIdIsInvalid_ReturnsError(string? userId)
    {
        var result = await _service.GetProfileByUserIdAsync(userId!);

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.IsNull(result.Profile);
    }

    [TestMethod]
    public async Task GetProfileByUserIdAsync_WhenUserNotFound_ReturnsUserNotFoundError()
    {
        _repositoryMock.Setup(r => r.GetByUserIdAsync("user-99"))
            .ReturnsAsync((UserProfile?)null);

        var result = await _service.GetProfileByUserIdAsync("user-99");

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("UserNotFound", result.Error.ErrorCode);
        Assert.IsNull(result.Profile);
    }

    [TestMethod]
    public async Task GetProfileByUserIdAsync_WhenUserExists_ReturnsMappedProfile()
    {
        var entity = new UserProfile
        {
            UserId = "user-1",
            FirstName = "Ana",
            LastName = "García",
            Email = "ana@example.com",
            Role = RoleType.Student,
        };
        _repositoryMock.Setup(r => r.GetByUserIdAsync("user-1")).ReturnsAsync(entity);

        var result = await _service.GetProfileByUserIdAsync("user-1");

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.IsNotNull(result.Profile);
        Assert.AreEqual("user-1", result.Profile.Id);
        Assert.AreEqual("Ana", result.Profile.FirstName);
        Assert.AreEqual("García", result.Profile.LastName);
        Assert.AreEqual("ana@example.com", result.Profile.Email);
        Assert.AreEqual(RoleType.Student, result.Profile.Role);
    }

    // -------------------------------------------------------------------------
    // CreateProfileAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task CreateProfileAsync_WhenRequestIsNull_ReturnsError()
    {
        var result = await _service.CreateProfileAsync(null!);

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("Request payload cannot be null.", result.Error.Message);
    }

    [TestMethod]
    public async Task CreateProfileAsync_WhenProfileIsNull_ReturnsError()
    {
        var request = new ProfileCreationRequest("user-1", null!);

        var result = await _service.CreateProfileAsync(request);

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("Profile data is required.", result.Error.Message);
    }

    [TestMethod]
    [DataRow("", "Lopez")]
    [DataRow("Ignacio", "  ")]
    public async Task CreateProfileAsync_WhenNameIsBlank_ReturnsError(string firstName, string lastName)
    {
        var request = new ProfileCreationRequest("user-1",
            new ProfileDto { FirstName = firstName, LastName = lastName, Email = "test@example.com" });

        var result = await _service.CreateProfileAsync(request);

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("First name and last name are required.", result.Error.Message);
    }

    [TestMethod]
    public async Task CreateProfileAsync_WhenEmailIsBlank_ReturnsError()
    {
        var request = new ProfileCreationRequest("user-1",
            new ProfileDto { FirstName = "Ignacio", LastName = "Lopez", Email = "" });

        var result = await _service.CreateProfileAsync(request);

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("Email is required.", result.Error.Message);
    }

    [TestMethod]
    public async Task CreateProfileAsync_WhenEmailFormatIsInvalid_ReturnsInvalidEmailError()
    {
        var request = new ProfileCreationRequest("user-1",
            new ProfileDto { FirstName = "Ignacio", LastName = "Lopez", Email = "not-an-email" });

        var result = await _service.CreateProfileAsync(request);

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("Invalid email format.", result.Error.Message);
        Assert.AreEqual("InvalidEmail", result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task CreateProfileAsync_WhenUserIdIsEmpty_ReturnsError()
    {
        var request = new ProfileCreationRequest("",
            new ProfileDto { FirstName = "Ignacio", LastName = "Lopez", Email = "ignacio@example.com" });

        var result = await _service.CreateProfileAsync(request);

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("User ID is required.", result.Error.Message);
    }

    [TestMethod]
    public async Task CreateProfileAsync_WhenRequestIsValid_CreatesProfileAndReturnsSuccess()
    {
        _repositoryMock
            .Setup(r => r.CreateProfileAsync(It.IsAny<UserProfile>()))
            .ReturnsAsync((UserProfile p) => p);

        var request = new ProfileCreationRequest(
            "user-1",
            new ProfileDto { FirstName = "Ignacio", LastName = "Lopez", Email = "ignacio@example.com" });

        var result = await _service.CreateProfileAsync(request);

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.AreEqual("Profile created successfully.", result.Error.Message);
        Assert.AreEqual("user-1", result.UserId);

        _repositoryMock.Verify(r => r.CreateProfileAsync(It.Is<UserProfile>(p =>
            p.UserId == "user-1" &&
            p.FirstName == "Ignacio" &&
            p.LastName == "Lopez" &&
            p.Email == "ignacio@example.com"
        )), Times.Once);
    }

    // -------------------------------------------------------------------------
    // UpdateProfileAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task UpdateProfileAsync_WhenUserIdIsBlank_ReturnsError()
    {
        var result = await _service.UpdateProfileAsync("", new ProfileUpdateRequest(new ProfileDto()));

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("User ID is required.", result.Error.Message);
    }

    [TestMethod]
    public async Task UpdateProfileAsync_WhenProfileIsNull_ReturnsError()
    {
        var result = await _service.UpdateProfileAsync("user-1", new ProfileUpdateRequest(null!));

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("Profile data cannot be empty.", result.Error.Message);
    }

    [TestMethod]
    public async Task UpdateProfileAsync_WhenUserNotFound_ReturnsUserNotFoundError()
    {
        _repositoryMock
            .Setup(r => r.UpdateUserProfileAsync(
                It.IsAny<UserProfile>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<SkillDto>>()))
            .ReturnsAsync(false);

        var result = await _service.UpdateProfileAsync("user-99",
            new ProfileUpdateRequest(new ProfileDto { FirstName = "Test" }));

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("UserNotFound", result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task UpdateProfileAsync_WhenSuccessful_ReturnsUpdatedProfile()
    {
        var profileDto = new ProfileDto { FirstName = "Ana", LastName = "García", Email = "ana@example.com" };

        _repositoryMock
            .Setup(r => r.UpdateUserProfileAsync(
                It.IsAny<UserProfile>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<SkillDto>>()))
            .ReturnsAsync(true);

        var result = await _service.UpdateProfileAsync("user-1", new ProfileUpdateRequest(profileDto));

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.AreEqual("Profile updated successfully.", result.Error.Message);
        Assert.IsNotNull(result.UpdatedProfile);
        Assert.AreEqual("Ana", result.UpdatedProfile.FirstName);
    }

    [TestMethod]
    public async Task UpdateProfileAsync_WhenRepositoryThrows_ReturnsDatabaseError()
    {
        _repositoryMock
            .Setup(r => r.UpdateUserProfileAsync(
                It.IsAny<UserProfile>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<SkillDto>>()))
            .ThrowsAsync(new Exception("Simulated DB failure"));

        var result = await _service.UpdateProfileAsync("user-1",
            new ProfileUpdateRequest(new ProfileDto { FirstName = "Test" }));

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("DatabaseError", result.Error.ErrorCode);
    }

    // -------------------------------------------------------------------------
    // UpdateUserRoleAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task UpdateUserRoleAsync_WhenUserIdIsBlank_ReturnsError()
    {
        var result = await _service.UpdateUserRoleAsync("", RoleType.Teacher);

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("User ID is required.", result.Error.Message);
    }

    [TestMethod]
    public async Task UpdateUserRoleAsync_WhenUserNotFound_ReturnsUserNotFoundError()
    {
        _repositoryMock.Setup(r => r.UpdateUserRoleAsync("user-99", RoleType.Teacher))
            .ReturnsAsync(false);

        var result = await _service.UpdateUserRoleAsync("user-99", RoleType.Teacher);

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("UserNotFound", result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task UpdateUserRoleAsync_WhenSuccessful_ReturnsSuccess()
    {
        _repositoryMock.Setup(r => r.UpdateUserRoleAsync("user-1", RoleType.Admin))
            .ReturnsAsync(true);

        var result = await _service.UpdateUserRoleAsync("user-1", RoleType.Admin);

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.AreEqual("User role updated successfully.", result.Error.Message);
    }

    [TestMethod]
    public async Task UpdateUserRoleAsync_WhenRepositoryThrows_ReturnsDatabaseError()
    {
        _repositoryMock
            .Setup(r => r.UpdateUserRoleAsync(It.IsAny<string>(), It.IsAny<RoleType>()))
            .ThrowsAsync(new Exception("Simulated DB failure"));

        var result = await _service.UpdateUserRoleAsync("user-1", RoleType.Admin);

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("DatabaseError", result.Error.ErrorCode);
    }

    // -------------------------------------------------------------------------
    // GetProfileByTfeInterestAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task GetProfileByTfeInterestAsync_WhenRequestIsNull_ReturnsEmptyList()
    {
        var result = await _service.GetProfileByTfeInterestAsync(null!);

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Interested.Count);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-5)]
    public async Task GetProfileByTfeInterestAsync_WhenTfeIdIsInvalid_ReturnsEmptyList(int tfeId)
    {
        var result = await _service.GetProfileByTfeInterestAsync(new ProfileByTfeInterestRequest(tfeId));

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Interested.Count);
    }

    [TestMethod]
    public async Task GetProfileByTfeInterestAsync_WhenRepositoryReturnsEmpty_ReturnsEmptyList()
    {
        _repositoryMock
            .Setup(r => r.GetInterestedUsersByTfeIdInUserServiceAsync(1))
            .ReturnsAsync(new List<UserProfile>());

        var result = await _service.GetProfileByTfeInterestAsync(new ProfileByTfeInterestRequest(1));

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Interested.Count);
    }

    [TestMethod]
    public async Task GetProfileByTfeInterestAsync_WhenUsersFound_ReturnsMappedCandidates()
    {
        var user = new UserProfile
        {
            UserId = "user-1",
            FirstName = "Ignacio",
            LastName = "Lopez",
            Email = "ignacio@example.com",
            TfeProposals = new List<TFEProposal>
            {
                new TFEProposal { TfeId = 5, Status = ProposalStatus.Pending }
            }
        };
        _repositoryMock
            .Setup(r => r.GetInterestedUsersByTfeIdInUserServiceAsync(5))
            .ReturnsAsync(new List<UserProfile> { user });

        var result = await _service.GetProfileByTfeInterestAsync(new ProfileByTfeInterestRequest(5));

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Interested.Count);
        Assert.AreEqual("user-1", result.Interested[0].Profile.Id);
        Assert.AreEqual(ProposalStatus.Pending, result.Interested[0].Status);
    }

    [TestMethod]
    public async Task GetProfileByTfeInterestAsync_WhenUserHasNoMatchingProposal_CandidateIsFiltered()
    {
        var user = new UserProfile
        {
            UserId = "user-1",
            TfeProposals = new List<TFEProposal>
            {
                new TFEProposal { TfeId = 99, Status = ProposalStatus.Pending }
            }
        };
        _repositoryMock
            .Setup(r => r.GetInterestedUsersByTfeIdInUserServiceAsync(5))
            .ReturnsAsync(new List<UserProfile> { user });

        var result = await _service.GetProfileByTfeInterestAsync(new ProfileByTfeInterestRequest(5));

        Assert.AreEqual(0, result.Interested.Count);
    }

    // -------------------------------------------------------------------------
    // GetAllProfilesAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task GetAllProfilesAsync_WhenNoProfiles_ReturnsEmptyList()
    {
        _repositoryMock.Setup(r => r.GetAllProfilesAsync())
            .ReturnsAsync(new List<UserProfile>());

        var result = await _service.GetAllProfilesAsync(new GetAllProfilesRequest());

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Profiles.Count);
    }

    [TestMethod]
    public async Task GetAllProfilesAsync_WhenProfilesExist_ReturnsMappedList()
    {
        var entities = new List<UserProfile>
        {
            new UserProfile { UserId = "user-1", FirstName = "Ana", LastName = "García", Email = "ana@example.com" },
            new UserProfile { UserId = "user-2", FirstName = "Luis", LastName = "Martínez", Email = "luis@example.com" }
        };
        _repositoryMock.Setup(r => r.GetAllProfilesAsync()).ReturnsAsync(entities);

        var result = await _service.GetAllProfilesAsync(new GetAllProfilesRequest());

        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Profiles.Count);

        var anaProfile = result.Profiles.FirstOrDefault(p => p.Id == "user-1");
        Assert.IsNotNull(anaProfile);
        Assert.AreEqual("Ana", anaProfile.FirstName);

        var luisProfile = result.Profiles.FirstOrDefault(p => p.Id == "user-2");
        Assert.IsNotNull(luisProfile);
        Assert.AreEqual("Luis", luisProfile.FirstName);
    }
}
