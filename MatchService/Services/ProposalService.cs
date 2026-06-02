using MatchService.Repositories;
using Microsoft.Extensions.Logging;
using TFELibrary.Data;
using TFELibrary.Shared;

namespace MatchService.Services
{
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
                throw new ArgumentException("User ID cannot be empty.");

            if (request == null || request.TfeId <= 0)
                return new TfeProposalCreationResponse { Error = new ErrorRecord(false, "Invalid proposal request.") };

            var tfe = await _tfeRepository.GetByIdAsync(request.TfeId);
            if (tfe == null)
                return new TfeProposalCreationResponse { Error = new ErrorRecord(false, "TFE not found.") };

            if (!TfeDateRules.IsValidExpirationDate(tfe.ExpirationDate))
                return new TfeProposalCreationResponse { Error = new ErrorRecord(false, "This TFE has expired and cannot receive new proposals.") };

            if (await _proposalRepository.TfeProposalExistsAsync(userId, request.TfeId))
                return new TfeProposalCreationResponse { Error = new ErrorRecord(false, "TFE already exists.", "DuplicateProposal") };

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
                return new TfeProposalCreationResponse { Error = new ErrorRecord(false, "Could not create the proposal due to a database error.", "DatabaseError") };
            }

            return new TfeProposalCreationResponse { Error = new ErrorRecord(true, "TFE proposal created successfully.") };
        }

        public async Task<TfeProposalUpdateResponse> UpdateTfeProposalAsync(TfeProposalUpdateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                return new TfeProposalUpdateResponse { Error = new ErrorRecord(false, "User ID cannot be empty.") };

            if (request.TfeId <= 0)
                return new TfeProposalUpdateResponse { Error = new ErrorRecord(false, "Invalid proposal request.") };

            var tfe = await _tfeRepository.GetByIdAsync(request.TfeId);
            if (tfe == null)
                return new TfeProposalUpdateResponse { Error = new ErrorRecord(false, "TFE not found.") };

            if (!TfeDateRules.IsValidExpirationDate(tfe.ExpirationDate))
                return new TfeProposalUpdateResponse { Error = new ErrorRecord(false, "This TFE has expired and can no longer be updated.") };

            var existingTfe = await _proposalRepository.GetTfeProposalByUserIdAsync(request.UserId, request.TfeId);
            if (existingTfe == null)
                return new TfeProposalUpdateResponse { Error = new ErrorRecord(false, "No previous proposal exists.") };

            if (!existingTfe.Status.Equals(ProposalStatus.Pending))
                return new TfeProposalUpdateResponse { Error = new ErrorRecord(false, "Invalid proposal status.") };

            if (request.IsInterested)
                existingTfe.Status = ProposalStatus.Accepted;
            else
                existingTfe.Status = ProposalStatus.Rejected;

            try
            {
                await _proposalRepository.UpdateTfeProposalAsync(existingTfe);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error while updating proposal for user {UserId} on TFE {TfeId}.", request.UserId, request.TfeId);
                return new TfeProposalUpdateResponse { Error = new ErrorRecord(false, "Could not update the proposal due to a database error.", "DatabaseError") };
            }

            return new TfeProposalUpdateResponse { Error = new ErrorRecord(true, "TFE proposal updated successfully.") };
        }

        public async Task<GetAcceptedMatchesResponse> GetAcceptedMatchesForUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new GetAcceptedMatchesResponse { Error = new ErrorRecord(false, "Usuario no especificado.") };

            try
            {
                var matches = await _proposalRepository.GetAcceptedMatchesForUserAsync(userId);

                return new GetAcceptedMatchesResponse
                {
                    Error = new ErrorRecord(true, $"Se encontraron {matches.Count} matches."),
                    Matches = matches,
                    TotalMatches = matches.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error while retrieving accepted matches for user {UserId}.", userId);
                return new GetAcceptedMatchesResponse { Error = new ErrorRecord(false, "Error al obtener los matches.", "DatabaseError") };
            }
        }

        public async Task<TfeCandidateDecisionResponse> DecideTfeCandidateAsync(string authorId, TfeCandidateDecisionRequest request)
        {
            if (string.IsNullOrWhiteSpace(authorId))
                throw new ArgumentException("User ID cannot be empty.");

            if (request == null || string.IsNullOrWhiteSpace(request.CandidateId) || request.TfeId <= 0)
                throw new ArgumentException("Invalid decision request.");

            if (request.Status is not (ProposalStatus.Accepted or ProposalStatus.Rejected))
                throw new ArgumentException("Only accepted or rejected statuses are allowed.");

            var tfe = await _tfeRepository.GetByIdAsync(request.TfeId);
            if (tfe == null)
                throw new KeyNotFoundException("TFE not found.");

            if (!TfeDateRules.IsValidExpirationDate(tfe.ExpirationDate))
                throw new InvalidOperationException("This TFE has expired and can no longer be decided.");

            if (!string.Equals(tfe.AuthorId, authorId, StringComparison.Ordinal))
                throw new UnauthorizedAccessException("You do not have permission to decide this proposal.");

            var proposal = await _proposalRepository.GetTfeProposalByUserIdAsync(request.CandidateId, request.TfeId);
            if (proposal == null)
                throw new KeyNotFoundException("Candidate proposal not found.");

            if (proposal.Status != ProposalStatus.Pending)
                throw new InvalidOperationException("This proposal has already been resolved.");

            proposal.Status = request.Status;
            await _proposalRepository.UpdateTfeProposalAsync(proposal);

            var message = request.Status == ProposalStatus.Accepted
                ? "Candidate accepted successfully."
                : "Candidate rejected successfully.";

            return new TfeCandidateDecisionResponse
            {
                Error = new ErrorRecord(true, message),
                Status = proposal.Status
            };
        }
    }
}
