using Microsoft.EntityFrameworkCore;
using TFELibrary.Data;
using TFELibrary.Shared;
using UserService.Data;
using UserService.Repositories;

namespace MatchTFE.UnitTest.UserService;

[TestClass]
public class UserProfileRepositoryTests
{
    private UserDbContext _context = null!;
    private UserProfileRepository _repo = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new UserDbContext(options);
        _repo = new UserProfileRepository(_context);
    }

    [TestCleanup]
    public void Cleanup() => _context.Dispose();

    // -------------------------------------------------------------------------
    // GetByUserIdAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task GetByUserIdAsync_WhenExists_ReturnsProfile()
    {
        _context.UserProfile.Add(new UserProfile { UserId = "u1", FirstName = "Ana", LastName = "García", Email = "ana@test.com" });
        await _context.SaveChangesAsync();

        var result = await _repo.GetByUserIdAsync("u1");

        Assert.IsNotNull(result);
        Assert.AreEqual("Ana", result.FirstName);
        Assert.AreEqual("García", result.LastName);
    }

    [TestMethod]
    public async Task GetByUserIdAsync_WhenNotExists_ReturnsNull()
    {
        var result = await _repo.GetByUserIdAsync("no-existe");

        Assert.IsNull(result);
    }

    // -------------------------------------------------------------------------
    // GetAllProfilesAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task GetAllProfilesAsync_WhenEmpty_ReturnsEmptyList()
    {
        var result = await _repo.GetAllProfilesAsync();

        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task GetAllProfilesAsync_WhenProfilesExist_ReturnsAll()
    {
        _context.UserProfile.AddRange(
            new UserProfile { UserId = "u1", FirstName = "Ana", LastName = "García", Email = "ana@test.com" },
            new UserProfile { UserId = "u2", FirstName = "Luis", LastName = "Pérez", Email = "luis@test.com" }
        );
        await _context.SaveChangesAsync();

        var result = await _repo.GetAllProfilesAsync();

        Assert.AreEqual(2, result.Count);
    }

    // -------------------------------------------------------------------------
    // CreateProfileAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task CreateProfileAsync_PersistsProfileAndReturnsIt()
    {
        var profile = new UserProfile { UserId = "u3", FirstName = "Marta", LastName = "López", Email = "marta@test.com" };

        var result = await _repo.CreateProfileAsync(profile);

        Assert.AreEqual("u3", result.UserId);
        Assert.AreEqual("Marta", result.FirstName);
        Assert.AreEqual(1, _context.UserProfile.Count());
    }

    // -------------------------------------------------------------------------
    // UpdateUserRoleAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task UpdateUserRoleAsync_WhenExists_UpdatesRoleAndReturnsTrue()
    {
        _context.UserProfile.Add(new UserProfile { UserId = "u1", FirstName = "Ana", LastName = "García", Email = "ana@test.com", Role = RoleType.Student });
        await _context.SaveChangesAsync();

        var result = await _repo.UpdateUserRoleAsync("u1", RoleType.Teacher);

        Assert.IsTrue(result);
        var updated = await _context.UserProfile.FindAsync("u1");
        Assert.AreEqual(RoleType.Teacher, updated!.Role);
    }

    [TestMethod]
    public async Task UpdateUserRoleAsync_WhenNotExists_ReturnsFalse()
    {
        var result = await _repo.UpdateUserRoleAsync("no-existe", RoleType.Teacher);

        Assert.IsFalse(result);
    }

    // -------------------------------------------------------------------------
    // UpdateUserSuspensionAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task UpdateUserSuspensionAsync_WhenExists_UpdatesSuspensionAndReturnsTrue()
    {
        _context.UserProfile.Add(new UserProfile { UserId = "u1", FirstName = "Ana", LastName = "García", Email = "ana@test.com", IsSuspended = false });
        await _context.SaveChangesAsync();

        var result = await _repo.UpdateUserSuspensionAsync("u1", true);

        Assert.IsTrue(result);
        var updated = await _context.UserProfile.FindAsync("u1");
        Assert.IsTrue(updated!.IsSuspended);
    }

    [TestMethod]
    public async Task UpdateUserSuspensionAsync_WhenNotExists_ReturnsFalse()
    {
        var result = await _repo.UpdateUserSuspensionAsync("no-existe", true);

        Assert.IsFalse(result);
    }
}
