using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TFELibrary.Shared;
using UserService.Service;

namespace UserService.Controllers;

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

        if (!response.Error.IsSuccess || response.Profile == null)
            return NotFound();

        return Ok(response);
    }

    [HttpGet("profile/{userId}")]
    public async Task<ActionResult<ProfileResponse>> GetProfileById([FromRoute] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("Invalid ID.");

        var response = await _profileService.GetProfileByUserIdAsync(userId);

        if (!response.Error.IsSuccess || response.Profile == null)
            return NotFound($"User not found: {userId}");

        return Ok(response);
    }

    [HttpPost("profile")]
    public async Task<ActionResult<ProfileCreationResponse>> CreateInitialProfile([FromBody] ProfileCreationRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (request == null)
            return BadRequest(new ProfileCreationResponse(new OperationResult(false, "Request payload cannot be null.")));

        if (string.IsNullOrWhiteSpace(userId) || userId != request.UserId)
            return Unauthorized();

        var response = await _profileService.CreateProfileAsync(request);

        if (!response.Error.IsSuccess)
        {
            if (response.Error.ErrorCode is "DuplicateEmail" or "DuplicateUserProfile")
                return Conflict(response);

            return BadRequest(response);
        }

        return CreatedAtAction(nameof(GetCurrentProfile), response);
    }

    [HttpPut("profile")]
    public async Task<ActionResult<ProfileUpdateResponse>> UpdateProfile([FromBody] ProfileUpdateRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new ProfileUpdateResponse(new OperationResult(false, "Could not identify the user.")));

        var response = await _profileService.UpdateProfileAsync(userId, request);

        if (response.Error.IsSuccess)
            return Ok(response);

        if (response.Error.ErrorCode == "UserNotFound")
            return NotFound(response);

        return BadRequest(response);
    }

    [HttpGet("tfe/{tfeId}/candidates")]
    [Authorize(Roles = "User")]
    public async Task<ActionResult<ProfileByTfeInterestResponse>> GetInterestedCandidates([FromRoute] int tfeId)
    {
        var authorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(authorId)) return Unauthorized();

        var response = await _profileService.GetProfileByTfeInterestAsync(new ProfileByTfeInterestRequest(tfeId));

        return Ok(response);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("profile/{userId}/role")]
    public async Task<ActionResult<RoleUpdateResponse>> ChangeRole([FromRoute] string userId, [FromBody] ChangeRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest(new RoleUpdateResponse(new OperationResult(false, "Invalid user ID.")));

        if (request == null)
            return BadRequest(new RoleUpdateResponse(new OperationResult(false, "Request body cannot be null.")));

        if (!Enum.IsDefined(typeof(RoleType), request.NewRole))
            return BadRequest(new RoleUpdateResponse(new OperationResult(false, "Invalid role type.")));

        var response = await _profileService.UpdateUserRoleAsync(userId, request.NewRole);

        if (response.Error.IsSuccess)
            return Ok(response);

        if (response.Error.ErrorCode == "UserNotFound")
            return NotFound(response);

        return BadRequest(response);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("profile/{userId}/suspension")]
    public async Task<ActionResult<SuspensionUpdateResponse>> UpdateSuspension([FromRoute] string userId, [FromBody] SuspensionUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest(new SuspensionUpdateResponse(new OperationResult(false, "Invalid user ID.")));

        if (request == null)
            return BadRequest(new SuspensionUpdateResponse(new OperationResult(false, "Request body cannot be null.")));

        var response = await _profileService.UpdateUserSuspensionAsync(userId, request.IsSuspended);

        if (response.Error.IsSuccess)
            return Ok(response);

        if (response.Error.ErrorCode == "UserNotFound")
            return NotFound(response);

        return BadRequest(response);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("profiles")]
    public async Task<ActionResult<GetAllProfilesResponse>> GetAllProfiles()
    {
        var request = new GetAllProfilesRequest();
        var response = await _profileService.GetAllProfilesAsync(request);

        return Ok(response);
    }

    [Authorize(Roles = "Service")]
    [HttpGet("notifications/pending")]
    public async Task<ActionResult<PendingNotificationsResponse>> GetPendingNotifications()
    {
        var response = await _profileService.GetUsersForNotificationAsync();
        return Ok(response);
    }

    [Authorize(Roles = "Service")]
    [HttpPut("notifications/mark-sent")]
    public async Task<IActionResult> MarkNotificationsSent([FromBody] MarkNotificationsSentRequest request)
    {
        if (request?.UserIds == null || request.UserIds.Count == 0)
            return BadRequest("UserIds cannot be empty.");

        await _profileService.MarkNotificationSentAsync(request.UserIds);
        return NoContent();
    }
}
