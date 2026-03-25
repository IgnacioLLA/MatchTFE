using Microsoft.AspNetCore.Mvc;
using TFELibrary.Shared;

namespace UserService.Service
{
    public interface IUserService
    {
        Task<ProfileResponse?> GetProfileByUserIdAsync(string userId);
    }
}
