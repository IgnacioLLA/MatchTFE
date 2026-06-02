using System.ComponentModel.DataAnnotations;

namespace TFELibrary.Data;

public class Tag
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
}
