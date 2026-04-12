using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TFELibrary.Shared
{

    public class TagCreationRequest
    {
        [MaxLength(50)]
        public TagDto Tag { get; set; }
    }

    public class TagCreationResponse
    {
        [MaxLength(50)]
        public TagDto Tag { get; set; }
        public int TagId { get; set; }
    }


    public class TfeCreationRequest
    {
        public TfeDto Tfe { get; set; }
    }

    public class TfeCreationResponse
    {
        public TfeDto Tfe { get; set; }
        public int TfeId { get; set; }
    }
    public class TfeUpdateRequest
    {
        [Required]
        public TfeDto Tfe { get; set; } = new TfeDto();
    }
    public class TfeRecommendedRequest
    {
        public int Count { get; set; } = 10;
    }
    public class TfeRecommendedResponse
    {
        public List<TfeDto> Tfes { get; set; } = new();
        public int TotalCount { get; set; }
    }

    // -- Proposals --
    public class TfeProposalRequest
    {
        [Required]
        public int TfeId { get; set; }
        [Required]
        public bool IsInterested { get; set; }
    }

    public class TfeProposalResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
