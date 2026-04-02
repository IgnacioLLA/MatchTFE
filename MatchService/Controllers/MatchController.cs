using MatchService.Services;
using Microsoft.AspNetCore.Mvc;
using TFELibrary.Data;

namespace MatchService.Controllers
{
    public class MatchController : ControllerBase, IMatchController
    {
        private readonly ITagService _tagService;

        public MatchController(ITagService tagService)
        {
            _tagService = tagService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tags = await _tagService.GetAllTagsAsync();
            return Ok(tags);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var tag = await _tagService.GetTagByIdAsync(id);
            if (tag == null) return NotFound();
            return Ok(tag);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Tag tag)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var created = await _tagService.CreateTagAsync(tag);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _tagService.DeleteTagAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
