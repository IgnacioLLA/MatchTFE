using Microsoft.EntityFrameworkCore;
using TFELibrary.Data;
using TFELibrary.Shared;
using UserService.Data;

namespace UserService.Repositories;

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
            .Include(u => u.UserInterests)
                .ThenInclude(ui => ui.Tag)
            .Include(u => u.StudentSkills)
                .ThenInclude(ss => ss.Tag)
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<List<UserProfile>> GetAllProfilesAsync()
    {
        return await _context.UserProfile
            .Include(u => u.UserInterests)
                .ThenInclude(ui => ui.Tag)
            .Include(u => u.StudentSkills)
                .ThenInclude(ss => ss.Tag)
            .ToListAsync();
    }

    public async Task<UserProfile> CreateProfileAsync(UserProfile profile)
    {
        await _context.UserProfile.AddAsync(profile);

        await _context.SaveChangesAsync();

        return profile;
    }

    public async Task<bool> UpdateUserProfileAsync(UserProfile entity, List<string> interests, List<SkillDto> skills)
    {
        var existing = await _context.UserProfile
            .FirstOrDefaultAsync(u => u.UserId == entity.UserId);

        if (existing == null) return false;

        existing.FirstName = entity.FirstName;
        existing.LastName = entity.LastName;
        existing.Bio = entity.Bio;
        existing.AcademicYear = entity.AcademicYear;
        existing.Department = entity.Department;
        existing.OfficeLocation = entity.OfficeLocation;
        existing.Role = entity.Role;
        existing.NotificationFrequency = entity.NotificationFrequency;

        await UpdateInterestsAsync(existing.UserId, interests);
        await UpdateSkillsAsync(existing.UserId, skills);

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task UpdateInterestsAsync(string userId, List<string> interestNames)
    {
        var existingInterests = await _context.UserInterest
            .Where(ui => ui.UserProfileId == userId)
            .ToListAsync();
        _context.UserInterest.RemoveRange(existingInterests);

        var tagIds = await GetTagIdsByNamesAsync(interestNames);
        await _context.UserInterest.AddRangeAsync(tagIds.Select(tagId => new UserInterest
        {
            UserProfileId = userId,
            TagId = tagId
        }));
    }

    private async Task UpdateSkillsAsync(string userId, List<SkillDto> skills)
    {
        var existingSkills = await _context.StudentSkill
            .Where(ss => ss.StudentProfileId == userId)
            .ToListAsync();
        _context.StudentSkill.RemoveRange(existingSkills);

        var tagMap = await GetTagMapByNamesAsync(skills.Select(s => s.Tag).ToList());
        await _context.StudentSkill.AddRangeAsync(
            skills
                .Where(s => tagMap.ContainsKey(s.Tag))
                .Select(s => new StudentSkill
                {
                    StudentProfileId = userId,
                    TagId = tagMap[s.Tag],
                    Level = s.Level
                })
        );
    }

    private async Task<List<int>> GetTagIdsByNamesAsync(List<string> tagNames)
    {
        if (tagNames == null || !tagNames.Any()) return new List<int>();
        return await _context.Tag
            .Where(t => tagNames.Contains(t.Name))
            .Select(t => t.Id)
            .ToListAsync();
    }

    private async Task<Dictionary<string, int>> GetTagMapByNamesAsync(List<string> tagNames)
    {
        if (tagNames == null || !tagNames.Any()) return new Dictionary<string, int>();
        return await _context.Tag
            .Where(t => tagNames.Contains(t.Name))
            .ToDictionaryAsync(t => t.Name, t => t.Id);
    }

    public async Task<List<UserProfile>> GetInterestedUsersByTfeIdInUserServiceAsync(int tfeId)
    {
        return await _context.UserProfile
            .Include(u => u.UserInterests)
                .ThenInclude(ui => ui.Tag)
            .Include(u => u.StudentSkills)
                .ThenInclude(ss => ss.Tag)
            .Include(u => u.TfeProposals)
            .Where(u => u.TfeProposals.Any(tp => tp.TfeId == tfeId && tp.Status != ProposalStatus.NotInterested))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<UserProfile>> GetUsersForNotificationAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.UserProfile
            .Include(u => u.UserInterests).ThenInclude(ui => ui.Tag)
            .Where(u =>
                u.NotificationFrequency != NotificationFrequency.Disabled &&
                !u.IsSuspended &&
                u.Role != RoleType.Admin &&
                (u.LastNotificationSentAt == null ||
                 (u.NotificationFrequency == NotificationFrequency.Weekly && u.LastNotificationSentAt <= now.AddDays(-7)) ||
                 (u.NotificationFrequency == NotificationFrequency.Biweekly && u.LastNotificationSentAt <= now.AddDays(-14)) ||
                 (u.NotificationFrequency == NotificationFrequency.Monthly && u.LastNotificationSentAt <= now.AddDays(-30))))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task MarkNotificationSentAsync(List<string> userIds)
    {
        var profiles = await _context.UserProfile
            .Where(u => userIds.Contains(u.UserId))
            .ToListAsync();

        foreach (var profile in profiles)
            profile.LastNotificationSentAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<bool> UpdateUserRoleAsync(string userId, RoleType newRole)
    {
        var profile = await _context.UserProfile.FirstOrDefaultAsync(u => u.UserId == userId);
        if (profile == null) return false;

        profile.Role = newRole;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateUserSuspensionAsync(string userId, bool isSuspended)
    {
        var profile = await _context.UserProfile.FirstOrDefaultAsync(u => u.UserId == userId);
        if (profile == null) return false;

        profile.IsSuspended = isSuspended;
        await _context.SaveChangesAsync();
        return true;
    }
}
