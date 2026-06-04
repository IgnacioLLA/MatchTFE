using AuthService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TFELibrary.Shared;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase, IAuthController
{
    private int TokenCookieLifetime;

    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
        TokenCookieLifetime = _authService.TokenLifetime;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto loginDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (response, tokens) = await _authService.LoginAsync(loginDto);

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

        if (tokens != null)
        {
            Response.Cookies.Append("AccessToken", tokens.AccessToken, jwtCookieOptions);
            Response.Cookies.Append("RefreshToken", tokens.RefreshToken, refreshCookieOptions);
        }

        return Ok(response);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (result, tokens) = await _authService.RegisterAsync(request);

        if (!result.Error.IsSuccess)
        {
            if (result.Error.ErrorCode is "DuplicateEmail" or "DuplicateUserProfile")
            {
                return Conflict(result);
            }

            return BadRequest(result);
        }

        if (tokens != null)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                // Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(TokenCookieLifetime)
            };

            Response.Cookies.Append("AccessToken", tokens.AccessToken, cookieOptions);
            Response.Cookies.Append("RefreshToken", tokens.RefreshToken, cookieOptions);
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

        string? userId;
        try
        {
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(expiredToken);
            userId = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value
                  ?? jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        }
        catch (Exception)
        {
            return Unauthorized(new { IsSuccess = false, ErrorMessage = "Invalid token format." });
        }

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { IsSuccess = false, ErrorMessage = "Invalid Token." });
        }

        var requestDto = new RefreshTokenRequestDto
        {
            UserId = userId,
            RefreshToken = refreshToken
        };

        var (response, tokens) = await _authService.RefreshTokenAsync(requestDto);

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

        if (tokens != null)
        {
            Response.Cookies.Append("AccessToken", tokens.AccessToken, jwtCookieOptions);
            Response.Cookies.Append("RefreshToken", tokens.RefreshToken, refreshCookieOptions);
        }

        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var authorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(authorId)) return Unauthorized();

        var result = await _authService.LogoutAsync(authorId);

        if (!result)
        {
            return BadRequest(new { IsSuccess = false, ErrorMessage = "Error al cerrar sesión." });
        }

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
        };

        Response.Cookies.Delete("AccessToken", cookieOptions);
        Response.Cookies.Delete("RefreshToken", cookieOptions);

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

    [HttpPut("role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ChangeRole([FromBody] UserRoleUpdateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new UserRoleUpdateResponse
            {
                Error = new OperationResult(false, "Invalid request format.")
            });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized(new UserRoleUpdateResponse { Error = new OperationResult(false, "Could not identify the current user.") });

        var response = await _authService.ChangeUserRoleAsync(request, currentUserId);

        if (!response.Error.IsSuccess)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("import")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkImportUsers([FromBody] BulkUserImportRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new BulkUserImportResponse
            {
                Error = new OperationResult(false, "Invalid file or request format.")
            });
        }

        var response = await _authService.BulkImportUsersAsync(request);

        return response.Error.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpPut("admin/password")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ChangeUserPassword([FromBody] AdminPasswordChangeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AdminPasswordChangeResponse
            {
                Error = new OperationResult(false, "Invalid request format.")
            });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized(new AdminPasswordChangeResponse { Error = new OperationResult(false, "Could not identify the current user.") });

        var response = await _authService.ChangeUserPasswordAsync(request, currentUserId);

        return response.Error.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpPost("bulk-action")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExecuteBulkAction([FromBody] BulkUserActionRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new BulkUserActionResponse
            {
                Error = new OperationResult(false, "Invalid request format.")
            });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized(new BulkUserActionResponse { Error = new OperationResult(false, "Could not identify the current user.") });

        var response = await _authService.ExecuteBulkActionAsync(request, currentUserId);

        if (!response.Error.IsSuccess)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }
}
