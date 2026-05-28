using MatchService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TFELibrary.Shared;

namespace MatchService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MatchController : ControllerBase, IMatchController
    {
        private readonly ITagService _tagService;
        private readonly ITfeService _tfeService;
        private readonly IProposalService _proposalService;

        public MatchController(ITagService tagService, ITfeService tfeService, IProposalService proposalService)
        {
            _tagService = tagService;
            _tfeService = tfeService;
            _proposalService = proposalService;
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

            var created = await _tagService.CreateTagAsync(request);
            return CreatedAtAction(nameof(GetTagById), new { id = created.TagId }, created);
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
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("tag/{id}")]
        public async Task<IActionResult> DeleteTag(int id)
        {
            var deleted = await _tagService.DeleteTagAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }

        [HttpPost("tfe")]
        public async Task<IActionResult> CreateTfe([FromBody] TfeCreationRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var authorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(authorId)) return Unauthorized();

            var response = await _tfeService.CreateTfeAsync(request, authorId);

            return CreatedAtAction(nameof(GetTfeById), new { id = response.TfeId }, response.Tfe);
        }

        [HttpGet("tfe/{id}")]
        public async Task<IActionResult> GetTfeById(int id)
        {
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
            catch (Exception)
            {
                return StatusCode(500, "Ocurrió un error al actualizar el TFE.");
            }
        }

        [HttpGet("tfe/author")]
        public async Task<IActionResult> GetTfesByAuthor()
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var authorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(authorId)) return Unauthorized();

            var tfes = await _tfeService.GetTfesByAuthorIdAsync(authorId);

            return Ok(tfes);
        }

        [HttpDelete("tfe/{id}")]
        public async Task<IActionResult> DeleteTfe(int id)
        {
            var authorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(authorId)) return Unauthorized();

            var deleted = await _tfeService.DeleteTfeAsync(id, authorId);
            if (!deleted) return NotFound("TFE not found or you don't have permission to delete it.");

            return NoContent();
        }

        [HttpGet("tfe/recommended")]
        public async Task<IActionResult> GetRecommendedTfes([FromQuery] TfeRecommendedRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var response = await _tfeService.GetRecommendedTfesAsync(userId, request);
            return Ok(response);
        }

        [HttpPost("proposal/tfe")]
        public async Task<IActionResult> CreateTfeProposal([FromBody] TfeProposalCreationRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var response = await _proposalService.CreateTfeProposalAsync(userId, request);

            if (!response.Success) return Conflict(response.Message);
            return Ok(response);
        }

        [HttpPut("proposal/tfe")]
        public async Task<IActionResult> UpdateTfeProposal([FromBody] TfeProposalUpdateRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var response = await _proposalService.UpdateTfeProposalAsync(request);

            if (!response.Success) return Conflict(response.Message);
            return Ok(response);
        }

        [HttpGet("proposal/tfe/matches")]
        [HttpGet("matches/accepted")]
        public async Task<IActionResult> GetAcceptedMatches()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "Usuario no autenticado." });

            try
            {
                var response = await _proposalService.GetAcceptedMatchesForUserAsync(userId);

                if (!response.Success)
                    return BadRequest(response);

                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error al obtener los matches." });
            }
        }
    }
}
