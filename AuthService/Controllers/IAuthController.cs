using Microsoft.AspNetCore.Mvc;
using TFELibrary.Shared;

namespace AuthService.Controllers
{
    public interface IAuthController
    {
        Task<IActionResult> Login(LoginRequestDto loginDto);
        Task<IActionResult> Register(RegisterRequestDto request);
    }
}
