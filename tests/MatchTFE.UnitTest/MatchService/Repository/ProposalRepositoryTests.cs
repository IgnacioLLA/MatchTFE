using MatchService.Data;
using MatchService.Repositories;
using Microsoft.EntityFrameworkCore;
using TFELibrary.Data;
using TFELibrary.Shared;

namespace MatchTFE.UnitTest.MatchService.Repository;

[TestClass]
public class ProposalRepositoryTests
{
    private MatchDbContext _context = null!;
    private ProposalRepository _repo = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<MatchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new MatchDbContext(options);
        _repo = new ProposalRepository(_context);
    }

    [TestCleanup]
    public void Cleanup() => _context.Dispose();

    private async Task SeedAsync(string studentId = "student-1", int tfeId = 1)
    {
        _context.UserProfiles.AddRange(
            new UserProfile { UserId = "tutor-1", FirstName = "Tutor", LastName = "Test", Email = "tutor@test.com", Role = RoleType.Teacher },
            new UserProfile { UserId = studentId, FirstName = "Alumno", LastName = "Test", Email = $"{studentId}@test.com", Role = RoleType.Student }
        );
        _context.Tfe.Add(new TFE
        {
            Id = tfeId,
            AuthorId = "tutor-1",
            Title = "TFE Test",
            ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            CreationDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        await _context.SaveChangesAsync();
    }

    // -------------------------------------------------------------------------
    // TfeProposalExistsAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task TfeProposalExistsAsync_WhenNotExists_ReturnsFalse()
    {
        var result = await _repo.TfeProposalExistsAsync("student-1", 1);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task TfeProposalExistsAsync_WhenExists_ReturnsTrue()
    {
        await SeedAsync();
        _context.TfeProposal.Add(new TFEProposal { OriginUserId = "student-1", TfeId = 1 });
        await _context.SaveChangesAsync();

        var result = await _repo.TfeProposalExistsAsync("student-1", 1);

        Assert.IsTrue(result);
    }

    // -------------------------------------------------------------------------
    // GetTfeProposalByUserIdAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task GetTfeProposalByUserIdAsync_WhenNotExists_ReturnsNull()
    {
        var result = await _repo.GetTfeProposalByUserIdAsync("student-1", 1);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetTfeProposalByUserIdAsync_WhenExists_ReturnsProposal()
    {
        await SeedAsync();
        _context.TfeProposal.Add(new TFEProposal { OriginUserId = "student-1", TfeId = 1, Status = ProposalStatus.Pending });
        await _context.SaveChangesAsync();

        var result = await _repo.GetTfeProposalByUserIdAsync("student-1", 1);

        Assert.IsNotNull(result);
        Assert.AreEqual(ProposalStatus.Pending, result.Status);
        Assert.AreEqual("student-1", result.OriginUserId);
    }

    // -------------------------------------------------------------------------
    // CreateTfeProposalAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task CreateTfeProposalAsync_PersistsProposal()
    {
        await SeedAsync();
        var proposal = new TFEProposal { OriginUserId = "student-1", TfeId = 1 };

        await _repo.CreateTfeProposalAsync(proposal);

        Assert.AreEqual(1, _context.TfeProposal.Count());
    }

    // -------------------------------------------------------------------------
    // GetInterestedCountsByTfeIdsAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task GetInterestedCountsByTfeIdsAsync_WhenEmptyList_ReturnsEmptyDictionary()
    {
        var result = await _repo.GetInterestedCountsByTfeIdsAsync(new List<int>());

        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task GetInterestedCountsByTfeIdsAsync_CountsOnlyPendingProposals()
    {
        await SeedAsync();
        _context.TfeProposal.AddRange(
            new TFEProposal { OriginUserId = "student-1", TfeId = 1, Status = ProposalStatus.Pending },
            new TFEProposal { OriginUserId = "tutor-1", TfeId = 1, Status = ProposalStatus.NotInterested }
        );
        await _context.SaveChangesAsync();

        var result = await _repo.GetInterestedCountsByTfeIdsAsync(new List<int> { 1 });

        Assert.IsTrue(result.ContainsKey(1));
        Assert.AreEqual(1, result[1]);
    }

    // -------------------------------------------------------------------------
    // UpdateTfeProposalAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task UpdateTfeProposalAsync_ChangesStatus()
    {
        await SeedAsync();
        _context.TfeProposal.Add(new TFEProposal { OriginUserId = "student-1", TfeId = 1, Status = ProposalStatus.Pending });
        await _context.SaveChangesAsync();

        var proposal = await _context.TfeProposal.FindAsync("student-1", 1);
        proposal!.Status = ProposalStatus.Accepted;
        await _repo.UpdateTfeProposalAsync(proposal);

        var updated = await _context.TfeProposal.FindAsync("student-1", 1);
        Assert.AreEqual(ProposalStatus.Accepted, updated!.Status);
    }
}
