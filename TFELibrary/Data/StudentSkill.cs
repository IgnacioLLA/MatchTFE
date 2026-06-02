using System.ComponentModel.DataAnnotations;

namespace TFELibrary.Data;

public class StudentSkill
{
    public string StudentProfileId { get; set; } = string.Empty;
    public UserProfile StudentProfile { get; set; } = null!;

    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;

    [Range(1, 5)]
    public int Level { get; set; }
}
