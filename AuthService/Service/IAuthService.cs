using TFELibrary.Shared;

namespace AuthService.Service
{
    public interface IAuthService
    {
        public int TokenLifetime { get; }
        public int RefreshTokenLifetime { get; }
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
        Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<RefreshTokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request);
        Task<bool> LogoutAsync(string id);
        Task<UserRoleUpdateResponse> ChangeUserRoleAsync(UserRoleUpdateRequest request, string currentUserId);
        Task<BulkUserActionResponse> ExecuteBulkActionAsync(BulkUserActionRequest request, string currentUserId);
    }
}
