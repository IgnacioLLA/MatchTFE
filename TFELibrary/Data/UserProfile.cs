using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TFELibrary.Shared;

namespace TFELibrary.Data;

public class UserProfile
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Bio { get; set; }

    public RoleType Role { get; set; } = RoleType.Student;
    public bool IsSuspended { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<UserInterest> UserInterests { get; set; } = new List<UserInterest>();
    public ICollection<TFEProposal> TfeProposals { get; set; } = new List<TFEProposal>();

    // --- STUDENT ---
    public string? AcademicYear { get; set; }
    public ICollection<StudentSkill> StudentSkills { get; set; } = new List<StudentSkill>();

    // --- TEACHER ---
    public string? Department { get; set; }
    public string? OfficeLocation { get; set; }
}
