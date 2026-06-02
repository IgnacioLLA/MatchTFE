using Microsoft.AspNetCore.Mvc;
using TFELibrary.Shared;

namespace UserService.Controllers
{
    public interface IUserController
    {
        [HttpGet("profile")]
        Task<ActionResult<ProfileResponse>> GetCurrentProfile();

        [HttpPost("profile")]
        Task<ActionResult<ProfileCreationResponse>> CreateInitialProfile(ProfileCreationRequest newProfile);

        [HttpPut("profile")]
        Task<ActionResult<ProfileUpdateResponse>> UpdateProfile(ProfileUpdateRequest request);

        [HttpGet("tfe/{tfeId}/candidates")]
        Task<ActionResult<ProfileByTfeInterestResponse>> GetInterestedCandidates([FromRoute] int tfeId);

        [HttpGet("profile/{userId}")]
        Task<ActionResult<ProfileResponse>> GetProfileById([FromRoute] string userId);

        [HttpPut("profile/{userId}/role")]
        Task<ActionResult<RoleUpdateResponse>> ChangeRole([FromRoute] string userId, [FromBody] ChangeRoleRequest request);

        [HttpGet("profiles")]
        Task<ActionResult<GetAllProfilesResponse>> GetAllProfiles();
    }
}
