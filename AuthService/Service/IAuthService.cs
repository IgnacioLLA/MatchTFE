using TFELibrary.Shared;

namespace AuthService.Service;

public sealed record AuthTokenPair(string AccessToken, string RefreshToken);

public interface IAuthService
{
    int TokenLifetime { get; }
    int RefreshTokenLifetime { get; }
    Task<(LoginResponseDto Response, AuthTokenPair? Tokens)> LoginAsync(LoginRequestDto request);
    Task<(RegisterResponseDto Response, AuthTokenPair? Tokens)> RegisterAsync(RegisterRequestDto request);
    Task<(RefreshTokenResponseDto Response, AuthTokenPair? Tokens)> RefreshTokenAsync(RefreshTokenRequestDto request);
    Task<bool> LogoutAsync(string id);
    Task<UserRoleUpdateResponse> ChangeUserRoleAsync(UserRoleUpdateRequest request, string currentUserId);
    Task<BulkUserActionResponse> ExecuteBulkActionAsync(BulkUserActionRequest request, string currentUserId);
}
