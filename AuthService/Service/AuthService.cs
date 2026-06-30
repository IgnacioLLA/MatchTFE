using AuthService.Data;
using AuthService.Parsing;
using AuthService.Repositories;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using TFELibrary.Shared;

namespace AuthService.Service;

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

    public async Task<(RegisterResponse Response, AuthTokenPair? Tokens)> RegisterAsync(RegisterRequest request)
    {
        if (request == null)
            return (new RegisterResponse
            {
                Error = new OperationResult(false, "Registration request cannot be null.")
            }, null);

        var newUser = new MatchUser
        {
            UserName = request.Email,
            Email = request.Email,
            Name = request.FirstName,
            Surname = request.LastName
        };

        var result = await _authRepository.CreateUserAsync(newUser, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            var isDuplicateUser = result.Errors.Any(e =>
                e.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase));

            return (new RegisterResponse
            {
                Error = new OperationResult(false, errors, isDuplicateUser ? "DuplicateEmail" : null)
            }, null);
        }

        var roleResult = await _authRepository.AddToRoleAsync(newUser, Roles.User);
        if (!roleResult.Succeeded)
            _logger.LogWarning("Failed to assign the 'User' role to user {UserId}.", newUser.Id);

        var (authResult, authTokens) = await GenerateAndSaveTokensAsync(newUser);

        var profileResult = await CreateUserProfileAsync(newUser, authTokens.AccessToken);
        if (!profileResult.IsSuccess)
        {
            await _authRepository.DeleteUserAsync(newUser);
            return (new RegisterResponse
            {
                Error = profileResult
            }, null);
        }

        return (new RegisterResponse
        {
            Error = new OperationResult(true, string.Empty)
        }, authTokens);
    }

    public async Task<(LoginResponse Response, AuthTokenPair? Tokens)> LoginAsync(LoginRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return (new LoginResponse { Error = new OperationResult(false, "Email and password are required.") }, null);

        var user = await _authRepository.GetUserByEmailAsync(request.Email);

        if (user == null || !await _authRepository.CheckPasswordAsync(user, request.Password))
        {
            return (new LoginResponse
            {
                Error = new OperationResult(false, "Invalid credentials.")
            }, null);
        }

        if (await _authRepository.IsLockedOutAsync(user))
        {
            return (new LoginResponse
            {
                Error = new OperationResult(false, "Account suspended.", "AccountSuspended")
            }, null);
        }

        var (authResult, authTokens) = await GenerateAndSaveTokensAsync(user);

        return (new LoginResponse
        {
            FirstName = user.Name,
            LastName = user.Surname,
            Error = authResult
        }, authTokens);
    }

    public async Task<(RefreshTokenResponse Response, AuthTokenPair? Tokens)> RefreshTokenAsync(RefreshTokenRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.RefreshToken))
            return (new RefreshTokenResponse { Error = new OperationResult(false, "Invalid refresh token request.") }, null);

        var refreshSecret = _configuration.GetSection("JwtSettings")["RefreshSecret"]!;

        if (!IsValidToken(request.RefreshToken, refreshSecret))
        {
            return (new RefreshTokenResponse { Error = new OperationResult(false, "Token expirado, falso o incorrecto.") }, null);
        }

        var user = await _authRepository.GetUserByIdAsync(request.UserId);

        if (user == null)
        {
            return (new RefreshTokenResponse { Error = new OperationResult(false, "User not found.") }, null);
        }

        if (await _authRepository.GetRefreshTokenAsync(user) != request.RefreshToken)
        {
            return (new RefreshTokenResponse { Error = new OperationResult(false, "Token revocado o reemplazado.") }, null);
        }

        var (authResult, authTokens) = await GenerateAndSaveTokensAsync(user);

        return (new RefreshTokenResponse { Error = authResult }, authTokens);
    }

    private async Task<(OperationResult Result, AuthTokenPair Tokens)> GenerateAndSaveTokensAsync(MatchUser user)
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

        return (
            new OperationResult(true, string.Empty),
            new AuthTokenPair(jwtToken, newRefreshToken)
        );
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
        catch (Exception)
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
            response.Error = new OperationResult(false, $"No user found with email '{request.Email}'.");
            return response;
        }

        if (user.Id == currentUserId)
        {
            response.Error = new OperationResult(false, "You cannot change your own role.");
            return response;
        }

        var newRoleString = request.NewRole.ToString();
        var currentRoles = await _authRepository.GetUserRolesAsync(user);

        if (currentRoles.Count == 1 && currentRoles.Contains(newRoleString, StringComparer.OrdinalIgnoreCase))
        {
            response.Error = new OperationResult(true, "The user already has this role assigned.");
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
            response.Error = new OperationResult(true, $"Role successfully updated to {newRoleString}.");
            await UpdateUserRoleInUserServiceAsync(user.Id, request.NewRole, ExtractBearerToken());
        }
        else
        {
            response.Error = new OperationResult(false, errorMessage);
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

        if (request == null || request.UserIds == null || !request.UserIds.Any())
        {
            response.Error = new OperationResult(false, "Request must include at least one user ID.");
            return response;
        }

        if (request.UserIds.Contains(currentUserId))
        {
            response.Error = new OperationResult(false, "You cannot perform actions on yourself.");
            return response;
        }

        if (request.Action == BulkUserActionType.Delete)
        {
            var users = await _authRepository.GetUsersByIdsAsync(request.UserIds);
            var adminToken = ExtractBearerToken();
            foreach (var user in users)
            {
                var result = await _authRepository.DeleteUserAsync(user);
                if (result.Succeeded)
                {
                    await DeleteUserProfileInUserServiceAsync(user.Id, adminToken);
                    response.AffectedCount++;
                }
                else
                {
                    _logger.LogWarning("Failed to delete user {UserId}: {Errors}", user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            response.Error = new OperationResult(true, $"Successfully deleted {response.AffectedCount} user(s).");
        }
        else if (request.Action == BulkUserActionType.Suspend)
        {
            var users = await _authRepository.GetUsersByIdsAsync(request.UserIds);
            var adminToken = ExtractBearerToken();
            foreach (var user in users)
            {
                await _authRepository.LockUserAsync(user);
                await UpdateUserSuspensionInUserServiceAsync(user.Id, true, adminToken);
                response.AffectedCount++;
            }

            response.Error = new OperationResult(true, $"Successfully suspended {response.AffectedCount} user(s).");
        }
        else if (request.Action == BulkUserActionType.Unsuspend)
        {
            var users = await _authRepository.GetUsersByIdsAsync(request.UserIds);
            var adminToken = ExtractBearerToken();
            foreach (var user in users)
            {
                await _authRepository.UnlockUserAsync(user);
                await UpdateUserSuspensionInUserServiceAsync(user.Id, false, adminToken);
                response.AffectedCount++;
            }

            response.Error = new OperationResult(true, $"Successfully reactivated {response.AffectedCount} user(s).");
        }
        else
        {
            response.Error = new OperationResult(false, "Action not supported.");
        }

        return response;
    }

    public async Task<BulkUserImportResponse> BulkImportUsersAsync(BulkUserImportRequest request)
    {
        var parseResult = UserImportFileParser.Parse(request.FileContent);

        if (!parseResult.IsValid)
        {
            return new BulkUserImportResponse
            {
                Error = new OperationResult(false, parseResult.ErrorMessage!, "ParseError")
            };
        }

        var adminToken = ExtractBearerToken();
        var response = new BulkUserImportResponse();

        foreach (var record in parseResult.Records)
        {
            var existingUser = await _authRepository.GetUserByEmailAsync(record.Email);
            if (existingUser != null)
            {
                response.SkippedCount++;
                continue;
            }

            var newUser = new MatchUser
            {
                UserName = record.Email,
                Email = record.Email,
                Name = record.FirstName,
                Surname = record.LastName
            };

            var createResult = await _authRepository.CreateUserAsync(newUser, record.Password);
            if (!createResult.Succeeded)
            {
                _logger.LogWarning(
                    "Failed to create user {Email} during bulk import: {Errors}",
                    record.Email,
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                continue;
            }

            await _authRepository.AddToRoleAsync(newUser, Roles.User);

            var (_, userTokens) = await GenerateAndSaveTokensAsync(newUser);

            var profileResult = await CreateUserProfileAsync(newUser, userTokens.AccessToken);
            if (!profileResult.IsSuccess)
            {
                await _authRepository.DeleteUserAsync(newUser);
                continue;
            }

            if (record.Role == RoleType.Teacher)
                await UpdateUserRoleInUserServiceAsync(newUser.Id, RoleType.Teacher, adminToken);

            response.CreatedCount++;
        }

        response.Error = new OperationResult(true, "Import completed successfully.");
        return response;
    }

    private async Task<OperationResult> CreateUserProfileAsync(MatchUser user, string userAccessToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("UserServiceClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userAccessToken);

            var httpResponse = await client.PostAsJsonAsync("api/user/profile", new ProfileCreationRequest(
                user.Id,
                new ProfileDto { FirstName = user.Name, LastName = user.Surname, Email = user.Email! }));

            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Profile creation failed for user {UserId}. StatusCode: {StatusCode}", user.Id, httpResponse.StatusCode);

                var profileResponse = await httpResponse.Content.ReadFromJsonAsync<ProfileCreationResponse>();
                var errorMessage = profileResponse?.Error.Message ?? "Could not create the user profile.";
                return new OperationResult(false, errorMessage, profileResponse?.Error.ErrorCode);
            }

            return new OperationResult(true, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error connecting to UserService for user {UserId}.", user.Id);
            return new OperationResult(false, "Could not create the user profile.");
        }
    }

    private async Task UpdateUserRoleInUserServiceAsync(string userId, RoleType role, string? adminToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("UserServiceClient");
            if (!string.IsNullOrEmpty(adminToken))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var roleResponse = await client.PutAsJsonAsync(
                $"api/user/profile/{userId}/role",
                new ChangeRoleRequest(role));

            if (!roleResponse.IsSuccessStatusCode)
                _logger.LogWarning("Role update for user {UserId} failed in UserService. StatusCode: {StatusCode}", userId, roleResponse.StatusCode);
            else
                _logger.LogInformation("Role for user {UserId} updated successfully in UserService.", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error connecting to UserService during role update for user {UserId}.", userId);
        }
    }

    private async Task UpdateUserSuspensionInUserServiceAsync(string userId, bool isSuspended, string? adminToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("UserServiceClient");
            if (!string.IsNullOrEmpty(adminToken))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var suspensionResponse = await client.PutAsJsonAsync(
                $"api/user/profile/{userId}/suspension",
                new SuspensionUpdateRequest(isSuspended));

            if (!suspensionResponse.IsSuccessStatusCode)
                _logger.LogWarning("Suspension update for user {UserId} failed in UserService. StatusCode: {StatusCode}", userId, suspensionResponse.StatusCode);
            else
                _logger.LogInformation("Suspension status for user {UserId} updated to {IsSuspended} in UserService.", userId, isSuspended);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error connecting to UserService during suspension update for user {UserId}.", userId);
        }
    }

    private async Task DeleteUserProfileInUserServiceAsync(string userId, string? adminToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("UserServiceClient");
            if (!string.IsNullOrEmpty(adminToken))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var deleteResponse = await client.DeleteAsync($"api/user/profile/{userId}");

            if (!deleteResponse.IsSuccessStatusCode)
                _logger.LogWarning("Profile deletion for user {UserId} failed in UserService. StatusCode: {StatusCode}", userId, deleteResponse.StatusCode);
            else
                _logger.LogInformation("Profile for user {UserId} deleted successfully in UserService.", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error connecting to UserService during profile deletion for user {UserId}.", userId);
        }
    }

    private string? ExtractBearerToken()
    {
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authHeader["Bearer ".Length..].Trim();

        return _httpContextAccessor.HttpContext?.Request.Cookies["AccessToken"];
    }

    public async Task<AdminPasswordChangeResponse> ChangeUserPasswordAsync(AdminPasswordChangeRequest request, string currentUserId)
    {
        var response = new AdminPasswordChangeResponse();

        if (request == null || string.IsNullOrWhiteSpace(request.Email))
        {
            response.Error = new OperationResult(false, "Email is required.");
            return response;
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            response.Error = new OperationResult(false, "Passwords do not match.", "PasswordMismatch");
            return response;
        }

        var user = await _authRepository.GetUserByEmailAsync(request.Email);

        if (user == null)
        {
            response.Error = new OperationResult(false, $"No user found with email '{request.Email}'.", "UserNotFound");
            return response;
        }

        var result = await _authRepository.ResetPasswordDirectlyAsync(user, request.NewPassword);

        if (!result.Succeeded)
        {
            var errorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
            response.Error = new OperationResult(false, errorMessage);
            return response;
        }

        response.Error = new OperationResult(true, "Password updated successfully.");
        return response;
    }
}
