using TFELibrary.Shared;

namespace AuthService.Service;

public sealed record AuthTokenPair(string AccessToken, string RefreshToken);

public interface IAuthService
{
    int TokenLifetime { get; }
    int RefreshTokenLifetime { get; }
    Task<(LoginResponse Response, AuthTokenPair? Tokens)> LoginAsync(LoginRequest request);
    Task<(RegisterResponse Response, AuthTokenPair? Tokens)> RegisterAsync(RegisterRequest request);
    Task<(RefreshTokenResponse Response, AuthTokenPair? Tokens)> RefreshTokenAsync(RefreshTokenRequest request);
    Task<bool> LogoutAsync(string id);
    Task<UserRoleUpdateResponse> ChangeUserRoleAsync(UserRoleUpdateRequest request, string currentUserId);
    Task<BulkUserActionResponse> ExecuteBulkActionAsync(BulkUserActionRequest request, string currentUserId);
    Task<AdminPasswordChangeResponse> ChangeUserPasswordAsync(AdminPasswordChangeRequest request, string currentUserId);
    Task<BulkUserImportResponse> BulkImportUsersAsync(BulkUserImportRequest request);
}
