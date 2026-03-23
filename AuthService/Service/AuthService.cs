using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using AuthService.Data;
using AuthService.Repositories;
using TFELibrary.Shared;

namespace AuthService.Service
{
    public class AuthService : IAuthService
    {
        public const int TokenLifetime = 1;
        public const int RefreshTokenLifetime = 2;

        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IAuthRepository authRepository, IConfiguration configuration)
        {
            _authRepository = authRepository;
            _configuration = configuration;
        }

        public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            var newUser = new MatchUser
            {
                UserName = request.Email,
                Email = request.Email,
                Name = request.Name,
                Surname = request.Surname
            };

            var result = await _authRepository.CreateUserAsync(newUser, request.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new RegisterResponseDto { IsSuccess = false, ErrorMessage = errors };
            }

            return new RegisterResponseDto { IsSuccess = true };
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            var user = await _authRepository.GetUserByEmailAsync(request.Email);

            if (user == null || !await _authRepository.CheckPasswordAsync(user, request.Password))
            {
                return new LoginResponseDto
                {
                    AuthData = new AuthResultDto { IsSuccess = false, ErrorMessage = "Invalid credentials." }
                };
            }

            var authResult = await GenerateAndSaveTokensAsync(user);

            return new LoginResponseDto
            {
                Name = user.Name,
                Surname = user.Surname,
                AuthData = authResult
            };
        }

        public async Task<RefreshTokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var refreshSecret = jwtSettings["RefreshSecret"]!;

            if (!IsValidToken(request.RefreshToken, refreshSecret))
            {
                return new RefreshTokenResponseDto { AuthData = new AuthResultDto { IsSuccess = false, ErrorMessage = "Token expirado, falso o incorrecto." } };
            }

            var user = await _authRepository.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                return new RefreshTokenResponseDto { AuthData = new AuthResultDto { IsSuccess = false, ErrorMessage = "User not found." } };
            }

            if (await _authRepository.GetRefreshTokenAsync(user) != request.RefreshToken)
            {
                return new RefreshTokenResponseDto { AuthData = new AuthResultDto { IsSuccess = false, ErrorMessage = "Token revocado o reemplazado." } };
            }

            var authResult = await GenerateAndSaveTokensAsync(user);

            return new RefreshTokenResponseDto { AuthData = authResult };
        }

        private async Task<AuthResultDto> GenerateAndSaveTokensAsync(MatchUser user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var accessSecret = jwtSettings["Secret"]!;
            var refreshSecret = jwtSettings["RefreshSecret"]!;

            var jwtToken = GenerateJwtToken(
                user,
                accessSecret,
                DateTime.UtcNow.AddMinutes(TokenLifetime));

            var newRefreshToken = GenerateJwtToken(
                user,
                refreshSecret,
                DateTime.UtcNow.AddMinutes(RefreshTokenLifetime));

            await _authRepository.SaveRefreshTokenAsync(user, newRefreshToken);

            return new AuthResultDto
            {
                IsSuccess = true,
                Token = jwtToken,
                RefreshToken = newRefreshToken,
                ErrorMessage = string.Empty
            };
        }

        private string GenerateJwtToken(MatchUser user, string secretKey, DateTime expiration)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: expiration,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<bool> LogoutAsync(string email)
        {
            var user = await _authRepository.GetUserByEmailAsync(email);
            if (user == null) return false;

            await _authRepository.RemoveRefreshTokenAsync(user);

            return true;
        }

        private bool IsValidToken(string token, string secretKey)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                tokenHandler.ValidateToken(token, validationParameters, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}