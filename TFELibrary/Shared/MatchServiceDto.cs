using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TFELibrary.Shared
{

    public class TagCreationRequest
    {
        public TagDto Tag { get; set; }
    }

    public class TagCreationResponse
    {
        public TagDto Tag { get; set; }
        public int TagId { get; set; }
    }

    public class TagUpdateRequest
    {
        [Required(ErrorMessage = "Tag's name is mandatory.")]
        [MaxLength(50, ErrorMessage = "Message too long, max 50 characters.")]
        public string Name { get; set; } = string.Empty;
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
    public class TfeProposalCreationRequest
    {
        [Required]
        public int TfeId { get; set; }
        [Required]
        public bool IsInterested { get; set; }
    }

    public class TfeProposalCreationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class TfeProposalUpdateRequest
    {
        [Required]
        public string UserId { get; set; }
        [Required]
        public int TfeId { get; set; }
        [Required]
        public bool IsInterested { get; set; }
    }

    public class TfeProposalUpdateResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class TfeCandidateDecisionRequest
    {
        [Required]
        public int TfeId { get; set; }

        [Required]
        public string CandidateId { get; set; } = string.Empty;

        [Required]
        public ProposalStatus Status { get; set; }
    }

    public class TfeCandidateDecisionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ProposalStatus Status { get; set; }
    }

    public class TfeCandidateDto
    {
        public ProfileDto Profile { get; set; } = new ProfileDto();
        public ProposalStatus Status { get; set; } = ProposalStatus.Pending;

        public bool CanDecide => Status == ProposalStatus.Pending;
    }

    // -- Accepted Matches --
    public class AcceptedMatchDto
    {
        public string MatchedUserId { get; set; } = string.Empty;
        public string MatchedUserFullName { get; set; } = string.Empty;
        public string MatchedUserEmail { get; set; } = string.Empty;
        public RoleType MatchedUserRole { get; set; }
        public string MatchedUserAvatarUrl { get; set; } = string.Empty;
        public string? MatchedUserAcademicYear { get; set; }
        public string? MatchedUserDepartment { get; set; }
        public int TfeId { get; set; }
        public string TfeTitle { get; set; } = string.Empty;
        public string TfeTutorName { get; set; } = string.Empty;
        public DateTime MatchDate { get; set; }
        public ProposalStatus Status { get; set; } = ProposalStatus.Accepted;
    }

    public class GetAcceptedMatchesResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalMatches { get; set; }
        public List<AcceptedMatchDto> Matches { get; set; } = new();
    }
}
