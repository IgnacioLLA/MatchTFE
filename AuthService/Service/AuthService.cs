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
        public int TokenLifetime { get; } = 15;
        public int RefreshTokenLifetime { get; } = 25;

        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AuthService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JwtSecurityTokenHandler _tokenHandler = new JwtSecurityTokenHandler();

        public AuthService(IAuthRepository authRepository, IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<AuthService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _authRepository = authRepository;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
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
                var isDuplicateUser = result.Errors.Any(e =>
                    e.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase));

                return new RegisterResponseDto
                {
                    Error = new ErrorRecord(false, errors, isDuplicateUser ? "DuplicateEmail" : null),
                    AuthData = new AuthResultDto { IsSuccess = false, Message = errors }
                };
            }

            var roleResult = await _authRepository.AddToRoleAsync(newUser, Roles.User);
            if (!roleResult.Succeeded)
                _logger.LogWarning("Failed to assign the 'User' role to user {UserId}.", newUser.Id);

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
                    _logger.LogWarning("User {UserId} registered but profile creation in UserService failed. StatusCode: {StatusCode}", newUser.Id, response.StatusCode);

                    var profileResponse = await response.Content.ReadFromJsonAsync<ProfileCreationResponse>();
                    var errorMessage = profileResponse?.Error.Message ?? "Could not create the user profile.";

                    await _authRepository.DeleteUserAsync(newUser);

                    return new RegisterResponseDto
                    {
                        Error = new ErrorRecord(false, errorMessage, profileResponse?.Error.ErrorCode),
                        AuthData = new AuthResultDto { IsSuccess = false, Message = errorMessage }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error connecting to UserService during registration.");
                await _authRepository.DeleteUserAsync(newUser);

                const string errorMessage = "Could not create the user profile.";
                return new RegisterResponseDto
                {
                    Error = new ErrorRecord(false, errorMessage),
                    AuthData = new AuthResultDto { IsSuccess = false, Message = errorMessage }
                };
            }

            return new RegisterResponseDto
            {
                Error = new ErrorRecord(true, string.Empty),
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
                    AuthData = new AuthResultDto { IsSuccess = false, Message = "Invalid credentials." }
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
                return new RefreshTokenResponseDto { AuthData = new AuthResultDto { IsSuccess = false, Message = "Token expirado, falso o incorrecto." } };
            }

            var user = await _authRepository.GetUserByIdAsync(request.UserId);

            if (user == null)
            {
                return new RefreshTokenResponseDto { AuthData = new AuthResultDto { IsSuccess = false, Message = "User not found." } };
            }

            if (await _authRepository.GetRefreshTokenAsync(user) != request.RefreshToken)
            {
                return new RefreshTokenResponseDto { AuthData = new AuthResultDto { IsSuccess = false, Message = "Token revocado o reemplazado." } };
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
                Message = string.Empty
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

        public async Task<bool> LogoutAsync(string id)
        {
            var user = await _authRepository.GetUserByIdAsync(id);
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

        public async Task<UserRoleUpdateResponse> ChangeUserRoleAsync(UserRoleUpdateRequest request, string currentUserId)
        {
            var response = new UserRoleUpdateResponse();
            var user = await _authRepository.GetUserByEmailAsync(request.Email);

            if (user == null)
            {
                response.Error = new ErrorRecord(false, $"No user found with email '{request.Email}'.");
                return response;
            }

            if (user.Id == currentUserId)
            {
                response.Error = new ErrorRecord(false, "You cannot change your own role.");
                return response;
            }

            var newRoleString = request.NewRole.ToString();
            var currentRoles = await _authRepository.GetUserRolesAsync(user);

            if (currentRoles.Count == 1 && currentRoles.Contains(newRoleString, StringComparer.OrdinalIgnoreCase))
            {
                response.Error = new ErrorRecord(true, "The user already has this role assigned.");
                return response;
            }

            var isExecutionOk = true;
            var errorMessage = string.Empty;

            foreach (var role in currentRoles)
            {
                var removeResult = await _authRepository.RemoveFromRoleAsync(user, role);
                if (!removeResult.Succeeded)
                {
                    errorMessage = $"Failed to remove the previous role '{role}'.";
                    isExecutionOk = false;
                    break;
                }
            }

            if (isExecutionOk)
            {
                newRoleString = GetAspNetRole(request.NewRole);
                var addResult = await _authRepository.AddToRoleAsync(user, newRoleString);
                if (!addResult.Succeeded)
                {
                    errorMessage = $"Failed to assign the new role '{newRoleString}'.";
                    isExecutionOk = false;
                }
            }

            if (isExecutionOk)
            {
                response.Error = new ErrorRecord(true, $"Role successfully updated to {newRoleString}.");

                try
                {
                    var client = _httpClientFactory.CreateClient("UserServiceClient");

                    var token = string.Empty;
                    var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
                    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        token = authHeader.Substring("Bearer ".Length).Trim();
                    }
                    else
                    {
                        token = _httpContextAccessor.HttpContext?.Request.Cookies["AccessToken"];
                    }

                    if (!string.IsNullOrEmpty(token))
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }

                    var changeRoleReq = new ChangeRoleRequest(request.NewRole);
                    var apiResponse = await client.PutAsJsonAsync($"api/user/profile/{user.Id}/role", changeRoleReq);

                    if (!apiResponse.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Role for user {UserId} was updated locally but failed in UserService. StatusCode: {StatusCode}", user.Id, apiResponse.StatusCode);
                    }
                    else
                    {
                        _logger.LogInformation("Role for user {UserId} updated successfully in UserService.", user.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Critical error connecting to UserService during role change.");
                }
            }
            else
            {
                response.Error = new ErrorRecord(false, errorMessage);
            }

            return response;
        }

        private static string GetAspNetRole(RoleType role)
        {
            if (role == RoleType.Admin)
                return Roles.Admin;
            if (role == RoleType.Teacher || role == RoleType.Student)
                return Roles.User;
            throw new ArgumentException("Invalid role.");
        }

        public async Task<BulkUserActionResponse> ExecuteBulkActionAsync(BulkUserActionRequest request, string currentUserId)
        {
            var response = new BulkUserActionResponse { AffectedCount = 0 };

            if (request.UserIds.Contains(currentUserId))
            {
                response.Error = new ErrorRecord(false, "You cannot perform actions on yourself.");
                return response;
            }

            if (request.Action == BulkUserActionType.Delete)
            {
                var users = await _authRepository.GetUsersByIdsAsync(request.UserIds);
                foreach (var user in users)
                {
                    var result = await _authRepository.DeleteUserAsync(user);
                    if (result.Succeeded)
                    {
                        response.AffectedCount++;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to delete user {UserId}: {Errors}", user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }

                response.Error = new ErrorRecord(true, $"Successfully deleted {response.AffectedCount} user(s).");
            }
            else
            {
                response.Error = new ErrorRecord(false, "Action not supported.");
            }

            return response;
        }
    }
}
