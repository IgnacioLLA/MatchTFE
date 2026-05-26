using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TFELibrary.Data;
using TFELibrary.Shared;
using UserService.Repositories;

namespace UserService.Service
{
    public class UserService : IUserService
    {
        private readonly IUserProfileRepository _userRepository;

        public UserService(IUserProfileRepository userRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task<ProfileResponse?> GetProfileByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            var entity = await _userRepository.GetByUserIdAsync(userId);

            if (entity == null)
                return null;

            var dto = GetProfileDto(entity);

            return new ProfileResponse(dto);
        }
        public async Task<ProfileCreationResponse> CreateProfileAsync(ProfileCreationRequest request)
        {
            if (request == null)
                return new ProfileCreationResponse(false, "Request payload cannot be null.");

            if (request.Profile == null)
                return new ProfileCreationResponse(false, "Profile data is required.");

            if (string.IsNullOrWhiteSpace(request.UserId))
                return new ProfileCreationResponse(false, "User ID is required.");

            var profDto = request.Profile;
            var newProfile = new UserProfile
            {
                UserId = request.UserId,
                FirstName = profDto.FirstName ?? string.Empty,
                LastName = profDto.LastName ?? string.Empty,
                Email = profDto.Email ?? string.Empty
            };

            await _userRepository.CreateProfileAsync(newProfile);

            return new ProfileCreationResponse(true, "Profile created successfully.", newProfile.UserId);
        }

        public async Task<ProfileUpdateResponse> UpdateProfileAsync(string userId, ProfileUpdateRequest request)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new ProfileUpdateResponse(false, "User ID is required.");

            if (request?.Profile == null)
                return new ProfileUpdateResponse(false, "Profile data cannot be empty.");

            var entity = new UserProfile
            {
                UserId = userId,
                FirstName = request.Profile.FirstName,
                LastName = request.Profile.LastName,
                Bio = request.Profile.Bio,
                Role = request.Profile.Role,
                AcademicYear = request.Profile.AcademicYear,
                Department = request.Profile.Department,
                OfficeLocation = request.Profile.OfficeLocation
            };

            var isSaved = await _userRepository.UpdateUserProfileAsync(entity, request.Profile.Interests, request.Profile.Skills);

            if (isSaved)
                return new ProfileUpdateResponse(true, "Profile updated successfully.", request.Profile);

            return new ProfileUpdateResponse(false, "Could not update profile. Ensure the user exists.");
        }

        public async Task<ProfileByTfeInterestResponse> GetProfileByTfeInterestAsync(ProfileByTfeInterestRequest request)
        {
            if (request == null || int.IsNegative(request.TfeId))
                return new ProfileByTfeInterestResponse(new List<ProfileDto>());

            var entities = await _userRepository.GetInterestedUsersByTfeIdInUserServiceAsync(request.TfeId);

            if (entities == null || !entities.Any())
                return new ProfileByTfeInterestResponse(new List<ProfileDto>());

            var dtos = entities.Select(GetProfileDto).ToList();
            return new ProfileByTfeInterestResponse(dtos);
        }
        public async Task<RoleUpdateResponse> UpdateUserRoleAsync(string userId, RoleType newRole)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new RoleUpdateResponse(false, "User ID is required.");

            var isUpdated = await _userRepository.UpdateUserRoleAsync(userId, newRole);

            if (isUpdated)
                return new RoleUpdateResponse(true, "User role updated successfully.");

            return new RoleUpdateResponse(false, "Could not update role. Ensure the user exists.");
        }

        public async Task<GetAllProfilesResponse> GetAllProfilesAsync(GetAllProfilesRequest request)
        {
            var entities = await _userRepository.GetAllProfilesAsync();

            var dtos = entities?.Select(GetProfileDto).ToList() ?? new List<ProfileDto>();

            return new GetAllProfilesResponse(dtos);
        }

        // --------------------------------------------------

        private ProfileDto GetProfileDto(UserProfile profile)
        {
            return new ProfileDto
            {
                Id = profile.UserId,
                FirstName = profile.FirstName ?? string.Empty,
                LastName = profile.LastName ?? string.Empty,
                Email = profile.Email ?? string.Empty,
                Bio = profile.Bio ?? string.Empty,
                Interests = profile.UserInterests?.Select(ui => ui.Tag.Name).ToList() ?? new List<string>(),
                Role = profile.Role,
                AcademicYear = profile.AcademicYear ?? string.Empty,
                Skills = profile.StudentSkills?.Select(s => new SkillDto
                {
                    Tag = s.Tag.Name,
                    Level = s.Level
                }).ToList() ?? new List<SkillDto>(),
                Department = profile.Department ?? string.Empty,
                OfficeLocation = profile.OfficeLocation ?? string.Empty
            };
        }
    }
}