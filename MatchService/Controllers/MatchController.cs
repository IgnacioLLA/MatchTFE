using MatchService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TFELibrary.Shared;

namespace MatchService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MatchController : ControllerBase, IMatchController
    {
        private readonly ITagService _tagService;

        public MatchController(ITagService tagService)
        {
            _tagService = tagService;
        }

        [HttpGet("tag")]
        public async Task<IActionResult> GetAll()
        {
            var tags = await _tagService.GetAllTagsAsync();
            return Ok(tags);
        }

        [HttpGet("tag/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var tag = await _tagService.GetTagByIdAsync(id);
            if (tag == null) return NotFound();
            return Ok(tag);
        }

        [HttpPost("tag")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] TagCreationRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var created = await _tagService.CreateTagAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.Name }, created);
        }

        [HttpDelete("tag/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _tagService.DeleteTagAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
