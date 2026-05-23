using TFELibrary.Shared;

namespace AuthService.Service
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
        Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<RefreshTokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request);
        Task<bool> LogoutAsync(string id);
        Task<UserRoleUpdateResponse> ChangeUserRoleAsync(UserRoleUpdateRequest request);
        Task<BulkUserActionResponse> ExecuteBulkActionAsync(BulkUserActionRequest request);
    }
}
