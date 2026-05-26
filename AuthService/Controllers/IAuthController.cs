using Microsoft.AspNetCore.Mvc;
using TFELibrary.Shared;

namespace AuthService.Controllers
{
    public interface IAuthController
    {
        Task<IActionResult> Login(LoginRequestDto loginDto);
        Task<IActionResult> Register(RegisterRequestDto request);
        Task<IActionResult> Logout();
        Task<IActionResult> RefreshToken();

        Task<IActionResult> ChangeRole([FromBody] UserRoleUpdateRequest request);
        Task<IActionResult> BulkImportUsers([FromBody] BulkUserImportRequest request);
        Task<IActionResult> ExecuteBulkAction([FromBody] BulkUserActionRequest request);
    }
}
