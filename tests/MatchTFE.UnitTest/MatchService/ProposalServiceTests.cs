using MatchService.Repositories;
using MatchService.Services;
using Microsoft.Extensions.Logging;
using Moq;
using TFELibrary.Data;
using TFELibrary.Shared;

namespace MatchTFE.UnitTest.MatchService;

[TestClass]
public class ProposalServiceTests
{
    private Mock<IProposalRepository> _proposalRepoMock = null!;
    private Mock<ITfeRepository> _tfeRepoMock = null!;
    private Mock<ILogger<ProposalService>> _loggerMock = null!;
    private ProposalService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _proposalRepoMock = new Mock<IProposalRepository>();
        _tfeRepoMock = new Mock<ITfeRepository>();
        _loggerMock = new Mock<ILogger<ProposalService>>();
        _service = new ProposalService(_proposalRepoMock.Object, _tfeRepoMock.Object, _loggerMock.Object);
    }

    // -- helpers --

    private static TFE CreateValidTfe(int id = 1, string authorId = "author-1", bool expired = false) => new TFE
    {
        Id = id,
        AuthorId = authorId,
        Title = "Test TFE",
        Description = "Test description",
        ExpirationDate = expired
            ? DateOnly.FromDateTime(DateTime.Today)
            : DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
        EstimatedDelivery = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
        CreationDate = DateOnly.FromDateTime(DateTime.Today),
        Status = TfeStatus.Open,
        Topics = new List<Tag>(),
        RequiredSkills = new List<TfeRequiredSkill>()
    };

    private static TFEProposal CreateProposal(string userId, int tfeId, ProposalStatus status) => new TFEProposal
    {
        OriginUserId = userId,
        TfeId = tfeId,
        Status = status,
        CreationDate = DateOnly.FromDateTime(DateTime.Today)
    };

    private void VerifyLogErrorCalledOnce() => _loggerMock.Verify(
        x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => true),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((o, t) => true)),
        Times.Once);

    // =========================================================================
    // CreateTfeProposalAsync
    // =========================================================================

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public async Task CreateTfeProposalAsync_WhenUserIdIsInvalid_ReturnsError(string userId)
    {
        var result = await _service.CreateTfeProposalAsync(userId, new TfeProposalCreationRequest { TfeId = 1 });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.IsNull(result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task CreateTfeProposalAsync_WhenRequestIsNull_ReturnsError()
    {
        var result = await _service.CreateTfeProposalAsync("user-1", null!);

        Assert.IsFalse(result.Error.IsSuccess);
    }

    [TestMethod]
    public async Task CreateTfeProposalAsync_WhenTfeIdIsZero_ReturnsError()
    {
        var result = await _service.CreateTfeProposalAsync("user-1", new TfeProposalCreationRequest { TfeId = 0 });

        Assert.IsFalse(result.Error.IsSuccess);
    }

    [TestMethod]
    public async Task CreateTfeProposalAsync_WhenTfeNotFound_ReturnsTfeNotFoundError()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((TFE?)null);

        var result = await _service.CreateTfeProposalAsync("user-1", new TfeProposalCreationRequest { TfeId = 99 });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("TfeNotFound", result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task CreateTfeProposalAsync_WhenTfeIsExpired_ReturnsTfeExpiredError()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateValidTfe(expired: true));

        var result = await _service.CreateTfeProposalAsync("user-1", new TfeProposalCreationRequest { TfeId = 1 });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("TfeExpired", result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task CreateTfeProposalAsync_WhenProposalAlreadyExists_ReturnsDuplicateProposalError()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateValidTfe());
        _proposalRepoMock.Setup(r => r.TfeProposalExistsAsync("user-1", 1)).ReturnsAsync(true);

        var result = await _service.CreateTfeProposalAsync("user-1", new TfeProposalCreationRequest { TfeId = 1 });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("DuplicateProposal", result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task CreateTfeProposalAsync_WhenRepositoryThrows_ReturnsDatabaseError()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateValidTfe());
        _proposalRepoMock.Setup(r => r.TfeProposalExistsAsync("user-1", 1)).ReturnsAsync(false);
        _proposalRepoMock.Setup(r => r.CreateTfeProposalAsync(It.IsAny<TFEProposal>()))
            .ThrowsAsync(new Exception("DB error"));

        var result = await _service.CreateTfeProposalAsync("user-1", new TfeProposalCreationRequest { TfeId = 1, IsInterested = true });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("DatabaseError", result.Error.ErrorCode);
        VerifyLogErrorCalledOnce();
    }

    [TestMethod]
    public async Task CreateTfeProposalAsync_WhenInterestedIsTrue_CreatesProposalWithPendingStatus()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateValidTfe());
        _proposalRepoMock.Setup(r => r.TfeProposalExistsAsync("user-1", 1)).ReturnsAsync(false);
        _proposalRepoMock.Setup(r => r.CreateTfeProposalAsync(It.IsAny<TFEProposal>())).Returns(Task.CompletedTask);

        var result = await _service.CreateTfeProposalAsync("user-1", new TfeProposalCreationRequest { TfeId = 1, IsInterested = true });

        Assert.IsTrue(result.Error.IsSuccess);
        _proposalRepoMock.Verify(r => r.CreateTfeProposalAsync(It.Is<TFEProposal>(p =>
            p.Status == ProposalStatus.Pending &&
            p.OriginUserId == "user-1" &&
            p.TfeId == 1)), Times.Once);
    }

    [TestMethod]
    public async Task CreateTfeProposalAsync_WhenInterestedIsFalse_CreatesProposalWithRejectedStatus()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateValidTfe());
        _proposalRepoMock.Setup(r => r.TfeProposalExistsAsync("user-1", 1)).ReturnsAsync(false);
        _proposalRepoMock.Setup(r => r.CreateTfeProposalAsync(It.IsAny<TFEProposal>())).Returns(Task.CompletedTask);

        var result = await _service.CreateTfeProposalAsync("user-1", new TfeProposalCreationRequest { TfeId = 1, IsInterested = false });

        Assert.IsTrue(result.Error.IsSuccess);
        _proposalRepoMock.Verify(r => r.CreateTfeProposalAsync(It.Is<TFEProposal>(p =>
            p.Status == ProposalStatus.Rejected)), Times.Once);
    }

    // =========================================================================
    // UpdateTfeProposalAsync
    // =========================================================================

    [TestMethod]
    public async Task UpdateTfeProposalAsync_WhenUserIdIsEmpty_ReturnsError()
    {
        var result = await _service.UpdateTfeProposalAsync(new TfeProposalUpdateRequest { UserId = "", TfeId = 1 });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.IsNull(result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task UpdateTfeProposalAsync_WhenTfeIdIsZero_ReturnsError()
    {
        var result = await _service.UpdateTfeProposalAsync(new TfeProposalUpdateRequest { UserId = "user-1", TfeId = 0 });

        Assert.IsFalse(result.Error.IsSuccess);
    }

    [TestMethod]
    public async Task UpdateTfeProposalAsync_WhenTfeNotFound_ReturnsTfeNotFoundError()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((TFE?)null);

        var result = await _service.UpdateTfeProposalAsync(new TfeProposalUpdateRequest { UserId = "user-1", TfeId = 99 });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("TfeNotFound", result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task UpdateTfeProposalAsync_WhenTfeIsExpired_ReturnsTfeExpiredError()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateValidTfe(expired: true));

        var result = await _service.UpdateTfeProposalAsync(new TfeProposalUpdateRequest { UserId = "user-1", TfeId = 1 });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("TfeExpired", result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task UpdateTfeProposalAsync_WhenProposalNotFound_ReturnsProposalNotFoundError()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateValidTfe());
        _proposalRepoMock.Setup(r => r.GetTfeProposalByUserIdAsync("user-1", 1)).ReturnsAsync((TFEProposal?)null);

        var result = await _service.UpdateTfeProposalAsync(new TfeProposalUpdateRequest { UserId = "user-1", TfeId = 1 });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("ProposalNotFound", result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task UpdateTfeProposalAsync_WhenProposalIsAccepted_ReturnsInvalidStatusError()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateValidTfe());
        _proposalRepoMock.Setup(r => r.GetTfeProposalByUserIdAsync("user-1", 1))
            .ReturnsAsync(CreateProposal("user-1", 1, ProposalStatus.Accepted));

        var result = await _service.UpdateTfeProposalAsync(new TfeProposalUpdateRequest { UserId = "user-1", TfeId = 1 });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("InvalidProposalStatus", result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task UpdateTfeProposalAsync_WhenProposalIsRejected_ReturnsInvalidStatusError()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateValidTfe());
        _proposalRepoMock.Setup(r => r.GetTfeProposalByUserIdAsync("user-1", 1))
            .ReturnsAsync(CreateProposal("user-1", 1, ProposalStatus.Rejected));

        var result = await _service.UpdateTfeProposalAsync(new TfeProposalUpdateRequest { UserId = "user-1", TfeId = 1 });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("InvalidProposalStatus", result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task UpdateTfeProposalAsync_WhenRepositoryThrows_ReturnsDatabaseError()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateValidTfe());
        _proposalRepoMock.Setup(r => r.GetTfeProposalByUserIdAsync("user-1", 1))
            .ReturnsAsync(CreateProposal("user-1", 1, ProposalStatus.Pending));
        _proposalRepoMock.Setup(r => r.UpdateTfeProposalAsync(It.IsAny<TFEProposal>()))
            .ThrowsAsync(new Exception("DB error"));

        var result = await _service.UpdateTfeProposalAsync(new TfeProposalUpdateRequest { UserId = "user-1", TfeId = 1, IsInterested = true });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("DatabaseError", result.Error.ErrorCode);
        VerifyLogErrorCalledOnce();
    }

    [TestMethod]
    public async Task UpdateTfeProposalAsync_WhenInterestedIsTrue_SetsStatusToAccepted()
    {
        var proposal = CreateProposal("user-1", 1, ProposalStatus.Pending);
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateValidTfe());
        _proposalRepoMock.Setup(r => r.GetTfeProposalByUserIdAsync("user-1", 1)).ReturnsAsync(proposal);
        _proposalRepoMock.Setup(r => r.UpdateTfeProposalAsync(It.IsAny<TFEProposal>())).Returns(Task.CompletedTask);

        var result = await _service.UpdateTfeProposalAsync(new TfeProposalUpdateRequest { UserId = "user-1", TfeId = 1, IsInterested = true });

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.AreEqual(ProposalStatus.Accepted, proposal.Status);
    }

    [TestMethod]
    public async Task UpdateTfeProposalAsync_WhenInterestedIsFalse_SetsStatusToRejected()
    {
        var proposal = CreateProposal("user-1", 1, ProposalStatus.Pending);
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateValidTfe());
        _proposalRepoMock.Setup(r => r.GetTfeProposalByUserIdAsync("user-1", 1)).ReturnsAsync(proposal);
        _proposalRepoMock.Setup(r => r.UpdateTfeProposalAsync(It.IsAny<TFEProposal>())).Returns(Task.CompletedTask);

        var result = await _service.UpdateTfeProposalAsync(new TfeProposalUpdateRequest { UserId = "user-1", TfeId = 1, IsInterested = false });

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.AreEqual(ProposalStatus.Rejected, proposal.Status);
    }

    // =========================================================================
    // GetAcceptedMatchesForUserAsync
    // =========================================================================

    [TestMethod]
    public async Task GetAcceptedMatchesForUserAsync_WhenUserIdIsEmpty_ReturnsError()
    {
        var result = await _service.GetAcceptedMatchesForUserAsync(string.Empty);

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual(0, result.TotalMatches);
        Assert.AreEqual(0, result.Matches.Count);
    }

    [TestMethod]
    public async Task GetAcceptedMatchesForUserAsync_WhenRepositoryThrows_ReturnsDatabaseError()
    {
        _proposalRepoMock.Setup(r => r.GetAcceptedMatchesForUserAsync("user-1"))
            .ThrowsAsync(new Exception("DB error"));

        var result = await _service.GetAcceptedMatchesForUserAsync("user-1");

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("DatabaseError", result.Error.ErrorCode);
        VerifyLogErrorCalledOnce();
    }

    [TestMethod]
    public async Task GetAcceptedMatchesForUserAsync_WhenNoMatches_ReturnsSuccessWithEmptyList()
    {
        _proposalRepoMock.Setup(r => r.GetAcceptedMatchesForUserAsync("user-1"))
            .ReturnsAsync(new List<AcceptedMatchDto>());

        var result = await _service.GetAcceptedMatchesForUserAsync("user-1");

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.AreEqual(0, result.TotalMatches);
        Assert.AreEqual(0, result.Matches.Count);
    }

    [TestMethod]
    public async Task GetAcceptedMatchesForUserAsync_WhenMatchesFound_ReturnsCorrectCount()
    {
        var matches = new List<AcceptedMatchDto>
        {
            new AcceptedMatchDto { TfeId = 1, MatchedUserId = "user-2" },
            new AcceptedMatchDto { TfeId = 2, MatchedUserId = "user-3" },
            new AcceptedMatchDto { TfeId = 3, MatchedUserId = "user-4" }
        };
        _proposalRepoMock.Setup(r => r.GetAcceptedMatchesForUserAsync("user-1")).ReturnsAsync(matches);

        var result = await _service.GetAcceptedMatchesForUserAsync("user-1");

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.AreEqual(3, result.TotalMatches);
        Assert.AreEqual(3, result.Matches.Count);
    }

    // =========================================================================
    // DecideTfeCandidateAsync
    // =========================================================================

    [TestMethod]
    public async Task DecideTfeCandidateAsync_WhenAuthorIdIsEmpty_ReturnsError()
    {
        var result = await _service.DecideTfeCandidateAsync(string.Empty, new TfeCandidateDecisionRequest { TfeId = 1, CandidateId = "cand-1", Status = ProposalStatus.Accepted });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.IsNull(result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task DecideTfeCandidateAsync_WhenRequestIsNull_ReturnsError()
    {
        var result = await _service.DecideTfeCandidateAsync("author-1", null!);

        Assert.IsFalse(result.Error.IsSuccess);
    }

    [TestMethod]
    public async Task DecideTfeCandidateAsync_WhenCandidateIdIsEmpty_ReturnsError()
    {
        var result = await _service.DecideTfeCandidateAsync("author-1", new TfeCandidateDecisionRequest { TfeId = 1, CandidateId = "", Status = ProposalStatus.Accepted });

        Assert.IsFalse(result.Error.IsSuccess);
    }

    [TestMethod]
    public async Task DecideTfeCandidateAsync_WhenTfeIdIsZero_ReturnsError()
    {
        var result = await _service.DecideTfeCandidateAsync("author-1", new TfeCandidateDecisionRequest { TfeId = 0, CandidateId = "cand-1", Status = ProposalStatus.Accepted });

        Assert.IsFalse(result.Error.IsSuccess);
    }

    [TestMethod]
    [DataRow(ProposalStatus.Pending)]
    [DataRow(ProposalStatus.Expired)]
    public async Task DecideTfeCandidateAsync_WhenStatusIsInvalid_ReturnsInvalidStatusError(ProposalStatus status)
    {
        var result = await _service.DecideTfeCandidateAsync("author-1", new TfeCandidateDecisionRequest { TfeId = 1, CandidateId = "cand-1", Status = status });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("InvalidStatus", result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task DecideTfeCandidateAsync_WhenTfeNotFound_ReturnsTfeNotFoundError()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((TFE?)null);

        var result = await _service.DecideTfeCandidateAsync("author-1", new TfeCandidateDecisionRequest { TfeId = 99, CandidateId = "cand-1", Status = ProposalStatus.Accepted });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("TfeNotFound", result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task DecideTfeCandidateAsync_WhenTfeIsExpired_ReturnsTfeExpiredError()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateValidTfe(expired: true));

        var result = await _service.DecideTfeCandidateAsync("author-1", new TfeCandidateDecisionRequest { TfeId = 1, CandidateId = "cand-1", Status = ProposalStatus.Accepted });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("TfeExpired", result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task DecideTfeCandidateAsync_WhenNotOwner_ReturnsUnauthorizedError()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateValidTfe(authorId: "real-author"));

        var result = await _service.DecideTfeCandidateAsync("different-author", new TfeCandidateDecisionRequest { TfeId = 1, CandidateId = "cand-1", Status = ProposalStatus.Accepted });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("Unauthorized", result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task DecideTfeCandidateAsync_WhenAuthorIdDiffersInCase_ReturnsUnauthorizedError()
    {
        // Comparison is case-sensitive (StringComparison.Ordinal)
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateValidTfe(authorId: "Author-1"));

        var result = await _service.DecideTfeCandidateAsync("author-1", new TfeCandidateDecisionRequest { TfeId = 1, CandidateId = "cand-1", Status = ProposalStatus.Accepted });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("Unauthorized", result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task DecideTfeCandidateAsync_WhenProposalNotFound_ReturnsProposalNotFoundError()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateValidTfe(authorId: "author-1"));
        _proposalRepoMock.Setup(r => r.GetTfeProposalByUserIdAsync("cand-1", 1)).ReturnsAsync((TFEProposal?)null);

        var result = await _service.DecideTfeCandidateAsync("author-1", new TfeCandidateDecisionRequest { TfeId = 1, CandidateId = "cand-1", Status = ProposalStatus.Accepted });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("ProposalNotFound", result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task DecideTfeCandidateAsync_WhenProposalAlreadyAccepted_ReturnsAlreadyResolvedError()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateValidTfe(authorId: "author-1"));
        _proposalRepoMock.Setup(r => r.GetTfeProposalByUserIdAsync("cand-1", 1))
            .ReturnsAsync(CreateProposal("cand-1", 1, ProposalStatus.Accepted));

        var result = await _service.DecideTfeCandidateAsync("author-1", new TfeCandidateDecisionRequest { TfeId = 1, CandidateId = "cand-1", Status = ProposalStatus.Accepted });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("ProposalAlreadyResolved", result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task DecideTfeCandidateAsync_WhenRepositoryThrows_ReturnsDatabaseError()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateValidTfe(authorId: "author-1"));
        _proposalRepoMock.Setup(r => r.GetTfeProposalByUserIdAsync("cand-1", 1))
            .ReturnsAsync(CreateProposal("cand-1", 1, ProposalStatus.Pending));
        _proposalRepoMock.Setup(r => r.UpdateTfeProposalAsync(It.IsAny<TFEProposal>()))
            .ThrowsAsync(new Exception("DB error"));

        var result = await _service.DecideTfeCandidateAsync("author-1", new TfeCandidateDecisionRequest { TfeId = 1, CandidateId = "cand-1", Status = ProposalStatus.Accepted });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("DatabaseError", result.Error.ErrorCode);
        VerifyLogErrorCalledOnce();
    }

    [TestMethod]
    public async Task DecideTfeCandidateAsync_WhenAccepted_ReturnsSuccessWithAcceptedStatus()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateValidTfe(authorId: "author-1"));
        _proposalRepoMock.Setup(r => r.GetTfeProposalByUserIdAsync("cand-1", 1))
            .ReturnsAsync(CreateProposal("cand-1", 1, ProposalStatus.Pending));
        _proposalRepoMock.Setup(r => r.UpdateTfeProposalAsync(It.IsAny<TFEProposal>())).Returns(Task.CompletedTask);

        var result = await _service.DecideTfeCandidateAsync("author-1", new TfeCandidateDecisionRequest { TfeId = 1, CandidateId = "cand-1", Status = ProposalStatus.Accepted });

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.AreEqual(ProposalStatus.Accepted, result.Status);
        Assert.IsTrue(result.Error.Message.Contains("accepted", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task DecideTfeCandidateAsync_WhenRejected_ReturnsSuccessWithRejectedStatus()
    {
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateValidTfe(authorId: "author-1"));
        _proposalRepoMock.Setup(r => r.GetTfeProposalByUserIdAsync("cand-1", 1))
            .ReturnsAsync(CreateProposal("cand-1", 1, ProposalStatus.Pending));
        _proposalRepoMock.Setup(r => r.UpdateTfeProposalAsync(It.IsAny<TFEProposal>())).Returns(Task.CompletedTask);

        var result = await _service.DecideTfeCandidateAsync("author-1", new TfeCandidateDecisionRequest { TfeId = 1, CandidateId = "cand-1", Status = ProposalStatus.Rejected });

        Assert.IsTrue(result.Error.IsSuccess);
        Assert.AreEqual(ProposalStatus.Rejected, result.Status);
        Assert.IsTrue(result.Error.Message.Contains("rejected", StringComparison.OrdinalIgnoreCase));
    }

    // =========================================================================
    // TFE status checks (TfeNotOpen)
    // =========================================================================

    [TestMethod]
    [DataRow(TfeStatus.Completed)]
    [DataRow(TfeStatus.Cancelled)]
    public async Task CreateTfeProposalAsync_WhenTfeIsNotOpen_ReturnsTfeNotOpenError(TfeStatus status)
    {
        var tfe = CreateValidTfe();
        tfe.Status = status;
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tfe);

        var result = await _service.CreateTfeProposalAsync("user-1", new TfeProposalCreationRequest { TfeId = 1, IsInterested = true });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("TfeNotOpen", result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task UpdateTfeProposalAsync_WhenTfeIsCompleted_ReturnsTfeNotOpenError()
    {
        var completedTfe = CreateValidTfe();
        completedTfe.Status = TfeStatus.Completed;
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(completedTfe);

        var result = await _service.UpdateTfeProposalAsync(new TfeProposalUpdateRequest { UserId = "user-1", TfeId = 1, IsInterested = true });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("TfeNotOpen", result.Error.ErrorCode);
    }

    [TestMethod]
    public async Task DecideTfeCandidateAsync_WhenTfeIsCompleted_ReturnsTfeNotOpenError()
    {
        var completedTfe = CreateValidTfe(authorId: "author-1");
        completedTfe.Status = TfeStatus.Completed;
        _tfeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(completedTfe);

        var result = await _service.DecideTfeCandidateAsync("author-1", new TfeCandidateDecisionRequest { TfeId = 1, CandidateId = "cand-1", Status = ProposalStatus.Accepted });

        Assert.IsFalse(result.Error.IsSuccess);
        Assert.AreEqual("TfeNotOpen", result.Error.ErrorCode);
    }
}
