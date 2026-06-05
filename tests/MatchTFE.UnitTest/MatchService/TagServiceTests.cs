using MatchService.Repositories;
using MatchService.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using TFELibrary.Data;
using TFELibrary.Shared;

namespace MatchTFE.UnitTest.MatchService;

[TestClass]
public class TagServiceTests
{
    private Mock<ITagRepository> _tagRepoMock = null!;
    private TagService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _tagRepoMock = new Mock<ITagRepository>();
        _service     = new TagService(_tagRepoMock.Object);
    }

    // =========================================================================
    // GetAllTagsAsync
    // =========================================================================

    [TestMethod]
    public async Task GetAllTagsAsync_WhenNoTags_ReturnsEmptyEnumerable()
    {
        _tagRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(Enumerable.Empty<Tag>());

        var result = await _service.GetAllTagsAsync();

        Assert.IsFalse(result.Any());
    }

    [TestMethod]
    public async Task GetAllTagsAsync_WhenTagsExist_ReturnsMappedDtos()
    {
        _tagRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Tag>
        {
            new Tag { Id = 1, Name = "AI" },
            new Tag { Id = 2, Name = "Backend" }
        });

        var result = (await _service.GetAllTagsAsync()).ToList();

        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.Any(t => t.Id == 1 && t.Name == "AI"));
        Assert.IsTrue(result.Any(t => t.Id == 2 && t.Name == "Backend"));
    }

    // =========================================================================
    // GetTagByIdAsync
    // =========================================================================

    [TestMethod]
    public async Task GetTagByIdAsync_WhenTagNotFound_ReturnsNull()
    {
        _tagRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Tag?)null);

        var result = await _service.GetTagByIdAsync(99);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetTagByIdAsync_WhenTagFound_ReturnsMappedDto()
    {
        _tagRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Tag { Id = 1, Name = "AI" });

        var result = await _service.GetTagByIdAsync(1);

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("AI", result.Name);
    }

    // =========================================================================
    // CreateTagAsync
    // =========================================================================

    [TestMethod]
    public async Task CreateTagAsync_WhenNameIsEmpty_ThrowsArgumentException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _service.CreateTagAsync(new TagCreationRequest { Tag = new TagDto { Name = "" } }));
    }

    [TestMethod]
    public async Task CreateTagAsync_WhenNameIsWhitespace_ThrowsArgumentException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _service.CreateTagAsync(new TagCreationRequest { Tag = new TagDto { Name = "   " } }));
    }

    [TestMethod]
    public async Task CreateTagAsync_WhenDuplicateName_ThrowsInvalidOperationException()
    {
        _tagRepoMock.Setup(r => r.CreateAsync(It.IsAny<Tag>()))
            .ThrowsAsync(new DbUpdateException("Unique constraint", new Exception()));

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => _service.CreateTagAsync(new TagCreationRequest { Tag = new TagDto { Name = "AI" } }));

        StringAssert.Contains(ex.Message, "AI");
    }

    [TestMethod]
    public async Task CreateTagAsync_WhenValid_ReturnsSuccessWithTagId()
    {
        _tagRepoMock.Setup(r => r.CreateAsync(It.IsAny<Tag>()))
            .ReturnsAsync(new Tag { Id = 5, Name = "AI" });

        var result = await _service.CreateTagAsync(new TagCreationRequest { Tag = new TagDto { Name = "AI" } });

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.AreEqual(5, result.TagId);
        Assert.AreEqual("AI", result.Tag.Name);
        Assert.AreEqual(5, result.Tag.Id);
    }

    // =========================================================================
    // UpdateTagAsync
    // =========================================================================

    [TestMethod]
    public async Task UpdateTagAsync_WhenNameIsEmpty_ThrowsArgumentException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _service.UpdateTagAsync(1, new TagUpdateRequest { Name = "" }));
    }

    [TestMethod]
    public async Task UpdateTagAsync_WhenTagNotFound_ReturnsFalse()
    {
        _tagRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Tag?)null);

        var result = await _service.UpdateTagAsync(99, new TagUpdateRequest { Name = "NewName" });

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task UpdateTagAsync_WhenDuplicateName_ThrowsInvalidOperationException()
    {
        _tagRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Tag { Id = 1, Name = "OldName" });
        _tagRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Tag>()))
            .ThrowsAsync(new DbUpdateException("Unique constraint", new Exception()));

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => _service.UpdateTagAsync(1, new TagUpdateRequest { Name = "ExistingName" }));

        StringAssert.Contains(ex.Message, "ExistingName");
    }

    [TestMethod]
    public async Task UpdateTagAsync_WhenValid_UpdatesNameAndReturnsTrue()
    {
        var tag = new Tag { Id = 1, Name = "OldName" };
        _tagRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tag);
        _tagRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Tag>())).ReturnsAsync(tag);

        var result = await _service.UpdateTagAsync(1, new TagUpdateRequest { Name = "NewName" });

        Assert.IsTrue(result);
        _tagRepoMock.Verify(r => r.UpdateAsync(It.Is<Tag>(t => t.Name == "NewName")), Times.Once);
    }

    // =========================================================================
    // DeleteTagAsync
    // =========================================================================

    [TestMethod]
    public async Task DeleteTagAsync_WhenTagNotFound_ReturnsFalse()
    {
        _tagRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Tag?)null);

        var result = await _service.DeleteTagAsync(99);

        Assert.IsFalse(result);
        _tagRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Tag>()), Times.Never);
    }

    [TestMethod]
    public async Task DeleteTagAsync_WhenTagFound_DeletesAndReturnsTrue()
    {
        var tag = new Tag { Id = 1, Name = "AI" };
        _tagRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tag);
        _tagRepoMock.Setup(r => r.DeleteAsync(tag)).Returns(Task.CompletedTask);

        var result = await _service.DeleteTagAsync(1);

        Assert.IsTrue(result);
        _tagRepoMock.Verify(r => r.DeleteAsync(tag), Times.Once);
    }
}
