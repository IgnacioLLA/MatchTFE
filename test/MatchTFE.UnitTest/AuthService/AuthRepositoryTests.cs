using AuthService.Data;
using AuthService.Repositories;
using MatchTFE.AuthService.Repositories;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace MatchTFE.UnitTest.AuthService;

[TestClass]
public class AuthRepositoryTests
{
    private Mock<UserManager<MatchUser>> _userManagerMock = null!;
    private AuthRepository _repo = null!;

    private static MatchUser MakeUser(string id = "u1", string email = "test@test.com") => new()
    {
        Id = id,
        Email = email,
        UserName = email,
        Name = "Test",
        Surname = "User"
    };

    [TestInitialize]
    public void Setup()
    {
        var store = new Mock<IUserStore<MatchUser>>();
        _userManagerMock = new Mock<UserManager<MatchUser>>(
            store.Object, null, null, null, null, null, null, null, null);
        _repo = new AuthRepository(_userManagerMock.Object);
    }

    // -------------------------------------------------------------------------
    // GetUserByEmailAsync / GetUserByIdAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task GetUserByEmailAsync_WhenExists_ReturnsUser()
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.FindByEmailAsync("test@test.com")).ReturnsAsync(user);

        var result = await _repo.GetUserByEmailAsync("test@test.com");

        Assert.IsNotNull(result);
        Assert.AreEqual("u1", result.Id);
    }

    [TestMethod]
    public async Task GetUserByIdAsync_WhenExists_ReturnsUser()
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);

        var result = await _repo.GetUserByIdAsync("u1");

        Assert.IsNotNull(result);
        Assert.AreEqual("test@test.com", result.Email);
    }

    // -------------------------------------------------------------------------
    // CheckPasswordAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task CheckPasswordAsync_ReturnsUserManagerResult(bool expected)
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "pass")).ReturnsAsync(expected);

        var result = await _repo.CheckPasswordAsync(user, "pass");

        Assert.AreEqual(expected, result);
    }

    // -------------------------------------------------------------------------
    // CreateUserAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task CreateUserAsync_WhenSucceeds_ReturnsSuccessResult()
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.CreateAsync(user, "Password1!")).ReturnsAsync(IdentityResult.Success);

        var result = await _repo.CreateUserAsync(user, "Password1!");

        Assert.IsTrue(result.Succeeded);
    }

    [TestMethod]
    public async Task CreateUserAsync_WhenFails_ReturnsFailedResult()
    {
        var user = MakeUser();
        var failure = IdentityResult.Failed(new IdentityError { Code = "DuplicateEmail", Description = "Email already exists." });
        _userManagerMock.Setup(m => m.CreateAsync(user, It.IsAny<string>())).ReturnsAsync(failure);

        var result = await _repo.CreateUserAsync(user, "Password1!");

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("DuplicateEmail", result.Errors.First().Code);
    }

    // -------------------------------------------------------------------------
    // Refresh token
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task GetRefreshTokenAsync_ReturnsStoredToken()
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.GetAuthenticationTokenAsync(user, "MatchTFE", "RefreshToken")).ReturnsAsync("token-abc");

        var result = await _repo.GetRefreshTokenAsync(user);

        Assert.AreEqual("token-abc", result);
    }

    [TestMethod]
    public async Task SaveRefreshTokenAsync_CallsSetAuthenticationToken()
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.SetAuthenticationTokenAsync(user, "MatchTFE", "RefreshToken", "new-token")).ReturnsAsync(IdentityResult.Success);

        await _repo.SaveRefreshTokenAsync(user, "new-token");

        _userManagerMock.Verify(m => m.SetAuthenticationTokenAsync(user, "MatchTFE", "RefreshToken", "new-token"), Times.Once);
    }

    [TestMethod]
    public async Task RemoveRefreshTokenAsync_CallsRemoveAuthenticationToken()
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.RemoveAuthenticationTokenAsync(user, "MatchTFE", "RefreshToken")).ReturnsAsync(IdentityResult.Success);

        await _repo.RemoveRefreshTokenAsync(user);

        _userManagerMock.Verify(m => m.RemoveAuthenticationTokenAsync(user, "MatchTFE", "RefreshToken"), Times.Once);
    }

    // -------------------------------------------------------------------------
    // Roles
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task GetUserRolesAsync_ReturnsRoleList()
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Student" });

        var result = await _repo.GetUserRolesAsync(user);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Student", result[0]);
    }

    [TestMethod]
    public async Task AddToRoleAsync_ReturnsSuccessResult()
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.AddToRoleAsync(user, "Teacher")).ReturnsAsync(IdentityResult.Success);

        var result = await _repo.AddToRoleAsync(user, "Teacher");

        Assert.IsTrue(result.Succeeded);
    }

    // -------------------------------------------------------------------------
    // ResetPasswordDirectlyAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task ResetPasswordDirectlyAsync_WhenRemoveFails_ReturnsFailureWithoutCallingAdd()
    {
        var user = MakeUser();
        var failure = IdentityResult.Failed(new IdentityError { Code = "RemoveFailed" });
        _userManagerMock.Setup(m => m.RemovePasswordAsync(user)).ReturnsAsync(failure);

        var result = await _repo.ResetPasswordDirectlyAsync(user, "NuevoPass1!");

        Assert.IsFalse(result.Succeeded);
        _userManagerMock.Verify(m => m.AddPasswordAsync(It.IsAny<MatchUser>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ResetPasswordDirectlyAsync_WhenRemoveSucceeds_CallsAddPasswordAndReturnsResult()
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.RemovePasswordAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddPasswordAsync(user, "NuevoPass1!")).ReturnsAsync(IdentityResult.Success);

        var result = await _repo.ResetPasswordDirectlyAsync(user, "NuevoPass1!");

        Assert.IsTrue(result.Succeeded);
        _userManagerMock.Verify(m => m.AddPasswordAsync(user, "NuevoPass1!"), Times.Once);
    }

    // -------------------------------------------------------------------------
    // LockUserAsync / UnlockUserAsync / IsLockedOutAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task LockUserAsync_CallsSetLockoutEnabledAndSetLockoutEndDate()
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.SetLockoutEnabledAsync(user, true)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue)).ReturnsAsync(IdentityResult.Success);

        await _repo.LockUserAsync(user);

        _userManagerMock.Verify(m => m.SetLockoutEnabledAsync(user, true), Times.Once);
        _userManagerMock.Verify(m => m.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue), Times.Once);
    }

    [TestMethod]
    public async Task UnlockUserAsync_ClearsLockoutEndDateAndDisablesLockout()
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.SetLockoutEndDateAsync(user, null)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.SetLockoutEnabledAsync(user, false)).ReturnsAsync(IdentityResult.Success);

        await _repo.UnlockUserAsync(user);

        _userManagerMock.Verify(m => m.SetLockoutEndDateAsync(user, null), Times.Once);
        _userManagerMock.Verify(m => m.SetLockoutEnabledAsync(user, false), Times.Once);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task IsLockedOutAsync_ReturnsUserManagerResult(bool expected)
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(expected);

        var result = await _repo.IsLockedOutAsync(user);

        Assert.AreEqual(expected, result);
    }
}
