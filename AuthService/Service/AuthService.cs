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
            var user = await _authRepository.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                return new RefreshTokenResponseDto
                {
                    AuthData = new AuthResultDto { IsSuccess = false, ErrorMessage = "User not found." }
                };
            }

            var storedRefreshToken = await _authRepository.GetRefreshTokenAsync(user);
            if (storedRefreshToken != request.RefreshToken)
            {
                return new RefreshTokenResponseDto
                {
                    AuthData = new AuthResultDto { IsSuccess = false, ErrorMessage = "Invalid or expired token." }
                };
            }

            var authResult = await GenerateAndSaveTokensAsync(user);

            return new RefreshTokenResponseDto
            {
                AuthData = authResult
            };
        }

        private async Task<AuthResultDto> GenerateAndSaveTokensAsync(MatchUser user)
        {
            var jwtToken = GenerateJwtToken(user);

            var newRefreshToken = Guid.NewGuid().ToString("N");

            await _authRepository.SaveRefreshTokenAsync(user, newRefreshToken);

            return new AuthResultDto
            {
                IsSuccess = true,
                Token = jwtToken,
                RefreshToken = newRefreshToken,
                ErrorMessage = string.Empty
            };
        }
        private string GenerateJwtToken(MatchUser user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["Secret"];

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
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
                expires: DateTime.UtcNow.AddMinutes(TokenLifetime),
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
    }
}
