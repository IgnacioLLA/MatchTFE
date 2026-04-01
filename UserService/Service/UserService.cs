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
            {
                return new ProfileUpdateResponse(false, "Los datos del perfil no pueden estar vacíos.");
            }

            var isSaved = await _userRepository.UpdateUserProfileAsync(userId, request.Profile);

            if (isSaved)
            {
                return new ProfileUpdateResponse(true, "Perfil actualizado correctamente.", request.Profile);
            }

            return new ProfileUpdateResponse(false, "No se pudo actualizar el perfil en la base de datos.");
        }

        private ProfileDto GetProfileDto(UserProfile profile)
        {
            var dto = new ProfileDto
            {
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Email = profile.Email,
                Bio = profile.Bio ?? string.Empty,
                Interests = profile.Interests?.Select(t => t.Name).ToList() ?? new List<string>(),
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

            return dto;
        }


    }
}
