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
        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IAuthRepository authRepository, IConfiguration configuration)
        {
            _authRepository = authRepository;
            _configuration = configuration;
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

            var token = GenerateJwtToken(user);

            return new LoginResponseDto
            {
                AuthData = new AuthResultDto { IsSuccess = true, Token = token },
                Name = user.Name,
                Surname = user.Surname
            };
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

        private string GenerateJwtToken(MatchUser user)
        {
            var secretKey = _configuration["JwtSettings:Secret"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),

            new Claim("name", user.Name),
            new Claim("surname", user.Surname)
        };

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
