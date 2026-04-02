using System.ComponentModel.DataAnnotations;

namespace TFELibrary.Data
{
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
        public List<Tag> Topics { get; set; } = new List<Tag>();
        public TFEStatus Status { get; set; } = TFEStatus.Open;
    }

    public enum TFEStatus
    {
        Open,
        Completed,
        Cancelled
    }
}
