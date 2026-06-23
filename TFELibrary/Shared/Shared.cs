using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace TFELibrary.Shared;

public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
}

public enum RoleType
{
    Student,
    Teacher,
    Admin
}

public enum NotificationFrequency
{
    Disabled = 0,
    Weekly = 1,
    Biweekly = 2,
    Monthly = 3
}

public class TfeDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TutorName { get; set; } = string.Empty;
    public int InterestedAmount { get; set; }
    public List<TagDto> Topics { get; set; } = new();
    public List<SkillDto> RequiredSkills { get; set; } = new();
    public DateTime EstimatedDelivery { get; set; }
    public DateTime ExpirationDate { get; set; }
    public DateTime CreationDate { get; set; }
    public TfeStatus Status { get; set; }
}

public enum TfeStatus
{
    Open = 1,
    Completed = 2,
    Cancelled = 0
}

public enum ProposalStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
    Expired = 0,
    NotInterested = 4
}

public static class TfeDateRules
{
    public static DateOnly MinimumExpirationDate => DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    public static bool IsValidExpirationDate(DateTime expirationDate)
    {
        return DateOnly.FromDateTime(expirationDate) >= MinimumExpirationDate;
    }

    public static bool IsValidExpirationDate(DateOnly expirationDate)
    {
        return expirationDate >= MinimumExpirationDate;
    }

    public static string ToInputDateValue(DateOnly date)
    {
        return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}

public class CandidateProfileDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Degree { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public List<string> AreasOfInterest { get; set; } = new();
    public string Biography { get; set; } = string.Empty;
    public List<CompetencyDto> Competencies { get; set; } = new();
}

public class ProfileDto
{
    public string Id { get; set; } = string.Empty;
    public RoleType Role { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public string Bio { get; set; } = string.Empty;
    public bool IsSuspended { get; set; } = false;
    public List<string> Interests { get; set; } = new();

    // ----------------------------------
    // STUDENT
    // ----------------------------------
    public string AcademicYear { get; set; } = string.Empty;
    public List<SkillDto> Skills { get; set; } = new();

    // ----------------------------------
    // TEACHER
    // ----------------------------------
    public string Department { get; set; } = string.Empty;
    public string OfficeLocation { get; set; } = string.Empty;

    // ----------------------------------
    // NOTIFICATIONS
    // ----------------------------------
    public NotificationFrequency NotificationFrequency { get; set; } = NotificationFrequency.Disabled;
}

public class SkillDto
{
    public string Tag { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
}

public class CompetencyDto
{
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
    public int MaxScore { get; set; } = 5;
}

public class TagDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class LoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public record OperationResult(bool IsSuccess, string Message, string? ErrorCode = null);

public class LoginResponseDto
{
    public OperationResult Error { get; set; } = new OperationResult(false, string.Empty);
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class RegisterRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;
}

public class RegisterResponseDto
{
    public OperationResult Error { get; set; } = new OperationResult(false, string.Empty);
}

public class RefreshTokenRequestDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class RefreshTokenResponseDto
{
    public OperationResult Error { get; set; } = new OperationResult(false, string.Empty);
}
