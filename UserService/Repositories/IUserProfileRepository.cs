using TFELibrary.Data;
using TFELibrary.Shared;

namespace UserService.Repositories
{
    public interface IUserProfileRepository
    {
        Task<UserProfile> CreateProfileAsync(UserProfile profile);
        Task<UserProfile?> GetByUserIdAsync(string userId);
        Task<bool> UpdateUserProfileAsync(UserProfile entity, List<string> interests, List<SkillDto> skills);
        Task<List<UserProfile>> GetInterestedUsersByTfeIdInUserServiceAsync(int tfeId);
        Task<bool> UpdateUserRoleAsync(string userId, RoleType newRole);
        Task<bool> UpdateUserSuspensionAsync(string userId, bool isSuspended);
        Task<List<UserProfile>> GetAllProfilesAsync();
        Task<List<UserProfile>> GetUsersForNotificationAsync();
        Task MarkNotificationSentAsync(List<string> userIds);
    }
}
