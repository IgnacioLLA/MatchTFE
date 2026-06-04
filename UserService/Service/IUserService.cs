using Microsoft.AspNetCore.Mvc;
using TFELibrary.Data;
using TFELibrary.Shared;

namespace UserService.Service
{
    public interface IUserService
    {
        Task<ProfileResponse> GetProfileByUserIdAsync(string userId);
        Task<ProfileCreationResponse> CreateProfileAsync(ProfileCreationRequest request);
        Task<ProfileUpdateResponse> UpdateProfileAsync(string userId, ProfileUpdateRequest request);
        Task<ProfileByTfeInterestResponse> GetProfileByTfeInterestAsync(ProfileByTfeInterestRequest request);
        Task<RoleUpdateResponse> UpdateUserRoleAsync(string userId, RoleType newRole);
        Task<SuspensionUpdateResponse> UpdateUserSuspensionAsync(string userId, bool isSuspended);
        Task<GetAllProfilesResponse> GetAllProfilesAsync(GetAllProfilesRequest request);
    }
}
