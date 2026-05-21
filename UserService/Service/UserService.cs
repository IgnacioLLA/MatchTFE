using TFELibrary.Data;
using TFELibrary.Shared;
using UserService.Repositories;

namespace UserService.Service
{
    public class UserService : IUserService
    {
        private readonly IUserProfileRepository _userRepository;
        public UserService(IUserProfileRepository userProfileRepository, IConfiguration configuration)
        {
            _userRepository = userProfileRepository;
        }

        public async Task<ProfileResponse?> GetProfileByUserIdAsync(string userId)
        {
            var entity = await _userRepository.GetByUserIdAsync(userId);

            var dto = GetProfileDto(entity);

            return new ProfileResponse(dto);
        }

        public async Task<bool> CreateProfileAsync(ProfileCreationRequest request)
        {
            var profDto = request.Profile;
            var newProfile = new UserProfile
            {
                UserId = request.UserId,

                FirstName = profDto.FirstName ?? "",
                LastName = profDto.LastName ?? "",
                Email = profDto.Email
            };

            await _userRepository.CreateProfileAsync(newProfile);

            return true;
        }

        public async Task<ProfileUpdateResponse> UpdateProfileAsync(string userId, ProfileUpdateRequest request)
        {
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

            return new ProfileUpdateResponse(false, "Could not update profile.");

        }

        public async Task<ProfileByTfeInterestResponse> GetProfileByTfeInterest(ProfileByTfeInterestRequest request)
        {
            return new ProfileByTfeInterestResponse((await _userRepository.GetInterestedUsersByTfeIdInUserServiceAsync(request.TfeId)).ConvertAll((p) => GetProfileDto(p)));
        }

        // --------------------------------------------------

        private ProfileDto GetProfileDto(UserProfile profile)
        {
            return new ProfileDto
            {
                Id = profile.UserId,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Email = profile.Email,
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

        public async Task<bool> UpdateUserRoleAsync(string userId, RoleType newRole)
        {
            return await _userRepository.UpdateUserRoleAsync(userId, newRole);
        }

        public async Task<GetAllProfilesResponse> GetAllProfilesAsync(GetAllProfilesRequest request)
        {
            var entities = await _userRepository.GetAllProfilesAsync();
            var dtos = entities.Select(e => GetProfileDto(e)).ToList();

            return new GetAllProfilesResponse(dtos);
        }
    }
}
