using TFELibrary.Data;

namespace UserService.Repositories
{
    public interface IUserRepository
    {
        Task<UserProfile?> GetByUserIdAsync(string userId);
    }
}
