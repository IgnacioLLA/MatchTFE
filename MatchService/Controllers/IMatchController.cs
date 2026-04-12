using Microsoft.AspNetCore.Mvc;
using TFELibrary.Shared;

namespace MatchService.Controllers
{
    public interface IMatchController
    {
        [HttpGet("tag")]
        Task<IActionResult> GetAllTags();

        [HttpGet("tag/{id}")]
        Task<IActionResult> GetTagById(int id);

        [HttpPost("tag")]
        Task<IActionResult> CreateTag([FromBody] TagCreationRequest request);

        [HttpDelete("tag/{id}")]
        Task<IActionResult> DeleteTag(int id);

        [HttpPost("tfe")]
        Task<IActionResult> CreateTfe([FromBody] TfeCreationRequest request);

        [HttpGet("tfe/{id}")]
        Task<IActionResult> GetTfeById(int id);

        [HttpGet("tfe/author")]
        Task<IActionResult> GetTfesByAuthor();
        [HttpPut("tfe/{id}")]
        Task<IActionResult> UpdateTfe(int id, [FromBody] TfeUpdateRequest request);
    }
}
