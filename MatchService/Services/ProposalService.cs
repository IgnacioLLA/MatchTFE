using MatchService.Repositories;
using TFELibrary.Data;
using TFELibrary.Shared;

namespace MatchService.Services
{
    public class ProposalService : IProposalService
    {
        private readonly IProposalRepository _proposalRepository;
        private readonly ITfeRepository _tfeRepository;

        public ProposalService(IProposalRepository proposalRepository, ITfeRepository tfeRepository)
        {
            _proposalRepository = proposalRepository;
            _tfeRepository = tfeRepository;
        }

        public async Task<TfeProposalCreationResponse> CreateTfeProposalAsync(string userId, TfeProposalCreationRequest request)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty.");

            if (await _proposalRepository.TfeProposalExistsAsync(userId, request.TfeId))
            {
                return new TfeProposalCreationResponse { Success = false, Message = "TFE already exists." };
            }

            var proposal = new TFEProposal
            {
                OriginUserId = userId,
                TfeId = request.TfeId,
                Status = request.IsInterested
                ? ProposalStatus.Pending
                : ProposalStatus.Rejected,
                CreationDate = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            await _proposalRepository.CreateTfeProposalAsync(proposal);

            return new TfeProposalCreationResponse { Success = true, Message = "TFE proposal created successfully." };
        }

        public async Task<TfeProposalUpdateResponse> UpdateTfeProposalAsync(TfeProposalUpdateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                return new TfeProposalUpdateResponse { Success = false, Message = "User ID cannot be empty." };

            var existingTfe = await _proposalRepository.GetTfeProposalByUserIdAsync(request.UserId, request.TfeId);
            if (existingTfe == null)
                return new TfeProposalUpdateResponse { Success = false, Message = "No previous proposal exists." };

            if (!existingTfe.Status.Equals(ProposalStatus.Pending))
                return new TfeProposalUpdateResponse { Success = false, Message = "Invalid proposal status." };

            if (request.IsInterested)
                existingTfe.Status = ProposalStatus.Accepted;
            else
                existingTfe.Status = ProposalStatus.Rejected;

            await _proposalRepository.UpdateTfeProposalAsync(existingTfe);

            return new TfeProposalUpdateResponse { Success = true, Message = "TFE proposal updated successfully." };
        }

        public async Task<GetAcceptedMatchesResponse> GetAcceptedMatchesForUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new GetAcceptedMatchesResponse
                {
                    Success = false,
                    Message = "Usuario no especificado."
                };

            try
            {
                var matches = await _proposalRepository.GetAcceptedMatchesForUserAsync(userId);

                return new GetAcceptedMatchesResponse
                {
                    Success = true,
                    Matches = matches,
                    TotalMatches = matches.Count,
                    Message = $"Se encontraron {matches.Count} matches."
                };
            }
            catch (Exception)
            {
                return new GetAcceptedMatchesResponse
                {
                    Success = false,
                    Message = "Error al obtener los matches."
                };
            }
        }

        public async Task<TfeCandidateDecisionResponse> DecideTfeCandidateAsync(string authorId, TfeCandidateDecisionRequest request)
        {
            if (string.IsNullOrWhiteSpace(authorId))
            {
                throw new ArgumentException("User ID cannot be empty.");
            }

            if (request == null || string.IsNullOrWhiteSpace(request.CandidateId) || request.TfeId <= 0)
            {
                throw new ArgumentException("Invalid decision request.");
            }

            if (request.Status is not (ProposalStatus.Accepted or ProposalStatus.Rejected))
            {
                throw new ArgumentException("Only accepted or rejected statuses are allowed.");
            }

            var tfe = await _tfeRepository.GetByIdAsync(request.TfeId);
            if (tfe == null)
            {
                throw new KeyNotFoundException("TFE not found.");
            }

            if (!string.Equals(tfe.AuthorId, authorId, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException("You do not have permission to decide this proposal.");
            }

            var proposal = await _proposalRepository.GetTfeProposalByUserIdAsync(request.CandidateId, request.TfeId);
            if (proposal == null)
            {
                throw new KeyNotFoundException("Candidate proposal not found.");
            }

            if (proposal.Status != ProposalStatus.Pending)
            {
                throw new InvalidOperationException("This proposal has already been resolved.");
            }

            proposal.Status = request.Status;
            await _proposalRepository.UpdateTfeProposalAsync(proposal);

            return new TfeCandidateDecisionResponse
            {
                Success = true,
                Message = request.Status == ProposalStatus.Accepted
                    ? "Candidate accepted successfully."
                    : "Candidate rejected successfully.",
                Status = proposal.Status
            };
        }
    }
}
