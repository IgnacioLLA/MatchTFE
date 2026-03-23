using AuthService.Data;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Repositories
{
    public interface IAuthRepository
    {
        Task<MatchUser?> GetUserByEmailAsync(string email);
        Task<bool> CheckPasswordAsync(MatchUser user, string password);
        Task<IdentityResult> CreateUserAsync(MatchUser user, string password);
        Task SaveRefreshTokenAsync(MatchUser user, string refreshToken);
        Task<string> GetRefreshTokenAsync(MatchUser user);
        Task RemoveRefreshTokenAsync(MatchUser user);
    }
}
