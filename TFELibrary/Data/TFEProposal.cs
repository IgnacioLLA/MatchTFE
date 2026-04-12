using System.ComponentModel.DataAnnotations;
using TFELibrary.Shared;

namespace TFELibrary.Data
{
    public class TFEProposal
    {
        public string OriginUserId { get; set; } = string.Empty;
        public UserProfile OriginUser { get; set; } = null!;
        public int TfeId { get; set; }
        public TFE Tfe { get; set; } = null!;
        [Required]
        public ProposalStatus Status { get; set; } = ProposalStatus.Pending;
        public DateOnly CreationDate { get; set; }
    }
}