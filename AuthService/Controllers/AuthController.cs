using AuthService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TFELibrary.Shared;

namespace AuthService.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase, IAuthController
    {
        private const int TokenCookieLifetime = 20;

        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _authService.LoginAsync(loginDto);

            if (!response.AuthData.IsSuccess)
            {
                return Unauthorized(response);
            }

            var jwtCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                // Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(TokenCookieLifetime)
            };

            var refreshCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                // Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(TokenCookieLifetime)
            };

            Response.Cookies.Append("AccessToken", response.AuthData.Token, jwtCookieOptions);
            Response.Cookies.Append("RefreshToken", response.AuthData.RefreshToken, refreshCookieOptions);

            return Ok(response);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RegisterAsync(request);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            var expiredToken = Request.Cookies["AccessToken"];
            var refreshToken = Request.Cookies["RefreshToken"];

            if (string.IsNullOrEmpty(expiredToken) || string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized(new { IsSuccess = false, ErrorMessage = "Session expired." });
            }

            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(expiredToken);

            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
                        ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new { IsSuccess = false, ErrorMessage = "Invalid Token." });
            }

            var requestDto = new RefreshTokenRequestDto
            {
                Email = email,
                RefreshToken = refreshToken
            };

            var response = await _authService.RefreshTokenAsync(requestDto);

            if (!response.AuthData.IsSuccess)
            {
                return Unauthorized(response);
            }

            var jwtCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                // Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(TokenCookieLifetime)
            };

            var refreshCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                // Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(TokenCookieLifetime)
            };

            Response.Cookies.Append("AccessToken", response.AuthData.Token, jwtCookieOptions);
            Response.Cookies.Append("RefreshToken", response.AuthData.RefreshToken, refreshCookieOptions);

            return Ok(response);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { IsSuccess = false, ErrorMessage = "Token inválido." });
            }

            var result = await _authService.LogoutAsync(email);

            if (!result)
            {
                return BadRequest(new { IsSuccess = false, ErrorMessage = "Error al cerrar sesión." });
            }

            return Ok(new { IsSuccess = true, Message = "Sesión cerrada correctamente." });
        }

        [HttpGet("test-auth")]
        [Authorize]
        public IActionResult TestAuth()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            return Ok(new
            {
                IsSuccess = true,
                Message = "¡Autenticación exitosa! El token es válido.",
                Email = email
            });
        }
    }
}
