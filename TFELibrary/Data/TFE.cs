using System.ComponentModel.DataAnnotations;
using TFELibrary.Shared;

namespace TFELibrary.Data;

public class TFE
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string AuthorId { get; set; } = string.Empty;
    [Required]
    public UserProfile Author { get; set; } = null!;
    [MaxLength(4000)]
    public string Description { get; set; } = string.Empty;
    [Required]
    [MaxLength(400)]
    public string Title { get; set; } = string.Empty;
    public DateOnly EstimatedDelivery { get; set; }
    public DateOnly ExpirationDate { get; set; }
    public DateOnly CreationDate { get; set; }
    public List<Tag> Topics { get; set; } = new();
    public List<TfeRequiredSkill> RequiredSkills { get; set; } = new();
    public List<TFEProposal> Proposals { get; set; } = new();
    public TfeStatus Status { get; set; } = TfeStatus.Open;
}
