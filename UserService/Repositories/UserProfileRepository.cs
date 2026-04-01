using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TFELibrary.Data;
using TFELibrary.Shared;
using UserService.Data;

namespace UserService.Repositories
{
    public class UserProfileRepository : IUserProfileRepository
    {
        private readonly UserDbContext _context;

        public UserProfileRepository(UserDbContext context)
        {
            _context = context;
        }

        public async Task<UserProfile?> GetByUserIdAsync(string userId)
        {
            return await _context.UserProfile
                .Include(p => p.Interests)
                .Include(p => (p as UserProfile)!.StudentSkills)
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task<UserProfile> CreateProfileAsync(UserProfile profile)
        {
            await _context.UserProfile.AddAsync(profile);

            await _context.SaveChangesAsync();

            return profile;
        }

        public async Task<bool> UpdateUserProfileAsync(string userId, ProfileDto profileDto)
        {
            var userEntity = await _context.UserProfile.FindAsync(userId);

            if (userEntity == null)
                return false;

            // 1. Actualizamos datos comunes
            userEntity.FirstName = profileDto.FirstName;
            userEntity.LastName = profileDto.LastName;
            userEntity.Bio = profileDto.Bio;

            // 2. Lógica específica según el rol
            if (profileDto.Role == RoleType.Student)
            {
                userEntity.AcademicYear = profileDto.AcademicYear;
            }
            else if (profileDto.Role == RoleType.Teacher)
            {
                userEntity.Department = profileDto.Department;
                userEntity.OfficeLocation = profileDto.OfficeLocation;
            }

            try
            {
                _context.UserProfile.Update(userEntity);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
