using Microsoft.AspNetCore.Mvc;
using TFELibrary.Shared;

namespace UserService.Controllers
{
    public interface IUserController
    {
        [HttpGet("profile")]
        Task<ActionResult<ProfileResponse>> GetCurrentProfile();
        [HttpPost("profile")]
        Task<ActionResult> CreateInitialProfile(ProfileCreationRequest newProfile);
        [HttpPut("profile")]
        Task<IActionResult> UpdateProfile(ProfileUpdateRequest request);
        [HttpGet("tfe/{request.TfeId}/candidates")]
        Task<IActionResult> GetInterestedCandidates([FromRoute] ProfileByTfeInterestRequest request);
        [HttpGet("profile/{userId}")]
        Task<ActionResult<ProfileResponse>> GetProfileById([FromRoute] string userId);

        [HttpPut("profile/{userId}/role")]
        Task<ActionResult<ChangeRoleResponse>> ChangeRole([FromRoute] string userId, [FromBody] ChangeRoleRequest request);
        
        [HttpGet("profiles")]
        Task<ActionResult<GetAllProfilesResponse>> GetAllProfiles();
    }
}
