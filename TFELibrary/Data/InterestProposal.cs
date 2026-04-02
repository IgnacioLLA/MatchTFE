using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TFELibrary.Data
{
    public class InterestProposal
    {
        public string OriginUserId { get; set; } = string.Empty;
        public UserProfile OriginUser { get; set; } = null!;
        public string DestinationUserId { get; set; } = string.Empty;
        public UserProfile DestinationUser { get; set; } = null!;
        [Required]
        public ProposalStatus Status { get; set; } = ProposalStatus.Pending;
    }
}
