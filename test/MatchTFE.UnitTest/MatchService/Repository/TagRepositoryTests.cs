using MatchService.Data;
using MatchService.Repositories;
using Microsoft.EntityFrameworkCore;
using TFELibrary.Data;

namespace MatchTFE.UnitTest.MatchService.Repository;

[TestClass]
public class TagRepositoryTests
{
    private MatchDbContext _context = null!;
    private TagRepository _repo = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<MatchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new MatchDbContext(options);
        _repo = new TagRepository(_context);
    }

    [TestCleanup]
    public void Cleanup() => _context.Dispose();

    // -------------------------------------------------------------------------
    // GetAllAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        var result = await _repo.GetAllAsync();

        Assert.AreEqual(0, result.Count());
    }

    [TestMethod]
    public async Task GetAllAsync_WhenTagsExist_ReturnsAll()
    {
        _context.Tag.AddRange(
            new Tag { Id = 1, Name = "C#" },
            new Tag { Id = 2, Name = "Python" }
        );
        await _context.SaveChangesAsync();

        var result = await _repo.GetAllAsync();

        Assert.AreEqual(2, result.Count());
    }

    // -------------------------------------------------------------------------
    // GetByIdAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task GetByIdAsync_WhenExists_ReturnsTag()
    {
        _context.Tag.Add(new Tag { Id = 1, Name = "C#" });
        await _context.SaveChangesAsync();

        var result = await _repo.GetByIdAsync(1);

        Assert.IsNotNull(result);
        Assert.AreEqual("C#", result.Name);
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
    // GetByNameAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task GetByNameAsync_WhenExists_ReturnsTag()
    {
        _context.Tag.Add(new Tag { Id = 1, Name = "Machine Learning" });
        await _context.SaveChangesAsync();

        var result = await _repo.GetByNameAsync("Machine Learning");

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("NoExiste")]
    public async Task GetByNameAsync_WhenNotExists_ReturnsNull(string name)
    {
        var result = await _repo.GetByNameAsync(name);

        Assert.IsNull(result);
    }

    // -------------------------------------------------------------------------
    // CreateAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task CreateAsync_PersistsTagAndReturnsIt()
    {
        var tag = new Tag { Id = 5, Name = "Docker" };

        var result = await _repo.CreateAsync(tag);

        Assert.AreEqual("Docker", result.Name);
        Assert.AreEqual(1, _context.Tag.Count());
    }

    // -------------------------------------------------------------------------
    // UpdateAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task UpdateAsync_ChangesNameAndReturnsUpdatedTag()
    {
        _context.Tag.Add(new Tag { Id = 1, Name = "Viejo" });
        await _context.SaveChangesAsync();

        var tag = await _context.Tag.FindAsync(1);
        tag!.Name = "Nuevo";
        var result = await _repo.UpdateAsync(tag);

        Assert.AreEqual("Nuevo", result.Name);
        Assert.AreEqual("Nuevo", (await _context.Tag.FindAsync(1))!.Name);
    }

    // -------------------------------------------------------------------------
    // DeleteAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task DeleteAsync_RemovesTagFromDatabase()
    {
        var tag = new Tag { Id = 1, Name = "Temporal" };
        _context.Tag.Add(tag);
        await _context.SaveChangesAsync();

        await _repo.DeleteAsync(tag);

        Assert.AreEqual(0, _context.Tag.Count());
    }
}
