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

        public MatchController(ITagService tagService, ITfeService tfeService)
        {
            _tagService = tagService;
            _tfeService = tfeService;
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

        [HttpGet("tfe/author")]
        public async Task<IActionResult> GetTfesByAuthor()
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var authorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(authorId)) return Unauthorized();

            var tfes = await _tfeService.GetTfesByAuthorIdAsync(authorId);

            return Ok(tfes);
        }
    }
}
