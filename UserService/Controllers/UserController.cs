using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TFELibrary.Shared;
using UserService.Service;

namespace UserService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase, IUserController
    {
        private readonly IUserService _profileService;

        public UserController(IUserService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet("profile")]
        public async Task<ActionResult<ProfileResponse>> GetCurrentProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var response = await _profileService.GetProfileByUserIdAsync(userId);

            if (response == null)
            {
                return NotFound(new ProfileResponse(null));
            }

            return Ok(response);
        }

        [HttpGet("profile/{userId}")]
        public async Task<ActionResult<ProfileResponse>> GetProfileById([FromRoute] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest("Invalid ID.");
            }

            var response = await _profileService.GetProfileByUserIdAsync(userId);

            if (response == null || response.Profile == null)
            {
                return NotFound($"User not found: {userId}");
            }

            return Ok(response);
        }

        [HttpPost("profile")]
        public async Task<ActionResult> CreateInitialProfile([FromBody] ProfileCreationRequest newProfile)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null | userId != newProfile.UserId) return Unauthorized();

            var response = await _profileService.CreateProfileAsync(newProfile);

            if (!response)
            {
                return NotFound(new ProfileResponse(null));
            }

            return Ok(response);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "No se pudo identificar al usuario." });
            }

            var response = await _profileService.UpdateProfileAsync(userId, request);

            if (response.Success)
            {
                return Ok(response);
            }

            return BadRequest(response);
        }

        [HttpGet("tfe/{request.TfeId}/candidates")]
        public async Task<IActionResult> GetInterestedCandidates([FromRoute] ProfileByTfeInterestRequest request)
        {
            var authorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(authorId)) return Unauthorized();

            try
            {
                var interestedProfiles = await _profileService.GetProfileByTfeInterest(request);

                if (interestedProfiles == null)
                    return NotFound("Tfe not found.");

                var response = interestedProfiles;

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("profile/{userId}/role")]
        public async Task<ActionResult<ChangeRoleResponse>> ChangeRole([FromRoute] string userId, [FromBody] ChangeRoleRequest request)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest(new ChangeRoleResponse(false, "Invalid user ID."));
            }

            if (!Enum.IsDefined(typeof(RoleType), request.NewRole))
            {
                return BadRequest(new ChangeRoleResponse(false, "Invalid role type."));
            }

            var result = await _profileService.UpdateUserRoleAsync(userId, request.NewRole);
            if (!result) 
            {
                return NotFound(new ChangeRoleResponse(false, "User profile not found."));
            }

            return Ok(new ChangeRoleResponse(true, "Role updated successfully."));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("profiles")]
        public async Task<ActionResult<GetAllProfilesResponse>> GetAllProfiles()
        {
            var request = new GetAllProfilesRequest();
            var response = await _profileService.GetAllProfilesAsync(request);

            return Ok(response);
        }
    }
}