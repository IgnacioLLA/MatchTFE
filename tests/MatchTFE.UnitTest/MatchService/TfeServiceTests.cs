using MatchService.Repositories;
using MatchService.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using TFELibrary.Data;
using TFELibrary.Shared;

namespace MatchTFE.UnitTest.MatchService;

[TestClass]
public class TfeServiceTests
{
    private Mock<ITfeRepository> _tfeRepoMock = null!;
    private Mock<ITagRepository> _tagRepoMock = null!;
    private Mock<IProposalRepository> _proposalRepoMock = null!;
    private TfeService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _tfeRepoMock = new Mock<ITfeRepository>();
        _tagRepoMock = new Mock<ITagRepository>();
        _proposalRepoMock = new Mock<IProposalRepository>();
        _service = new TfeService(_tfeRepoMock.Object, _tagRepoMock.Object, _proposalRepoMock.Object);

        // MapTagsAndSkillsAsync calls GetByNamesAsync (bulk). Default: empty dictionary.
        _tagRepoMock.Setup(r => r.GetByNamesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, Tag>());
    }

    // -- helpers --

    private static TfeCreationRequest CreateValidRequest(
        string title = "Valid Title",
        string description = "Valid Description",
        DateTime? expDate = null) => new TfeCreationRequest
        {
            Tfe = new TfeDto
            {
                Title = title,
                Description = description,
                ExpirationDate = expDate ?? DateTime.Today.AddDays(2),
                EstimatedDelivery = DateTime.Today.AddDays(30),
                Topics = new List<TagDto>(),
                RequiredSkills = new List<SkillDto>()
            }
        };

    private static TFE CreateTfeEntity(int id, string authorId, DateOnly? expirationDate = null) => new TFE
    {
        Id = id,
        AuthorId = authorId,
        Title = "TFE Title",
        Description = "TFE Description",
        ExpirationDate = expirationDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
        EstimatedDelivery = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
        CreationDate = DateOnly.FromDateTime(DateTime.Today),
        Status = TfeStatus.Open,
        Author = new UserProfile { FirstName = "Author", LastName = "Name" },
        Topics = new List<Tag>(),
        RequiredSkills = new List<TfeRequiredSkill>()
    };

    private void SetupCreateAsyncReturnsWithId(int id = 1) =>
        _tfeRepoMock.Setup(r => r.CreateAsync(It.IsAny<TFE>()))
            .ReturnsAsync((TFE t) =>
            {
                t.Id = id;
                t.Author = new UserProfile { FirstName = "Author", LastName = "Name" };
                return t;
            });

    // =========================================================================
    // CreateTfeAsync
    // =========================================================================

    [TestMethod]
    public async Task CreateTfeAsync_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => _service.CreateTfeAsync(null!, "author-1"));
    }

    [TestMethod]
    public async Task CreateTfeAsync_WhenTfeIsNull_ThrowsArgumentNullException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => _service.CreateTfeAsync(new TfeCreationRequest { Tfe = null! }, "author-1"));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public async Task CreateTfeAsync_WhenTitleIsInvalid_ThrowsArgumentException(string title)
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _service.CreateTfeAsync(CreateValidRequest(title: title), "author-1"));
    }

    [TestMethod]
    public async Task CreateTfeAsync_WhenDescriptionIsEmpty_ThrowsArgumentException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _service.CreateTfeAsync(CreateValidRequest(description: ""), "author-1"));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-5)]
    public async Task CreateTfeAsync_WhenExpirationDateIsInvalid_ThrowsArgumentException(int offsetDays)
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _service.CreateTfeAsync(CreateValidRequest(expDate: DateTime.Today.AddDays(offsetDays)), "author-1"));
    }

    [TestMethod]
    public async Task CreateTfeAsync_WhenTopicTagNotFound_ThrowsArgumentException()
    {
        var request = CreateValidRequest();
        request.Tfe.Topics = new List<TagDto> { new TagDto { Name = "UnknownTag" } };

        var ex = await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _service.CreateTfeAsync(request, "author-1"));

        StringAssert.Contains(ex.Message, "UnknownTag");
    }

    [TestMethod]
    public async Task CreateTfeAsync_WhenSkillTagNotFound_ThrowsArgumentException()
    {
        var request = CreateValidRequest();
        request.Tfe.RequiredSkills = new List<SkillDto> { new SkillDto { Tag = "Python", Level = 3 } };

        var ex = await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _service.CreateTfeAsync(request, "author-1"));

        StringAssert.Contains(ex.Message, "Python");
    }

    [TestMethod]
    public async Task CreateTfeAsync_WhenRepositoryThrowsDbException_ThrowsInvalidOperationException()
    {
        _tfeRepoMock.Setup(r => r.CreateAsync(It.IsAny<TFE>()))
            .ThrowsAsync(new DbUpdateException("Unique constraint", new Exception()));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => _service.CreateTfeAsync(CreateValidRequest(), "author-1"));
    }

    [TestMethod]
    public async Task CreateTfeAsync_WhenValid_ReturnsCreatedResponseWithTfeId()
    {
        SetupCreateAsyncReturnsWithId(42);

        var result = await _service.CreateTfeAsync(CreateValidRequest(), "author-1");

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.AreEqual(42, result.TfeId);
        Assert.IsNotNull(result.Tfe);
    }

    [TestMethod]
    public async Task CreateTfeAsync_WhenValid_SetsStatusToOpenAndAuthorId()
    {
        SetupCreateAsyncReturnsWithId(1);

        var request = CreateValidRequest();
        await _service.CreateTfeAsync(request, "author-1");

        _tfeRepoMock.Verify(r => r.CreateAsync(It.Is<TFE>(tfe =>
            tfe.AuthorId == "author-1" &&
            tfe.Status == TfeStatus.Open)), Times.Once);
    }

    [TestMethod]
    public async Task CreateTfeAsync_WhenTopicsProvided_MapsTagsFromRepository()
    {
        _tagRepoMock.Setup(r => r.GetByNamesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, Tag> { { "AI", new Tag { Id = 1, Name = "AI" } } });
        SetupCreateAsyncReturnsWithId(1);

        var request = CreateValidRequest();
        request.Tfe.Topics = new List<TagDto> { new TagDto { Name = "AI" } };

        await _service.CreateTfeAsync(request, "author-1");

        _tfeRepoMock.Verify(r => r.CreateAsync(It.Is<TFE>(tfe => tfe.Topics.Count == 1)), Times.Once);
    }

    [TestMethod]
    public async Task CreateTfeAsync_WhenSkillsProvided_MapsSkillsWithLevel()
    {
        _tagRepoMock.Setup(r => r.GetByNamesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, Tag> { { "Python", new Tag { Id = 2, Name = "Python" } } });
        SetupCreateAsyncReturnsWithId(1);

        var request = CreateValidRequest();
        request.Tfe.RequiredSkills = new List<SkillDto> { new SkillDto { Tag = "Python", Level = 4 } };

        await _service.CreateTfeAsync(request, "author-1");

        _tfeRepoMock.Verify(r => r.CreateAsync(It.Is<TFE>(tfe =>
            tfe.RequiredSkills.Count == 1 &&
            tfe.RequiredSkills[0].TagId == 2 &&
            tfe.RequiredSkills[0].Level == 4)), Times.Once);
    }

    // =========================================================================
    // GetTfeByIdAsync
    // =========================================================================

    [TestMethod]
    public async Task GetTfeByIdAsync_WhenTfeNotFound_ReturnsNull()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((TFE?)null);

        var result = await _service.GetTfeByIdAsync(99);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetTfeByIdAsync_WhenTfeFound_ReturnsMappedDto()
    {
        var tfe = CreateTfeEntity(1, "author-1");
        tfe.Title = "My TFE";
        tfe.Description = "My Description";
        tfe.Topics = new List<Tag> { new Tag { Id = 1, Name = "AI" } };
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tfe);

        var result = await _service.GetTfeByIdAsync(1);

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("My TFE", result.Title);
        Assert.AreEqual("Author Name", result.TutorName);
        Assert.AreEqual(1, result.Topics.Count);
        Assert.AreEqual("AI", result.Topics[0].Name);
    }

    // =========================================================================
    // GetTfesByAuthorIdAsync
    // =========================================================================

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public async Task GetTfesByAuthorIdAsync_WhenAuthorIdIsInvalid_ThrowsArgumentException(string authorId)
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _service.GetTfesByAuthorIdAsync(authorId));
    }

    [TestMethod]
    public async Task GetTfesByAuthorIdAsync_WhenNoTfes_ReturnsEmptyList()
    {
        _tfeRepoMock.Setup(r => r.GetByAuthorIdAsync("author-1")).ReturnsAsync(new List<TFE>());
        _proposalRepoMock.Setup(r => r.GetInterestedCountsByTfeIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new Dictionary<int, int>());

        var result = await _service.GetTfesByAuthorIdAsync("author-1");

        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task GetTfesByAuthorIdAsync_WhenTfesFound_SetsInterestedAmountFromDictionary()
    {
        var tfe = CreateTfeEntity(1, "author-1");
        _tfeRepoMock.Setup(r => r.GetByAuthorIdAsync("author-1")).ReturnsAsync(new List<TFE> { tfe });
        _proposalRepoMock.Setup(r => r.GetInterestedCountsByTfeIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new Dictionary<int, int> { { 1, 5 } });

        var result = await _service.GetTfesByAuthorIdAsync("author-1");

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(5, result[0].InterestedAmount);
    }

    [TestMethod]
    public async Task GetTfesByAuthorIdAsync_WhenTfeNotInCountsDictionary_SetsInterestedAmountToZero()
    {
        var tfe = CreateTfeEntity(1, "author-1");
        _tfeRepoMock.Setup(r => r.GetByAuthorIdAsync("author-1")).ReturnsAsync(new List<TFE> { tfe });
        _proposalRepoMock.Setup(r => r.GetInterestedCountsByTfeIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new Dictionary<int, int>());

        var result = await _service.GetTfesByAuthorIdAsync("author-1");

        Assert.AreEqual(0, result[0].InterestedAmount);
    }

    // =========================================================================
    // UpdateTfeAsync
    // =========================================================================

    [TestMethod]
    public async Task UpdateTfeAsync_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => _service.UpdateTfeAsync(1, null!, "author-1"));
    }

    [TestMethod]
    public async Task UpdateTfeAsync_WhenTfeIsNull_ThrowsArgumentNullException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => _service.UpdateTfeAsync(1, new TfeUpdateRequest { Tfe = null! }, "author-1"));
    }

    [TestMethod]
    public async Task UpdateTfeAsync_WhenTitleIsEmpty_ThrowsArgumentException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _service.UpdateTfeAsync(1, new TfeUpdateRequest { Tfe = new TfeDto { Title = "", Description = "Desc" } }, "author-1"));
    }

    [TestMethod]
    public async Task UpdateTfeAsync_WhenTfeNotFound_ReturnsFalse()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((TFE?)null);

        var result = await _service.UpdateTfeAsync(99, new TfeUpdateRequest { Tfe = new TfeDto { Title = "T", Description = "D" } }, "author-1");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task UpdateTfeAsync_WhenAuthorDoesNotMatch_ReturnsFalse()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateTfeEntity(1, "real-author"));

        var result = await _service.UpdateTfeAsync(1, new TfeUpdateRequest { Tfe = new TfeDto { Title = "T", Description = "D" } }, "different-author");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task UpdateTfeAsync_WhenExpirationDateChangedToInvalid_ThrowsArgumentException()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateTfeEntity(1, "author-1"));

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _service.UpdateTfeAsync(1, new TfeUpdateRequest
            {
                Tfe = new TfeDto
                {
                    Title = "T",
                    Description = "D",
                    ExpirationDate = DateTime.Today,
                    EstimatedDelivery = DateTime.Today.AddDays(30),
                    Topics = new List<TagDto>(),
                    RequiredSkills = new List<SkillDto>()
                }
            }, "author-1"));
    }

    [TestMethod]
    public async Task UpdateTfeAsync_WhenExpirationDateUnchanged_DoesNotValidateDateAndReturnsTrue()
    {
        // Even if the date is in the past, it is not validated when unchanged
        var pastDate = DateTime.Today.AddDays(-10);
        var existingTfe = CreateTfeEntity(1, "author-1", DateOnly.FromDateTime(pastDate));
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingTfe);
        _tfeRepoMock.Setup(r => r.UpdateAsync(It.IsAny<TFE>())).Returns(Task.CompletedTask);

        var result = await _service.UpdateTfeAsync(1, new TfeUpdateRequest
        {
            Tfe = new TfeDto
            {
                Title = "Updated",
                Description = "Updated desc",
                ExpirationDate = pastDate,
                EstimatedDelivery = DateTime.Today.AddDays(30),
                Topics = new List<TagDto>(),
                RequiredSkills = new List<SkillDto>()
            }
        }, "author-1");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task UpdateTfeAsync_WhenTopicTagNotFound_ThrowsArgumentException()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateTfeEntity(1, "author-1"));
        _tagRepoMock.Setup(r => r.GetByNameAsync("UnknownTag")).ReturnsAsync((Tag?)null);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _service.UpdateTfeAsync(1, new TfeUpdateRequest
            {
                Tfe = new TfeDto
                {
                    Title = "T",
                    Description = "D",
                    ExpirationDate = DateTime.Today.AddDays(2),
                    EstimatedDelivery = DateTime.Today.AddDays(30),
                    Topics = new List<TagDto> { new TagDto { Name = "UnknownTag" } },
                    RequiredSkills = new List<SkillDto>()
                }
            }, "author-1"));
    }

    [TestMethod]
    public async Task UpdateTfeAsync_WhenRepositoryThrowsDbException_ThrowsInvalidOperationException()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateTfeEntity(1, "author-1"));
        _tfeRepoMock.Setup(r => r.UpdateAsync(It.IsAny<TFE>()))
            .ThrowsAsync(new DbUpdateException("DB error", new Exception()));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => _service.UpdateTfeAsync(1, new TfeUpdateRequest
            {
                Tfe = new TfeDto
                {
                    Title = "T",
                    Description = "D",
                    ExpirationDate = DateTime.Today.AddDays(2),
                    EstimatedDelivery = DateTime.Today.AddDays(30),
                    Topics = new List<TagDto>(),
                    RequiredSkills = new List<SkillDto>()
                }
            }, "author-1"));
    }

    [TestMethod]
    public async Task UpdateTfeAsync_WhenValid_UpdatesAndReturnsTrue()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateTfeEntity(1, "author-1"));
        _tfeRepoMock.Setup(r => r.UpdateAsync(It.IsAny<TFE>())).Returns(Task.CompletedTask);

        var result = await _service.UpdateTfeAsync(1, new TfeUpdateRequest
        {
            Tfe = new TfeDto
            {
                Title = "Updated Title",
                Description = "Updated Desc",
                ExpirationDate = DateTime.Today.AddDays(2),
                EstimatedDelivery = DateTime.Today.AddDays(30),
                Topics = new List<TagDto>(),
                RequiredSkills = new List<SkillDto>()
            }
        }, "author-1");

        Assert.IsTrue(result);
        _tfeRepoMock.Verify(r => r.UpdateAsync(It.Is<TFE>(tfe =>
            tfe.Title == "Updated Title" &&
            tfe.Description == "Updated Desc")), Times.Once);
    }

    // =========================================================================
    // DeleteTfeAsync
    // =========================================================================

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public async Task DeleteTfeAsync_WhenAuthorIdIsInvalid_ThrowsArgumentException(string authorId)
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _service.DeleteTfeAsync(1, authorId));
    }

    [TestMethod]
    public async Task DeleteTfeAsync_WhenTfeNotFoundOrUnauthorized_ReturnsFalse()
    {
        _tfeRepoMock.Setup(r => r.DeleteAsync(1, "author-1")).ReturnsAsync(false);

        var result = await _service.DeleteTfeAsync(1, "author-1");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task DeleteTfeAsync_WhenSuccess_ReturnsTrue()
    {
        _tfeRepoMock.Setup(r => r.DeleteAsync(1, "author-1")).ReturnsAsync(true);

        var result = await _service.DeleteTfeAsync(1, "author-1");

        Assert.IsTrue(result);
    }

    // =========================================================================
    // GetRecommendedTfesAsync
    // =========================================================================

    [TestMethod]
    public async Task GetRecommendedTfesAsync_WhenUserIdIsEmpty_ThrowsArgumentException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _service.GetRecommendedTfesAsync(string.Empty, new TfeRecommendedRequest { Count = 10 }));
    }

    [TestMethod]
    public async Task GetRecommendedTfesAsync_WhenCountIsZero_DefaultsToTenAndCallsRepository()
    {
        _tagRepoMock.Setup(r => r.GetUserInterestsAsync("user-1")).ReturnsAsync(new List<Tag>());
        _tfeRepoMock.Setup(r => r.GetRecommendedTfesAsync("user-1", It.IsAny<List<int>>(), 10))
            .ReturnsAsync(new List<TFE>());

        await _service.GetRecommendedTfesAsync("user-1", new TfeRecommendedRequest { Count = 0 });

        _tfeRepoMock.Verify(r => r.GetRecommendedTfesAsync("user-1", It.IsAny<List<int>>(), 10), Times.Once);
    }

    [TestMethod]
    public async Task GetRecommendedTfesAsync_WhenUserHasNoInterests_PassesEmptyTagIdList()
    {
        _tagRepoMock.Setup(r => r.GetUserInterestsAsync("user-1")).ReturnsAsync(new List<Tag>());
        _tfeRepoMock.Setup(r => r.GetRecommendedTfesAsync("user-1", It.IsAny<List<int>>(), It.IsAny<int>()))
            .ReturnsAsync(new List<TFE>());

        var result = await _service.GetRecommendedTfesAsync("user-1", new TfeRecommendedRequest { Count = 5 });

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.AreEqual(0, result.TotalCount);
        Assert.AreEqual(0, result.Tfes.Count);
        _tfeRepoMock.Verify(r => r.GetRecommendedTfesAsync("user-1", It.Is<List<int>>(ids => ids.Count == 0), 5), Times.Once);
    }

    [TestMethod]
    public async Task GetRecommendedTfesAsync_WhenTfesFound_ReturnsMappedDtos()
    {
        var tfe1 = CreateTfeEntity(1, "author-1");
        var tfe2 = CreateTfeEntity(2, "author-2");
        tfe1.Title = "TFE One";
        tfe2.Title = "TFE Two";

        _tagRepoMock.Setup(r => r.GetUserInterestsAsync("user-1"))
            .ReturnsAsync(new List<Tag> { new Tag { Id = 1, Name = "AI" } });
        _tfeRepoMock.Setup(r => r.GetRecommendedTfesAsync("user-1", It.IsAny<List<int>>(), It.IsAny<int>()))
            .ReturnsAsync(new List<TFE> { tfe1, tfe2 });

        var result = await _service.GetRecommendedTfesAsync("user-1", new TfeRecommendedRequest { Count = 10 });

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.AreEqual(2, result.TotalCount);
        Assert.AreEqual(2, result.Tfes.Count);
        Assert.IsTrue(result.Tfes.Any(t => t.Title == "TFE One"));
        Assert.IsTrue(result.Tfes.Any(t => t.Title == "TFE Two"));
    }

    [TestMethod]
    public async Task GetRecommendedTfesAsync_WhenUserInterestsExist_PassesTagIdsToRepository()
    {
        _tagRepoMock.Setup(r => r.GetUserInterestsAsync("user-1"))
            .ReturnsAsync(new List<Tag> { new Tag { Id = 3, Name = "AI" }, new Tag { Id = 7, Name = "ML" } });
        _tfeRepoMock.Setup(r => r.GetRecommendedTfesAsync("user-1", It.IsAny<List<int>>(), It.IsAny<int>()))
            .ReturnsAsync(new List<TFE>());

        await _service.GetRecommendedTfesAsync("user-1", new TfeRecommendedRequest { Count = 5 });

        _tfeRepoMock.Verify(r => r.GetRecommendedTfesAsync(
            "user-1",
            It.Is<List<int>>(ids => ids.Contains(3) && ids.Contains(7) && ids.Count == 2),
            5), Times.Once);
    }

    // =========================================================================
    // ChangeTfeStatusAsync
    // =========================================================================

    [TestMethod]
    public async Task ChangeTfeStatusAsync_WhenStatusIsOpen_ReturnsInvalidStatus()
    {
        var result = await _service.ChangeTfeStatusAsync(1, TfeStatus.Open, "author-1");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("InvalidStatus", result.ErrorCode);
    }

    [TestMethod]
    public async Task ChangeTfeStatusAsync_WhenTfeNotFound_ReturnsTfeNotFound()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((TFE?)null);

        var result = await _service.ChangeTfeStatusAsync(99, TfeStatus.Completed, "author-1");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("TfeNotFound", result.ErrorCode);
    }

    [TestMethod]
    public async Task ChangeTfeStatusAsync_WhenAuthorDoesNotMatch_ReturnsUnauthorized()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateTfeEntity(1, "real-author"));

        var result = await _service.ChangeTfeStatusAsync(1, TfeStatus.Completed, "different-author");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("Unauthorized", result.ErrorCode);
    }

    [TestMethod]
    [DataRow(TfeStatus.Completed)]
    [DataRow(TfeStatus.Cancelled)]
    public async Task ChangeTfeStatusAsync_WhenTfeIsAlreadyClosed_ReturnsInvalidCurrentStatus(TfeStatus currentStatus)
    {
        var tfe = CreateTfeEntity(1, "author-1");
        tfe.Status = currentStatus;
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tfe);

        var result = await _service.ChangeTfeStatusAsync(1, TfeStatus.Completed, "author-1");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("InvalidCurrentStatus", result.ErrorCode);
    }

    [TestMethod]
    public async Task ChangeTfeStatusAsync_WhenCompletingOpenTfe_UpdatesStatusAndDoesNotExpireProposals()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateTfeEntity(1, "author-1"));
        _tfeRepoMock.Setup(r => r.UpdateStatusAsync(1, TfeStatus.Completed)).Returns(Task.CompletedTask);

        var result = await _service.ChangeTfeStatusAsync(1, TfeStatus.Completed, "author-1");

        Assert.IsTrue(result.IsSuccess);
        _tfeRepoMock.Verify(r => r.UpdateStatusAsync(1, TfeStatus.Completed), Times.Once);
        _proposalRepoMock.Verify(r => r.ExpireProposalsByTfeIdAsync(It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task ChangeTfeStatusAsync_WhenCancellingOpenTfe_ExpiresProposalsAndUpdatesStatus()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateTfeEntity(1, "author-1"));
        _tfeRepoMock.Setup(r => r.UpdateStatusAsync(1, TfeStatus.Cancelled)).Returns(Task.CompletedTask);
        _proposalRepoMock.Setup(r => r.ExpireProposalsByTfeIdAsync(1)).Returns(Task.CompletedTask);

        var result = await _service.ChangeTfeStatusAsync(1, TfeStatus.Cancelled, "author-1");

        Assert.IsTrue(result.IsSuccess);
        _proposalRepoMock.Verify(r => r.ExpireProposalsByTfeIdAsync(1), Times.Once);
        _tfeRepoMock.Verify(r => r.UpdateStatusAsync(1, TfeStatus.Cancelled), Times.Once);
    }

    [TestMethod]
    public async Task ChangeTfeStatusAsync_WhenRepositoryThrowsOnStatusUpdate_ThrowsInvalidOperationException()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateTfeEntity(1, "author-1"));
        _tfeRepoMock.Setup(r => r.UpdateStatusAsync(It.IsAny<int>(), It.IsAny<TfeStatus>()))
            .ThrowsAsync(new Exception("DB error"));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => _service.ChangeTfeStatusAsync(1, TfeStatus.Completed, "author-1"));
    }
}
