using TFELibrary.Data;

namespace UserService.Repositories
{
    public class UserRepository : IUserRepository
    {
        public Task<UserProfile?> GetByUserIdAsync(string userId)
        {
            throw new NotImplementedException();
        }
    }
}
