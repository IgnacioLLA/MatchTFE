using System.ComponentModel.DataAnnotations;

namespace TFELibrary.Data;

public class TfeRequiredSkill
{
    public int TfeId { get; set; }
    [Required]
    public TFE Tfe { get; set; } = null!;
    public int TagId { get; set; }
    [Required]
    public Tag Tag { get; set; } = null!;

    [Range(1, 5)]
    public int Level { get; set; }
}
