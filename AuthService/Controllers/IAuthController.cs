using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TFELibrary.Shared;

namespace AuthService.Controllers
{
    public interface IAuthController
    {
        [HttpPost("login")]
        Task<IActionResult> Login(LoginRequest loginDto);
        [HttpPost("register")]
        Task<IActionResult> Register(RegisterRequest request);
        [HttpPost("refresh")]
        Task<IActionResult> RefreshToken();
        [HttpPost("logout")]
        [Authorize]
        Task<IActionResult> Logout();
        [HttpPut("role")]
        [Authorize(Roles = "Admin")]
        Task<IActionResult> ChangeRole([FromBody] UserRoleUpdateRequest request);
        [HttpPost("import")]
        [Authorize(Roles = "Admin")]
        Task<IActionResult> BulkImportUsers([FromBody] BulkUserImportRequest request);
        [HttpPost("bulk-action")]
        [Authorize(Roles = "Admin")]
        Task<IActionResult> ExecuteBulkAction([FromBody] BulkUserActionRequest request);

        [HttpPut("admin/password")]
        [Authorize(Roles = "Admin")]
        Task<IActionResult> ChangeUserPassword([FromBody] AdminPasswordChangeRequest request);
    }
}
