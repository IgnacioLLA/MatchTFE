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
    }
}