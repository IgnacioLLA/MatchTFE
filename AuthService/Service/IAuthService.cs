using TFELibrary.Shared;

namespace AuthService.Service
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
        Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<RefreshTokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request);
        Task<bool> LogoutAsync(string email);
        Task<bool> ChangeUserRoleAsync(string userId, string newRole);
    }
}
