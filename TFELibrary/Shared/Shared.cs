using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace TFELibrary.Shared
{
    public enum RoleType
    {
        Student,
        Teacher,
        Admin
    }

    public class TfeProposalDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string TutorName { get; set; } = string.Empty;
        public List<string> Technologies { get; set; } = new();
        public string EstimatedDuration { get; set; } = string.Empty;
        public DateTime ExpirationDate { get; set; }
        public string IconName { get; set; } = "Psychology";
    }

    public class CandidateProfileDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Degree { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public List<string> AreasOfInterest { get; set; } = new();
        public string Biography { get; set; } = string.Empty;
        public List<CompetencyDto> Competencies { get; set; } = new();
    }

    public class ProfileDto
    {
        public RoleType Role { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string Bio { get; set; } = string.Empty;
        public List<string> Interests { get; set; } = new();
        public string AvatarUrl { get; set; } = string.Empty;

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
        public string Name { get; set; } = string.Empty;
    }

    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public AuthResultDto AuthData { get; set; } = new AuthResultDto();
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
    }

    public class RegisterRequestDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        public string Surname { get; set; } = string.Empty;
    }
    public class RegisterResponseDto
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public AuthResultDto AuthData { get; set; } = new AuthResultDto();
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
        public AuthResultDto AuthData { get; set; } = new AuthResultDto();
    }

    public class AuthResultDto
    {
        public bool IsSuccess { get; set; }

        [JsonIgnore]
        public string Token { get; set; } = string.Empty;

        [JsonIgnore]
        public string RefreshToken { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
    }
}
