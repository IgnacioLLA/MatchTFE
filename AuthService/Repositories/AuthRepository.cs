using AuthService.Repositories;
using AuthService.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MatchTFE.AuthService.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly UserManager<MatchUser> _userManager;

    public AuthRepository(UserManager<MatchUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<MatchUser?> GetUserByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<bool> CheckPasswordAsync(MatchUser user, string password)
    {
        return await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task<IdentityResult> CreateUserAsync(MatchUser user, string password)
    {
        return await _userManager.CreateAsync(user, password);
    }

    public async Task<IdentityResult> DeleteUserAsync(MatchUser user)
    {
        return await _userManager.DeleteAsync(user);
    }

    public async Task SaveRefreshTokenAsync(MatchUser user, string refreshToken)
    {
        await _userManager.SetAuthenticationTokenAsync(user, "MatchTFE", "RefreshToken", refreshToken);
    }

    public async Task<string?> GetRefreshTokenAsync(MatchUser user)
    {
        return await _userManager.GetAuthenticationTokenAsync(user, "MatchTFE", "RefreshToken");
    }
    public async Task RemoveRefreshTokenAsync(MatchUser user)
    {
        await _userManager.RemoveAuthenticationTokenAsync(user, "MatchTFE", "RefreshToken");
    }

    public async Task<MatchUser?> GetUserByIdAsync(string userId)
    {
        return await _userManager.FindByIdAsync(userId);
    }

    public async Task<List<MatchUser>> GetUsersByIdsAsync(IEnumerable<string> userIds)
    {
        var idList = userIds.ToList();
        return await _userManager.Users
            .Where(u => idList.Contains(u.Id))
            .ToListAsync();
    }
    public async Task<IList<string>> GetUserRolesAsync(MatchUser user)
    {
        return await _userManager.GetRolesAsync(user);
    }
    public async Task<IdentityResult> AddToRoleAsync(MatchUser user, string role)
    {
        return await _userManager.AddToRoleAsync(user, role);
    }

    public async Task<IdentityResult> RemoveFromRoleAsync(MatchUser user, string role)
    {
        return await _userManager.RemoveFromRoleAsync(user, role);
    }
}