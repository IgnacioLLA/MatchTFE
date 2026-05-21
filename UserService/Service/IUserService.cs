using Microsoft.AspNetCore.Mvc;
using TFELibrary.Data;
using TFELibrary.Shared;

namespace UserService.Service
{
    public interface IUserService
    {
        Task<ProfileResponse?> GetProfileByUserIdAsync(string userId);
        Task<bool> CreateProfileAsync(ProfileCreationRequest request);
        Task<ProfileUpdateResponse> UpdateProfileAsync(string userId, ProfileUpdateRequest request);
        Task<ProfileByTfeInterestResponse> GetProfileByTfeInterest(ProfileByTfeInterestRequest request);
        Task<bool> UpdateUserRoleAsync(string userId, RoleType newRole);
        Task<GetAllProfilesResponse> GetAllProfilesAsync(GetAllProfilesRequest request);
    }
}
