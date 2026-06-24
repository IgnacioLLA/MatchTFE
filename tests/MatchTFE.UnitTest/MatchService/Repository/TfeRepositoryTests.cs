using MatchService.Data;
using MatchService.Repositories;
using Microsoft.EntityFrameworkCore;
using TFELibrary.Data;
using TFELibrary.Shared;

namespace MatchTFE.UnitTest.MatchService.Repository;

[TestClass]
public class TfeRepositoryTests
{
    private MatchDbContext _context = null!;
    private TfeRepository _repo = null!;

    private static UserProfile MakeAuthor(string id) => new()
    {
        UserId = id,
        FirstName = "Tutor",
        LastName = "Test",
        Email = $"{id}@test.com",
        Role = RoleType.Teacher
    };

    private static TFE MakeTfe(int id, string authorId, string title = "TFE Test", TfeStatus status = TfeStatus.Open) => new()
    {
        Id = id,
        AuthorId = authorId,
        Title = title,
        Status = status,
        ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        CreationDate = DateOnly.FromDateTime(DateTime.UtcNow)
    };

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<MatchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new MatchDbContext(options);
        _repo = new TfeRepository(_context);
    }

    [TestCleanup]
    public void Cleanup() => _context.Dispose();

    // -------------------------------------------------------------------------
    // GetByIdAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task GetByIdAsync_WhenExists_ReturnsTfeWithAuthor()
    {
        var author = MakeAuthor("author-1");
        _context.UserProfiles.Add(author);
        _context.Tfe.Add(MakeTfe(1, "author-1", "Mi TFE"));
        await _context.SaveChangesAsync();

        var result = await _repo.GetByIdAsync(1);

        Assert.IsNotNull(result);
        Assert.AreEqual("Mi TFE", result.Title);
        Assert.IsNotNull(result.Author);
        Assert.AreEqual("author-1", result.Author.UserId);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(999)]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull(int id)
    {
        var result = await _repo.GetByIdAsync(id);

        Assert.IsNull(result);
    }

    // -------------------------------------------------------------------------
    // CreateAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task CreateAsync_PersistsTfeAndReturnsWithId()
    {
        _context.UserProfiles.Add(MakeAuthor("author-1"));
        await _context.SaveChangesAsync();

        var tfe = new TFE
        {
            AuthorId = "author-1",
            Title = "Nuevo TFE",
            ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)),
            CreationDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var result = await _repo.CreateAsync(tfe);

        Assert.IsTrue(result.Id > 0);
        Assert.AreEqual("Nuevo TFE", result.Title);
        Assert.AreEqual(1, _context.Tfe.Count());
    }

    // -------------------------------------------------------------------------
    // GetByAuthorIdAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task GetByAuthorIdAsync_ReturnsOnlyTfesOfThatAuthor()
    {
        _context.UserProfiles.AddRange(MakeAuthor("author-1"), MakeAuthor("author-2"));
        _context.Tfe.AddRange(
            MakeTfe(1, "author-1", "TFE de autor 1"),
            MakeTfe(2, "author-2", "TFE de autor 2")
        );
        await _context.SaveChangesAsync();

        var result = await _repo.GetByAuthorIdAsync("author-1");

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("TFE de autor 1", result[0].Title);
    }

    [TestMethod]
    public async Task GetByAuthorIdAsync_WhenNone_ReturnsEmptyList()
    {
        var result = await _repo.GetByAuthorIdAsync("author-inexistente");

        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task GetByAuthorIdAsync_ExcludesCancelledTfes()
    {
        _context.UserProfiles.Add(MakeAuthor("author-1"));
        _context.Tfe.AddRange(
            MakeTfe(1, "author-1", "Activo", TfeStatus.Open),
            MakeTfe(2, "author-1", "Cancelado", TfeStatus.Cancelled)
        );
        await _context.SaveChangesAsync();

        var result = await _repo.GetByAuthorIdAsync("author-1");

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Activo", result[0].Title);
    }

    // -------------------------------------------------------------------------
    // DeleteAsync(int id)
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task DeleteAsync_WhenExists_ReturnsTrueAndRemovesTfe()
    {
        _context.UserProfiles.Add(MakeAuthor("author-1"));
        _context.Tfe.Add(MakeTfe(1, "author-1"));
        await _context.SaveChangesAsync();

        var result = await _repo.DeleteAsync(1);

        Assert.IsTrue(result);
        Assert.AreEqual(0, _context.Tfe.Count());
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(999)]
    public async Task DeleteAsync_WhenNotExists_ReturnsFalse(int id)
    {
        var result = await _repo.DeleteAsync(id);

        Assert.IsFalse(result);
    }

    // -------------------------------------------------------------------------
    // UpdateStatusAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task UpdateStatusAsync_WhenExists_ChangesStatus()
    {
        _context.UserProfiles.Add(MakeAuthor("author-1"));
        _context.Tfe.Add(MakeTfe(1, "author-1"));
        await _context.SaveChangesAsync();

        await _repo.UpdateStatusAsync(1, TfeStatus.Completed);

        var updated = await _context.Tfe.FindAsync(1);
        Assert.AreEqual(TfeStatus.Completed, updated!.Status);
    }
}
