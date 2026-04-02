using AuthService.Data;
using AuthService.Repositories;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using TFELibrary.Shared;

namespace AuthService.Service
{
    public class AuthService : IAuthService
    {
        public const int TokenLifetime = 15;
        public const int RefreshTokenLifetime = 25;

        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AuthService> _logger;
        private readonly JwtSecurityTokenHandler _tokenHandler = new JwtSecurityTokenHandler();

        public AuthService(IAuthRepository authRepository, IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<AuthService> logger)
        {
            _authRepository = authRepository;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
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

            var roleResult = await _authRepository.AddToRoleAsync(newUser, "User");
            if (!roleResult.Succeeded)
                _logger.LogWarning($"No se pudo asignar el rol 'User' al usuario {newUser.Id}.");


            // Generación de tokens para el nuevo usuario

            var authResult = await GenerateAndSaveTokensAsync(newUser);
            try
            {
                var client = _httpClientFactory.CreateClient("UserServiceClient");

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);

                ProfileCreationRequest profile = new ProfileCreationRequest(

                    newUser.Id,
                    new ProfileDto
                    {
                        FirstName = newUser.Name,
                        LastName = newUser.Surname,
                        Email = newUser.Email
                    }
                );

                var response = await client.PostAsJsonAsync("api/user/profile", profile);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Usuario {newUser.Id} registrado, pero falló la creación del perfil en UserService. StatusCode: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico conectando con UserService durante el registro.");
            }

            return new RegisterResponseDto
            {
                IsSuccess = true,
                ErrorMessage = string.Empty,
                AuthData = authResult
            };
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
            var refreshSecret = _configuration.GetSection("JwtSettings")["RefreshSecret"]!;

            if (!IsValidToken(request.RefreshToken, refreshSecret))
            {
                return new RefreshTokenResponseDto { AuthData = new AuthResultDto { IsSuccess = false, ErrorMessage = "Token expirado, falso o incorrecto." } };
            }

            var user = await _authRepository.GetUserByIdAsync(request.UserId);

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

            var roles = await _authRepository.GetUserRolesAsync(user);

            var jwtToken = GenerateJwtToken(
                user,
                accessSecret,
                DateTime.UtcNow.AddMinutes(TokenLifetime),
                roles);

            var newRefreshToken = GenerateJwtToken(
                user,
                refreshSecret,
                DateTime.UtcNow.AddMinutes(RefreshTokenLifetime),
                roles);

            await _authRepository.SaveRefreshTokenAsync(user, newRefreshToken);

            return new AuthResultDto
            {
                IsSuccess = true,
                Token = jwtToken,
                RefreshToken = newRefreshToken,
                ErrorMessage = string.Empty
            };
        }

        private string GenerateJwtToken(MatchUser user, string secretKey, DateTime expiration, IList<string> roles)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                //new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: expiration,
                signingCredentials: creds
            );

            return _tokenHandler.WriteToken(token);
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
                _tokenHandler.ValidateToken(token, validationParameters, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ChangeUserRoleAsync(string userId, string newRole)
        {
            var user = await _authRepository.GetUserByIdAsync(userId);
            if (user == null) return false;

            var currentRoles = await _authRepository.GetUserRolesAsync(user);

            foreach (var role in currentRoles)
            {
                var removeResult = await _authRepository.RemoveFromRoleAsync(user, role);
                if (!removeResult.Succeeded)
                {
                    _logger.LogWarning($"No se pudo quitar el rol '{role}' al usuario {userId}.");
                    return false;
                }
            }

            var addResult = await _authRepository.AddToRoleAsync(user, newRole);
            if (!addResult.Succeeded)
            {
                _logger.LogWarning($"No se pudo asignar el rol '{newRole}' al usuario {userId}.");
                return false;
            }

            return true;
        }
    }
}