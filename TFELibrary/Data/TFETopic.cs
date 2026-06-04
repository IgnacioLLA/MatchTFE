using System.ComponentModel.DataAnnotations;

namespace TFELibrary.Data;

public class TfeTopic
{
    [Required]
    public int TfeId { get; set; }
    [Required]
    public TFE Tfe { get; set; } = null!;
    [Required]
    public int TagId { get; set; }
    [Required]
    public Tag Tag { get; set; } = null!;
}
