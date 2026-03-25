using Microsoft.AspNetCore.Mvc;
using TFELibrary.Shared;

namespace UserService.Service
{
    public class UserService : IUserService
    {
        private readonly IUserService _userService;
        public UserService() { 
            _userService = new UserService();
        }

        public Task<ProfileResponse?> GetProfileByUserIdAsync(string userId)
        {
            throw new NotImplementedException();
        }
    }
}
