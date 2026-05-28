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

        [HttpPut("tag/{id}")]
        Task<IActionResult> UpdateTag(int id, [FromBody] TagUpdateRequest request);

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
        [HttpDelete("tfe/{id}")]
        Task<IActionResult> DeleteTfe(int id);
        [HttpGet("tfe/recommended")]
        Task<IActionResult> GetRecommendedTfes([FromQuery] TfeRecommendedRequest request);
        [HttpPost("proposal/tfe")]
        Task<IActionResult> CreateTfeProposal([FromBody] TfeProposalCreationRequest request);
        [HttpPut("proposal/tfe")]
        Task<IActionResult> UpdateTfeProposal([FromBody] TfeProposalUpdateRequest request);

        [HttpGet("proposal/tfe/matches")]
        Task<IActionResult> GetAcceptedMatches();
    }
}
