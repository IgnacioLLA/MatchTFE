using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System;
using System.Threading.Tasks;
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var response = await _profileService.GetProfileByUserIdAsync(userId);

            if (response == null || response.Profile == null)
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
        public async Task<ActionResult<ProfileCreationResponse>> CreateInitialProfile([FromBody] ProfileCreationRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId) || userId != request.UserId)
                return Unauthorized();

            var response = await _profileService.CreateProfileAsync(request);

            if (!response.IsSuccess)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPut("profile")]
        public async Task<ActionResult<ProfileUpdateResponse>> UpdateProfile([FromBody] ProfileUpdateRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new ProfileUpdateResponse(false, "Could not identify the user."));
            }

            var response = await _profileService.UpdateProfileAsync(userId, request);

            if (response.IsSuccess)
            {
                return Ok(response);
            }

            return BadRequest(response);
        }

        [HttpGet("tfe/{TfeId}/candidates")]
        public async Task<ActionResult<ProfileByTfeInterestResponse>> GetInterestedCandidates([FromRoute] ProfileByTfeInterestRequest request)
        {
            var authorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(authorId)) return Unauthorized();

            var response = await _profileService.GetProfileByTfeInterestAsync(request);

            if (response == null || response.Interested == null || response.Interested.Count == 0)
                return NotFound("No candidates found for this TFE interest.");

            return Ok(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("profile/{userId}/role")]
        public async Task<ActionResult<RoleUpdateResponse>> ChangeRole([FromRoute] string userId, [FromBody] ChangeRoleRequest request)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest(new RoleUpdateResponse(false, "Invalid user ID."));
            }

            if (!Enum.IsDefined(typeof(RoleType), request.NewRole))
            {
                return BadRequest(new RoleUpdateResponse(false, "Invalid role type."));
            }

            var response = await _profileService.UpdateUserRoleAsync(userId, request.NewRole);

            if (!response.IsSuccess)
            {
                return NotFound(response);
            }

            return Ok(response);
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