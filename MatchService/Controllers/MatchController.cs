using MatchService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TFELibrary.Shared;

namespace MatchService.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MatchController : ControllerBase, IMatchController
{
    private readonly ITagService _tagService;
    private readonly ITfeService _tfeService;
    private readonly IProposalService _proposalService;
    private readonly ILogger<MatchController> _logger;

    public MatchController(ITagService tagService, ITfeService tfeService, IProposalService proposalService, ILogger<MatchController> logger)
    {
        _tagService = tagService;
        _tfeService = tfeService;
        _proposalService = proposalService;
        _logger = logger;
    }

    [HttpGet("tag")]
    public async Task<IActionResult> GetAllTags()
    {
        var tags = await _tagService.GetAllTagsAsync();
        return Ok(tags);
    }

    [HttpGet("tag/{id}")]
    public async Task<IActionResult> GetTagById(int id)
    {
        var tag = await _tagService.GetTagByIdAsync(id);
        if (tag == null) return NotFound();
        return Ok(tag);
    }

    [HttpPost("tag")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateTag([FromBody] TagCreationRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var created = await _tagService.CreateTagAsync(request);
            return CreatedAtAction(nameof(GetTagById), new { id = created.TagId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut("tag/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateTag(int id, [FromBody] TagUpdateRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var updated = await _tagService.UpdateTagAsync(id, request);
            if (!updated) return NotFound();
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpDelete("tag/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTag(int id)
    {
        var deleted = await _tagService.DeleteTagAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("tfe")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> CreateTfe([FromBody] TfeCreationRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var authorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(authorId)) return Unauthorized();

            var response = await _tfeService.CreateTfeAsync(request, authorId);

            return CreatedAtAction(nameof(GetTfeById), new { id = response.TfeId }, response.Tfe);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpGet("tfe/{id}")]
    public async Task<IActionResult> GetTfeById(int id)
    {
        if (id <= 0) return BadRequest("Invalid TFE ID.");

        var tfe = await _tfeService.GetTfeByIdAsync(id);
        if (tfe == null) return NotFound();
        return Ok(tfe);
    }

    [HttpPut("tfe/{id}")]
    public async Task<IActionResult> UpdateTfe(int id, [FromBody] TfeUpdateRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var authorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(authorId)) return Unauthorized();

        try
        {
            var isUpdated = await _tfeService.UpdateTfeAsync(id, request, authorId);

            if (!isUpdated)
                return NotFound("La propuesta no existe o no tienes permisos para editarla.");

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating TFE {TfeId} for user {UserId}.", id, authorId);
            return StatusCode(500, "Ocurrió un error al actualizar el TFE.");
        }
    }

    [HttpGet("tfe/author")]
    public async Task<IActionResult> GetTfesByAuthor()
    {
        var authorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(authorId)) return Unauthorized();

        try
        {
            var tfes = await _tfeService.GetTfesByAuthorIdAsync(authorId);
            return Ok(tfes);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving TFEs for author {AuthorId}.", authorId);
            return StatusCode(500, "Ocurrió un error al obtener los TFEs.");
        }
    }

    [HttpDelete("tfe/{id}")]
    public async Task<IActionResult> DeleteTfe(int id)
    {
        if (id <= 0) return BadRequest("Invalid TFE ID.");

        var authorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(authorId)) return Unauthorized();

        try
        {
            var deleted = await _tfeService.DeleteTfeAsync(id, authorId);
            if (!deleted) return NotFound("TFE not found or you don't have permission to delete it.");
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting TFE {TfeId} for user {UserId}.", id, authorId);
            return StatusCode(500, "Ocurrió un error al eliminar el TFE.");
        }
    }

    [HttpGet("tfe/recommended")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> GetRecommendedTfes([FromQuery] TfeRecommendedRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        try
        {
            var response = await _tfeService.GetRecommendedTfesAsync(userId, request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving recommended TFEs for user {UserId}.", userId);
            return StatusCode(500, "Ocurrió un error al obtener los TFEs recomendados.");
        }
    }

    [HttpPost("proposal/tfe")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> CreateTfeProposal([FromBody] TfeProposalCreationRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var response = await _proposalService.CreateTfeProposalAsync(userId, request);

        if (!response.Error.IsSuccess)
        {
            return response.Error.ErrorCode switch
            {
                "DuplicateProposal" or "TfeExpired" => Conflict(response.Error.Message),
                "TfeNotFound" => NotFound(response.Error.Message),
                "DatabaseError" => StatusCode(500, response.Error.Message),
                _ => BadRequest(response.Error.Message)
            };
        }
        return Ok(response);
    }

    [HttpPut("proposal/tfe")]
    public async Task<IActionResult> UpdateTfeProposal([FromBody] TfeProposalUpdateRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var response = await _proposalService.UpdateTfeProposalAsync(request);

        if (!response.Error.IsSuccess)
        {
            return response.Error.ErrorCode switch
            {
                "TfeExpired" or "InvalidProposalStatus" => Conflict(response.Error.Message),
                "TfeNotFound" or "ProposalNotFound" => NotFound(response.Error.Message),
                "DatabaseError" => StatusCode(500, response.Error.Message),
                _ => BadRequest(response.Error.Message)
            };
        }
        return Ok(response);
    }

    [HttpGet("proposal/tfe/matches")]
    public async Task<IActionResult> GetAcceptedMatches()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Usuario no autenticado." });

        try
        {
            var response = await _proposalService.GetAcceptedMatchesForUserAsync(userId);

            if (!response.Error.IsSuccess)
                return BadRequest(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving accepted matches for user {UserId}.", userId);
            return StatusCode(500, new { message = "Error al obtener los matches." });
        }
    }

    [HttpPut("proposal/tfe/decision")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> DecideTfeCandidate([FromBody] TfeCandidateDecisionRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var authorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(authorId)) return Unauthorized();

        var response = await _proposalService.DecideTfeCandidateAsync(authorId, request);

        if (!response.Error.IsSuccess)
        {
            return response.Error.ErrorCode switch
            {
                "Unauthorized" => Forbid(),
                "TfeNotFound" or "ProposalNotFound" => NotFound(new { message = response.Error.Message }),
                "TfeExpired" or "ProposalAlreadyResolved" or "InvalidStatus" => Conflict(new { message = response.Error.Message }),
                "DatabaseError" => StatusCode(500, new { message = response.Error.Message }),
                _ => BadRequest(new { message = response.Error.Message })
            };
        }
        return Ok(response);
    }
}
