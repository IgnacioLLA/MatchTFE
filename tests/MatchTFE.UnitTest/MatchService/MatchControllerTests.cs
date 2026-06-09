using MatchService.Controllers;
using MatchService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using TFELibrary.Shared;

namespace MatchTFE.UnitTest.MatchService;

[TestClass]
public class MatchControllerTests
{
    private Mock<ITagService> _tagServiceMock = null!;
    private Mock<ITfeService> _tfeServiceMock = null!;
    private Mock<IProposalService> _proposalServiceMock = null!;
    private MatchController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _tagServiceMock = new Mock<ITagService>();
        _tfeServiceMock = new Mock<ITfeService>();
        _proposalServiceMock = new Mock<IProposalService>();
        _controller = new MatchController(
            _tagServiceMock.Object,
            _tfeServiceMock.Object,
            _proposalServiceMock.Object,
            Mock.Of<ILogger<MatchController>>());
        SetNoUserClaims();
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
    // Tags
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task GetAllTags_WhenCalled_ReturnsOk()
    {
        _tagServiceMock.Setup(s => s.GetAllTagsAsync()).ReturnsAsync(new List<TagDto> { new TagDto { Name = "AI" } });

        var result = await _controller.GetAllTags();

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    [TestMethod]
    public async Task GetTagById_WhenTagNotFound_ReturnsNotFound()
    {
        _tagServiceMock.Setup(s => s.GetTagByIdAsync(99)).ReturnsAsync((TagDto?)null);

        var result = await _controller.GetTagById(99);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task GetTagById_WhenTagExists_ReturnsOk()
    {
        _tagServiceMock.Setup(s => s.GetTagByIdAsync(1)).ReturnsAsync(new TagDto { Id = 1, Name = "AI" });

        var result = await _controller.GetTagById(1);

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    [TestMethod]
    public async Task CreateTag_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        _controller.ModelState.AddModelError("Tag", "Required");

        var result = await _controller.CreateTag(new TagCreationRequest());

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task CreateTag_WhenDuplicateTag_ReturnsConflict()
    {
        _tagServiceMock.Setup(s => s.CreateTagAsync(It.IsAny<TagCreationRequest>()))
            .ThrowsAsync(new InvalidOperationException("Tag already exists."));

        var result = await _controller.CreateTag(new TagCreationRequest { Tag = new TagDto { Name = "AI" } });

        Assert.IsInstanceOfType(result, typeof(ConflictObjectResult));
    }

    [TestMethod]
    public async Task CreateTag_WhenSuccess_ReturnsCreated()
    {
        _tagServiceMock.Setup(s => s.CreateTagAsync(It.IsAny<TagCreationRequest>()))
            .ReturnsAsync(new TagCreationResponse { Error = new OperationResult(true, "Created."), Tag = new TagDto { Id = 1, Name = "AI" }, TagId = 1 });

        var result = await _controller.CreateTag(new TagCreationRequest { Tag = new TagDto { Name = "AI" } });

        Assert.IsInstanceOfType(result, typeof(CreatedAtActionResult));
    }

    [TestMethod]
    public async Task UpdateTag_WhenTagNameEmpty_ReturnsBadRequest()
    {
        _tagServiceMock.Setup(s => s.UpdateTagAsync(1, It.IsAny<TagUpdateRequest>()))
            .ThrowsAsync(new ArgumentException("Tag name cannot be empty."));

        var result = await _controller.UpdateTag(1, new TagUpdateRequest { Name = "" });

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task UpdateTag_WhenDuplicateName_ReturnsConflict()
    {
        _tagServiceMock.Setup(s => s.UpdateTagAsync(1, It.IsAny<TagUpdateRequest>()))
            .ThrowsAsync(new InvalidOperationException("Tag already exists."));

        var result = await _controller.UpdateTag(1, new TagUpdateRequest { Name = "AI" });

        Assert.IsInstanceOfType(result, typeof(ConflictObjectResult));
    }

    [TestMethod]
    public async Task UpdateTag_WhenTagNotFound_ReturnsNotFound()
    {
        _tagServiceMock.Setup(s => s.UpdateTagAsync(99, It.IsAny<TagUpdateRequest>())).ReturnsAsync(false);

        var result = await _controller.UpdateTag(99, new TagUpdateRequest { Name = "AI" });

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task UpdateTag_WhenSuccess_ReturnsNoContent()
    {
        _tagServiceMock.Setup(s => s.UpdateTagAsync(1, It.IsAny<TagUpdateRequest>())).ReturnsAsync(true);

        var result = await _controller.UpdateTag(1, new TagUpdateRequest { Name = "AI" });

        Assert.IsInstanceOfType(result, typeof(NoContentResult));
    }

    [TestMethod]
    public async Task DeleteTag_WhenTagNotFound_ReturnsNotFound()
    {
        _tagServiceMock.Setup(s => s.DeleteTagAsync(99)).ReturnsAsync(false);

        var result = await _controller.DeleteTag(99);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task DeleteTag_WhenSuccess_ReturnsNoContent()
    {
        _tagServiceMock.Setup(s => s.DeleteTagAsync(1)).ReturnsAsync(true);

        var result = await _controller.DeleteTag(1);

        Assert.IsInstanceOfType(result, typeof(NoContentResult));
    }

    // -------------------------------------------------------------------------
    // TFEs
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task CreateTfe_WhenNoClaimPresent_ReturnsUnauthorized()
    {
        SetNoUserClaims();

        var result = await _controller.CreateTfe(new TfeCreationRequest { Tfe = new TfeDto() });

        Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task CreateTfe_WhenValidationFails_ReturnsBadRequest()
    {
        SetUserClaims("user-1");
        _tfeServiceMock.Setup(s => s.CreateTfeAsync(It.IsAny<TfeCreationRequest>(), It.IsAny<string>()))
            .ThrowsAsync(new ArgumentException("Title is mandatory."));

        var result = await _controller.CreateTfe(new TfeCreationRequest { Tfe = new TfeDto() });

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task CreateTfe_WhenDatabaseConflict_ReturnsConflict()
    {
        SetUserClaims("user-1");
        _tfeServiceMock.Setup(s => s.CreateTfeAsync(It.IsAny<TfeCreationRequest>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Database error."));

        var result = await _controller.CreateTfe(new TfeCreationRequest { Tfe = new TfeDto() });

        Assert.IsInstanceOfType(result, typeof(ConflictObjectResult));
    }

    [TestMethod]
    public async Task CreateTfe_WhenSuccess_ReturnsCreated()
    {
        SetUserClaims("user-1");
        _tfeServiceMock.Setup(s => s.CreateTfeAsync(It.IsAny<TfeCreationRequest>(), "user-1"))
            .ReturnsAsync(new TfeCreationResponse { Error = new OperationResult(true, "Created."), Tfe = new TfeDto { Id = 1 }, TfeId = 1 });

        var result = await _controller.CreateTfe(new TfeCreationRequest { Tfe = new TfeDto() });

        Assert.IsInstanceOfType(result, typeof(CreatedAtActionResult));
    }

    [TestMethod]
    public async Task GetTfeById_WhenIdIsZeroOrNegative_ReturnsBadRequest()
    {
        var result = await _controller.GetTfeById(0);

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task GetTfeById_WhenTfeNotFound_ReturnsNotFound()
    {
        _tfeServiceMock.Setup(s => s.GetTfeByIdAsync(99)).ReturnsAsync((TfeDto?)null);

        var result = await _controller.GetTfeById(99);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task GetTfeById_WhenTfeExists_ReturnsOk()
    {
        _tfeServiceMock.Setup(s => s.GetTfeByIdAsync(1)).ReturnsAsync(new TfeDto { Id = 1, Title = "My TFE" });

        var result = await _controller.GetTfeById(1);

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    [TestMethod]
    public async Task UpdateTfe_WhenNoClaimPresent_ReturnsUnauthorized()
    {
        SetNoUserClaims();

        var result = await _controller.UpdateTfe(1, new TfeUpdateRequest { Tfe = new TfeDto() });

        Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task UpdateTfe_WhenTfeNotFoundOrNotOwner_ReturnsNotFound()
    {
        SetUserClaims("user-1");
        _tfeServiceMock.Setup(s => s.UpdateTfeAsync(1, It.IsAny<TfeUpdateRequest>(), "user-1")).ReturnsAsync(false);

        var result = await _controller.UpdateTfe(1, new TfeUpdateRequest { Tfe = new TfeDto() });

        Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));
    }

    [TestMethod]
    public async Task UpdateTfe_WhenValidationFails_ReturnsBadRequest()
    {
        SetUserClaims("user-1");
        _tfeServiceMock.Setup(s => s.UpdateTfeAsync(1, It.IsAny<TfeUpdateRequest>(), "user-1"))
            .ThrowsAsync(new ArgumentException("Title is mandatory."));

        var result = await _controller.UpdateTfe(1, new TfeUpdateRequest { Tfe = new TfeDto() });

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task UpdateTfe_WhenUnexpectedError_Returns500()
    {
        SetUserClaims("user-1");
        _tfeServiceMock.Setup(s => s.UpdateTfeAsync(1, It.IsAny<TfeUpdateRequest>(), "user-1"))
            .ThrowsAsync(new Exception("Unexpected DB error."));

        var result = await _controller.UpdateTfe(1, new TfeUpdateRequest { Tfe = new TfeDto() });

        var statusResult = result as ObjectResult;
        Assert.AreEqual(500, statusResult?.StatusCode);
    }

    [TestMethod]
    public async Task UpdateTfe_WhenSuccess_ReturnsNoContent()
    {
        SetUserClaims("user-1");
        _tfeServiceMock.Setup(s => s.UpdateTfeAsync(1, It.IsAny<TfeUpdateRequest>(), "user-1")).ReturnsAsync(true);

        var result = await _controller.UpdateTfe(1, new TfeUpdateRequest { Tfe = new TfeDto() });

        Assert.IsInstanceOfType(result, typeof(NoContentResult));
    }

    [TestMethod]
    public async Task GetTfesByAuthor_WhenNoClaimPresent_ReturnsUnauthorized()
    {
        SetNoUserClaims();

        var result = await _controller.GetTfesByAuthor();

        Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task GetTfesByAuthor_WhenSuccess_ReturnsOk()
    {
        SetUserClaims("user-1");
        _tfeServiceMock.Setup(s => s.GetTfesByAuthorIdAsync("user-1")).ReturnsAsync(new List<TfeDto> { new TfeDto { Id = 1 } });

        var result = await _controller.GetTfesByAuthor();

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    [TestMethod]
    public async Task GetTfesByAuthor_WhenServiceThrows_Returns500()
    {
        SetUserClaims("user-1");
        _tfeServiceMock.Setup(s => s.GetTfesByAuthorIdAsync("user-1"))
            .ThrowsAsync(new Exception("DB error"));

        var result = await _controller.GetTfesByAuthor();

        var statusResult = result as ObjectResult;
        Assert.AreEqual(500, statusResult?.StatusCode);
    }

    [TestMethod]
    public async Task DeleteTfe_WhenIdIsZeroOrNegative_ReturnsBadRequest()
    {
        SetUserClaims("user-1");

        var result = await _controller.DeleteTfe(0);

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task DeleteTfe_WhenNoClaimPresent_ReturnsUnauthorized()
    {
        SetNoUserClaims();

        var result = await _controller.DeleteTfe(1);

        Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task DeleteTfe_WhenTfeNotFoundOrNotOwner_ReturnsNotFound()
    {
        SetUserClaims("user-1");
        _tfeServiceMock.Setup(s => s.DeleteTfeAsync(99, "user-1")).ReturnsAsync(false);

        var result = await _controller.DeleteTfe(99);

        Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));
    }

    [TestMethod]
    public async Task DeleteTfe_WhenUnexpectedError_Returns500()
    {
        SetUserClaims("user-1");
        _tfeServiceMock.Setup(s => s.DeleteTfeAsync(1, "user-1"))
            .ThrowsAsync(new Exception("DB error"));

        var result = await _controller.DeleteTfe(1);

        var statusResult = result as ObjectResult;
        Assert.AreEqual(500, statusResult?.StatusCode);
    }

    [TestMethod]
    public async Task DeleteTfe_WhenSuccess_ReturnsNoContent()
    {
        SetUserClaims("user-1");
        _tfeServiceMock.Setup(s => s.DeleteTfeAsync(1, "user-1")).ReturnsAsync(true);

        var result = await _controller.DeleteTfe(1);

        Assert.IsInstanceOfType(result, typeof(NoContentResult));
    }

    [TestMethod]
    public async Task GetRecommendedTfes_WhenNoClaimPresent_ReturnsUnauthorized()
    {
        SetNoUserClaims();

        var result = await _controller.GetRecommendedTfes(new TfeRecommendedRequest());

        Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task GetRecommendedTfes_WhenSuccess_ReturnsOk()
    {
        SetUserClaims("user-1");
        _tfeServiceMock.Setup(s => s.GetRecommendedTfesAsync("user-1", It.IsAny<TfeRecommendedRequest>()))
            .ReturnsAsync(new TfeRecommendedResponse { Error = new OperationResult(true, string.Empty), Tfes = new List<TfeDto>(), TotalCount = 0 });

        var result = await _controller.GetRecommendedTfes(new TfeRecommendedRequest());

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    [TestMethod]
    public async Task GetRecommendedTfes_WhenUnexpectedError_Returns500()
    {
        SetUserClaims("user-1");
        _tfeServiceMock.Setup(s => s.GetRecommendedTfesAsync("user-1", It.IsAny<TfeRecommendedRequest>()))
            .ThrowsAsync(new Exception("DB error"));

        var result = await _controller.GetRecommendedTfes(new TfeRecommendedRequest());

        var statusResult = result as ObjectResult;
        Assert.AreEqual(500, statusResult?.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Proposals
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task CreateTfeProposal_WhenNoClaimPresent_ReturnsUnauthorized()
    {
        SetNoUserClaims();

        var result = await _controller.CreateTfeProposal(new TfeProposalCreationRequest { TfeId = 1 });

        Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task CreateTfeProposal_WhenDuplicateProposal_ReturnsConflict()
    {
        SetUserClaims("user-1");
        _proposalServiceMock.Setup(s => s.CreateTfeProposalAsync("user-1", It.IsAny<TfeProposalCreationRequest>()))
            .ReturnsAsync(new TfeProposalCreationResponse { Error = new OperationResult(false, "TFE already exists.", "DuplicateProposal") });

        var result = await _controller.CreateTfeProposal(new TfeProposalCreationRequest { TfeId = 1 });

        Assert.IsInstanceOfType(result, typeof(ConflictObjectResult));
    }

    [TestMethod]
    public async Task CreateTfeProposal_WhenTfeNotFound_ReturnsNotFound()
    {
        SetUserClaims("user-1");
        _proposalServiceMock.Setup(s => s.CreateTfeProposalAsync("user-1", It.IsAny<TfeProposalCreationRequest>()))
            .ReturnsAsync(new TfeProposalCreationResponse { Error = new OperationResult(false, "TFE not found.", "TfeNotFound") });

        var result = await _controller.CreateTfeProposal(new TfeProposalCreationRequest { TfeId = 99 });

        Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));
    }

    [TestMethod]
    public async Task CreateTfeProposal_WhenGenericError_ReturnsBadRequest()
    {
        SetUserClaims("user-1");
        _proposalServiceMock.Setup(s => s.CreateTfeProposalAsync("user-1", It.IsAny<TfeProposalCreationRequest>()))
            .ReturnsAsync(new TfeProposalCreationResponse { Error = new OperationResult(false, "Invalid proposal request.") });

        var result = await _controller.CreateTfeProposal(new TfeProposalCreationRequest { TfeId = 0 });

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task CreateTfeProposal_WhenSuccess_ReturnsOk()
    {
        SetUserClaims("user-1");
        _proposalServiceMock.Setup(s => s.CreateTfeProposalAsync("user-1", It.IsAny<TfeProposalCreationRequest>()))
            .ReturnsAsync(new TfeProposalCreationResponse { Error = new OperationResult(true, "Proposal created.") });

        var result = await _controller.CreateTfeProposal(new TfeProposalCreationRequest { TfeId = 1, IsInterested = true });

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    [TestMethod]
    public async Task UpdateTfeProposal_WhenProposalNotFound_ReturnsNotFound()
    {
        _proposalServiceMock.Setup(s => s.UpdateTfeProposalAsync(It.IsAny<TfeProposalUpdateRequest>()))
            .ReturnsAsync(new TfeProposalUpdateResponse { Error = new OperationResult(false, "No previous proposal exists.", "ProposalNotFound") });

        var result = await _controller.UpdateTfeProposal(new TfeProposalUpdateRequest { UserId = "user-1", TfeId = 1 });

        Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));
    }

    [TestMethod]
    public async Task UpdateTfeProposal_WhenTfeExpired_ReturnsConflict()
    {
        _proposalServiceMock.Setup(s => s.UpdateTfeProposalAsync(It.IsAny<TfeProposalUpdateRequest>()))
            .ReturnsAsync(new TfeProposalUpdateResponse { Error = new OperationResult(false, "This TFE has expired.", "TfeExpired") });

        var result = await _controller.UpdateTfeProposal(new TfeProposalUpdateRequest { UserId = "user-1", TfeId = 1 });

        Assert.IsInstanceOfType(result, typeof(ConflictObjectResult));
    }

    [TestMethod]
    public async Task UpdateTfeProposal_WhenGenericError_ReturnsBadRequest()
    {
        _proposalServiceMock.Setup(s => s.UpdateTfeProposalAsync(It.IsAny<TfeProposalUpdateRequest>()))
            .ReturnsAsync(new TfeProposalUpdateResponse { Error = new OperationResult(false, "User ID cannot be empty.") });

        var result = await _controller.UpdateTfeProposal(new TfeProposalUpdateRequest { UserId = "", TfeId = 1 });

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task UpdateTfeProposal_WhenSuccess_ReturnsOk()
    {
        _proposalServiceMock.Setup(s => s.UpdateTfeProposalAsync(It.IsAny<TfeProposalUpdateRequest>()))
            .ReturnsAsync(new TfeProposalUpdateResponse { Error = new OperationResult(true, "Proposal updated.") });

        var result = await _controller.UpdateTfeProposal(new TfeProposalUpdateRequest { UserId = "user-1", TfeId = 1, IsInterested = true });

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    [TestMethod]
    public async Task GetAcceptedMatches_WhenNoClaimPresent_ReturnsUnauthorized()
    {
        SetNoUserClaims();

        var result = await _controller.GetAcceptedMatches();

        Assert.IsInstanceOfType(result, typeof(UnauthorizedObjectResult));
    }

    [TestMethod]
    public async Task GetAcceptedMatches_WhenServiceReturnsError_ReturnsBadRequest()
    {
        SetUserClaims("user-1");
        _proposalServiceMock.Setup(s => s.GetAcceptedMatchesForUserAsync("user-1"))
            .ReturnsAsync(new GetAcceptedMatchesResponse { Error = new OperationResult(false, "Failed to retrieve matches.", "DatabaseError") });

        var result = await _controller.GetAcceptedMatches();

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task GetAcceptedMatches_WhenServiceThrows_Returns500()
    {
        SetUserClaims("user-1");
        _proposalServiceMock.Setup(s => s.GetAcceptedMatchesForUserAsync("user-1"))
            .ThrowsAsync(new Exception("Unexpected error"));

        var result = await _controller.GetAcceptedMatches();

        var statusResult = result as ObjectResult;
        Assert.AreEqual(500, statusResult?.StatusCode);
    }

    [TestMethod]
    public async Task GetAcceptedMatches_WhenSuccess_ReturnsOk()
    {
        SetUserClaims("user-1");
        _proposalServiceMock.Setup(s => s.GetAcceptedMatchesForUserAsync("user-1"))
            .ReturnsAsync(new GetAcceptedMatchesResponse { Error = new OperationResult(true, "1 match found."), Matches = new List<AcceptedMatchDto>(), TotalMatches = 1 });

        var result = await _controller.GetAcceptedMatches();

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    [TestMethod]
    public async Task DecideTfeCandidate_WhenNoClaimPresent_ReturnsUnauthorized()
    {
        SetNoUserClaims();

        var result = await _controller.DecideTfeCandidate(new TfeCandidateDecisionRequest { TfeId = 1, CandidateId = "user-2", Status = ProposalStatus.Accepted });

        Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task DecideTfeCandidate_WhenNotOwner_ReturnsForbid()
    {
        SetUserClaims("user-1");
        _proposalServiceMock.Setup(s => s.DecideTfeCandidateAsync("user-1", It.IsAny<TfeCandidateDecisionRequest>()))
            .ReturnsAsync(new TfeCandidateDecisionResponse { Error = new OperationResult(false, "No permission.", "Unauthorized") });

        var result = await _controller.DecideTfeCandidate(new TfeCandidateDecisionRequest { TfeId = 1, CandidateId = "user-2", Status = ProposalStatus.Accepted });

        Assert.IsInstanceOfType(result, typeof(ForbidResult));
    }

    [TestMethod]
    public async Task DecideTfeCandidate_WhenTfeNotFound_ReturnsNotFound()
    {
        SetUserClaims("user-1");
        _proposalServiceMock.Setup(s => s.DecideTfeCandidateAsync("user-1", It.IsAny<TfeCandidateDecisionRequest>()))
            .ReturnsAsync(new TfeCandidateDecisionResponse { Error = new OperationResult(false, "TFE not found.", "TfeNotFound") });

        var result = await _controller.DecideTfeCandidate(new TfeCandidateDecisionRequest { TfeId = 99, CandidateId = "user-2", Status = ProposalStatus.Accepted });

        Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));
    }

    [TestMethod]
    public async Task DecideTfeCandidate_WhenProposalAlreadyResolved_ReturnsConflict()
    {
        SetUserClaims("user-1");
        _proposalServiceMock.Setup(s => s.DecideTfeCandidateAsync("user-1", It.IsAny<TfeCandidateDecisionRequest>()))
            .ReturnsAsync(new TfeCandidateDecisionResponse { Error = new OperationResult(false, "Proposal already resolved.", "ProposalAlreadyResolved") });

        var result = await _controller.DecideTfeCandidate(new TfeCandidateDecisionRequest { TfeId = 1, CandidateId = "user-2", Status = ProposalStatus.Accepted });

        Assert.IsInstanceOfType(result, typeof(ConflictObjectResult));
    }

    [TestMethod]
    public async Task DecideTfeCandidate_WhenDatabaseError_Returns500()
    {
        SetUserClaims("user-1");
        _proposalServiceMock.Setup(s => s.DecideTfeCandidateAsync("user-1", It.IsAny<TfeCandidateDecisionRequest>()))
            .ReturnsAsync(new TfeCandidateDecisionResponse { Error = new OperationResult(false, "DB error.", "DatabaseError") });

        var result = await _controller.DecideTfeCandidate(new TfeCandidateDecisionRequest { TfeId = 1, CandidateId = "user-2", Status = ProposalStatus.Accepted });

        var statusResult = result as ObjectResult;
        Assert.AreEqual(500, statusResult?.StatusCode);
    }

    [TestMethod]
    public async Task DecideTfeCandidate_WhenGenericError_ReturnsBadRequest()
    {
        SetUserClaims("user-1");
        _proposalServiceMock.Setup(s => s.DecideTfeCandidateAsync("user-1", It.IsAny<TfeCandidateDecisionRequest>()))
            .ReturnsAsync(new TfeCandidateDecisionResponse { Error = new OperationResult(false, "Invalid decision request.") });

        var result = await _controller.DecideTfeCandidate(new TfeCandidateDecisionRequest { TfeId = 1, CandidateId = "", Status = ProposalStatus.Accepted });

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task DecideTfeCandidate_WhenSuccess_ReturnsOk()
    {
        SetUserClaims("user-1");
        _proposalServiceMock.Setup(s => s.DecideTfeCandidateAsync("user-1", It.IsAny<TfeCandidateDecisionRequest>()))
            .ReturnsAsync(new TfeCandidateDecisionResponse { Error = new OperationResult(true, "Candidate accepted."), Status = ProposalStatus.Accepted });

        var result = await _controller.DecideTfeCandidate(new TfeCandidateDecisionRequest { TfeId = 1, CandidateId = "user-2", Status = ProposalStatus.Accepted });

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    // -------------------------------------------------------------------------
    // ChangeTfeStatus
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task ChangeTfeStatus_WhenUserClaimsMissing_ReturnsUnauthorized()
    {
        SetNoUserClaims();

        var result = await _controller.ChangeTfeStatus(1, new TfeStatusUpdateRequest { Status = TfeStatus.Completed });

        Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task ChangeTfeStatus_WhenTfeNotFound_ReturnsNotFound()
    {
        SetUserClaims("user-1");
        _tfeServiceMock.Setup(s => s.ChangeTfeStatusAsync(99, TfeStatus.Completed, "user-1"))
            .ReturnsAsync(new OperationResult(false, "TFE not found.", "TfeNotFound"));

        var result = await _controller.ChangeTfeStatus(99, new TfeStatusUpdateRequest { Status = TfeStatus.Completed });

        Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));
    }

    [TestMethod]
    public async Task ChangeTfeStatus_WhenUnauthorized_ReturnsForbid()
    {
        SetUserClaims("user-1");
        _tfeServiceMock.Setup(s => s.ChangeTfeStatusAsync(1, TfeStatus.Completed, "user-1"))
            .ReturnsAsync(new OperationResult(false, "No permission.", "Unauthorized"));

        var result = await _controller.ChangeTfeStatus(1, new TfeStatusUpdateRequest { Status = TfeStatus.Completed });

        Assert.IsInstanceOfType(result, typeof(ForbidResult));
    }

    [TestMethod]
    public async Task ChangeTfeStatus_WhenInvalidCurrentStatus_ReturnsConflict()
    {
        SetUserClaims("user-1");
        _tfeServiceMock.Setup(s => s.ChangeTfeStatusAsync(1, TfeStatus.Completed, "user-1"))
            .ReturnsAsync(new OperationResult(false, "Only Open TFEs can be completed or cancelled.", "InvalidCurrentStatus"));

        var result = await _controller.ChangeTfeStatus(1, new TfeStatusUpdateRequest { Status = TfeStatus.Completed });

        Assert.IsInstanceOfType(result, typeof(ConflictObjectResult));
    }

    [TestMethod]
    public async Task ChangeTfeStatus_WhenSuccess_ReturnsOkWithUpdatedTfe()
    {
        SetUserClaims("user-1");
        _tfeServiceMock.Setup(s => s.ChangeTfeStatusAsync(1, TfeStatus.Completed, "user-1"))
            .ReturnsAsync(new OperationResult(true, "TFE status updated successfully."));
        _tfeServiceMock.Setup(s => s.GetTfeByIdAsync(1))
            .ReturnsAsync(new TfeDto { Id = 1, Status = TfeStatus.Completed });

        var result = await _controller.ChangeTfeStatus(1, new TfeStatusUpdateRequest { Status = TfeStatus.Completed });

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }
}
