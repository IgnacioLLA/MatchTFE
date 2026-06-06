using System.ComponentModel.DataAnnotations;

namespace TFELibrary.Shared;

public class TagCreationRequest
{
    public TagDto Tag { get; set; } = new TagDto();
}

public class TagCreationResponse
{
    public OperationResult Error { get; set; } = new OperationResult(false, string.Empty);
    public TagDto Tag { get; set; } = new TagDto();
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
    public TfeDto Tfe { get; set; } = new TfeDto();
}

public class TfeCreationResponse
{
    public OperationResult Error { get; set; } = new OperationResult(false, string.Empty);
    public TfeDto Tfe { get; set; } = new TfeDto();
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
    public OperationResult Error { get; set; } = new OperationResult(false, string.Empty);
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
    public OperationResult Error { get; set; } = new OperationResult(false, string.Empty);
}

public class TfeProposalUpdateRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public int TfeId { get; set; }

    [Required]
    public bool IsInterested { get; set; }
}

public class TfeProposalUpdateResponse
{
    public OperationResult Error { get; set; } = new OperationResult(false, string.Empty);
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
    public OperationResult Error { get; set; } = new OperationResult(false, string.Empty);
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
    public OperationResult Error { get; set; } = new OperationResult(false, string.Empty);
    public int TotalMatches { get; set; }
    public List<AcceptedMatchDto> Matches { get; set; } = new();
}

public class TfeStatusUpdateRequest
{
    [Required]
    public TfeStatus Status { get; set; }
}

public class TfeStatusUpdateResponse
{
    public OperationResult Error { get; set; } = new OperationResult(false, string.Empty);
    public TfeDto Tfe { get; set; } = new TfeDto();
}
