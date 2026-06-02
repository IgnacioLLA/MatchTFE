using MatchService.Repositories;
using Microsoft.Extensions.Logging;
using TFELibrary.Data;
using TFELibrary.Shared;

namespace MatchService.Services;

public class ProposalService : IProposalService
{
    private readonly IProposalRepository _proposalRepository;
    private readonly ITfeRepository _tfeRepository;
    private readonly ILogger<ProposalService> _logger;

    public ProposalService(IProposalRepository proposalRepository, ITfeRepository tfeRepository, ILogger<ProposalService> logger)
    {
        _proposalRepository = proposalRepository;
        _tfeRepository = tfeRepository;
        _logger = logger;
    }

    public async Task<TfeProposalCreationResponse> CreateTfeProposalAsync(string userId, TfeProposalCreationRequest request)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new TfeProposalCreationResponse { Error = new OperationResult(false, "User ID cannot be empty.") };

        if (request == null || request.TfeId <= 0)
            return new TfeProposalCreationResponse { Error = new OperationResult(false, "Invalid proposal request.") };

        var tfe = await _tfeRepository.GetByIdAsync(request.TfeId);
        if (tfe == null)
            return new TfeProposalCreationResponse { Error = new OperationResult(false, "TFE not found.", "TfeNotFound") };

        if (!TfeDateRules.IsValidExpirationDate(tfe.ExpirationDate))
            return new TfeProposalCreationResponse { Error = new OperationResult(false, "This TFE has expired and cannot receive new proposals.", "TfeExpired") };

        if (await _proposalRepository.TfeProposalExistsAsync(userId, request.TfeId))
            return new TfeProposalCreationResponse { Error = new OperationResult(false, "TFE already exists.", "DuplicateProposal") };

        var proposal = new TFEProposal
        {
            OriginUserId = userId,
            TfeId = request.TfeId,
            Status = request.IsInterested
                ? ProposalStatus.Pending
                : ProposalStatus.Rejected,
            CreationDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        try
        {
            await _proposalRepository.CreateTfeProposalAsync(proposal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error while creating proposal for user {UserId} on TFE {TfeId}.", userId, request.TfeId);
            return new TfeProposalCreationResponse { Error = new OperationResult(false, "Could not create the proposal due to a database error.", "DatabaseError") };
        }

        return new TfeProposalCreationResponse { Error = new OperationResult(true, "TFE proposal created successfully.") };
    }

    public async Task<TfeProposalUpdateResponse> UpdateTfeProposalAsync(TfeProposalUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
            return new TfeProposalUpdateResponse { Error = new OperationResult(false, "User ID cannot be empty.") };

        if (request.TfeId <= 0)
            return new TfeProposalUpdateResponse { Error = new OperationResult(false, "Invalid proposal request.") };

        var tfe = await _tfeRepository.GetByIdAsync(request.TfeId);
        if (tfe == null)
            return new TfeProposalUpdateResponse { Error = new OperationResult(false, "TFE not found.", "TfeNotFound") };

        if (!TfeDateRules.IsValidExpirationDate(tfe.ExpirationDate))
            return new TfeProposalUpdateResponse { Error = new OperationResult(false, "This TFE has expired and can no longer be updated.", "TfeExpired") };

        var existingTfe = await _proposalRepository.GetTfeProposalByUserIdAsync(request.UserId, request.TfeId);
        if (existingTfe == null)
            return new TfeProposalUpdateResponse { Error = new OperationResult(false, "No previous proposal exists.", "ProposalNotFound") };

        if (!existingTfe.Status.Equals(ProposalStatus.Pending))
            return new TfeProposalUpdateResponse { Error = new OperationResult(false, "Invalid proposal status.", "InvalidProposalStatus") };

        existingTfe.Status = request.IsInterested ? ProposalStatus.Accepted : ProposalStatus.Rejected;

        try
        {
            await _proposalRepository.UpdateTfeProposalAsync(existingTfe);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error while updating proposal for user {UserId} on TFE {TfeId}.", request.UserId, request.TfeId);
            return new TfeProposalUpdateResponse { Error = new OperationResult(false, "Could not update the proposal due to a database error.", "DatabaseError") };
        }

        return new TfeProposalUpdateResponse { Error = new OperationResult(true, "TFE proposal updated successfully.") };
    }

    public async Task<GetAcceptedMatchesResponse> GetAcceptedMatchesForUserAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new GetAcceptedMatchesResponse { Error = new OperationResult(false, "Usuario no especificado.") };

        try
        {
            var matches = await _proposalRepository.GetAcceptedMatchesForUserAsync(userId);

            return new GetAcceptedMatchesResponse
            {
                Error = new OperationResult(true, $"Se encontraron {matches.Count} matches."),
                Matches = matches,
                TotalMatches = matches.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error while retrieving accepted matches for user {UserId}.", userId);
            return new GetAcceptedMatchesResponse { Error = new OperationResult(false, "Error al obtener los matches.", "DatabaseError") };
        }
    }

    public async Task<TfeCandidateDecisionResponse> DecideTfeCandidateAsync(string authorId, TfeCandidateDecisionRequest request)
    {
        if (string.IsNullOrWhiteSpace(authorId))
            return new TfeCandidateDecisionResponse { Error = new OperationResult(false, "Author ID cannot be empty.") };

        if (request == null || string.IsNullOrWhiteSpace(request.CandidateId) || request.TfeId <= 0)
            return new TfeCandidateDecisionResponse { Error = new OperationResult(false, "Invalid decision request.") };

        if (request.Status is not (ProposalStatus.Accepted or ProposalStatus.Rejected))
            return new TfeCandidateDecisionResponse { Error = new OperationResult(false, "Only accepted or rejected statuses are allowed.", "InvalidStatus") };

        var tfe = await _tfeRepository.GetByIdAsync(request.TfeId);
        if (tfe == null)
            return new TfeCandidateDecisionResponse { Error = new OperationResult(false, "TFE not found.", "TfeNotFound") };

        if (!TfeDateRules.IsValidExpirationDate(tfe.ExpirationDate))
            return new TfeCandidateDecisionResponse { Error = new OperationResult(false, "This TFE has expired and can no longer be decided.", "TfeExpired") };

        if (!string.Equals(tfe.AuthorId, authorId, StringComparison.Ordinal))
            return new TfeCandidateDecisionResponse { Error = new OperationResult(false, "You do not have permission to decide this proposal.", "Unauthorized") };

        var proposal = await _proposalRepository.GetTfeProposalByUserIdAsync(request.CandidateId, request.TfeId);
        if (proposal == null)
            return new TfeCandidateDecisionResponse { Error = new OperationResult(false, "Candidate proposal not found.", "ProposalNotFound") };

        if (proposal.Status != ProposalStatus.Pending)
            return new TfeCandidateDecisionResponse { Error = new OperationResult(false, "This proposal has already been resolved.", "ProposalAlreadyResolved") };

        proposal.Status = request.Status;

        try
        {
            await _proposalRepository.UpdateTfeProposalAsync(proposal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error while deciding proposal for candidate {CandidateId} on TFE {TfeId}.", request.CandidateId, request.TfeId);
            return new TfeCandidateDecisionResponse { Error = new OperationResult(false, "Could not save the decision due to a database error.", "DatabaseError") };
        }

        var message = request.Status == ProposalStatus.Accepted
            ? "Candidate accepted successfully."
            : "Candidate rejected successfully.";

        return new TfeCandidateDecisionResponse
        {
            Error = new OperationResult(true, message),
            Status = proposal.Status
        };
    }
}
