using MatchService.Repositories;
using TFELibrary.Data;
using TFELibrary.Shared;

namespace MatchService.Services
{
    public class ProposalService : IProposalService
    {
        private readonly IProposalRepository _proposalRepository;

        public ProposalService(IProposalRepository proposalRepository)
        {
            _proposalRepository = proposalRepository;
        }

        public async Task<TfeProposalCreationResponse> CreateTfeProposalAsync(string userId, TfeProposalCreationRequest request)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty.");

            var existingTfe = await _proposalRepository.GetTfeProposalByUserIdAsync(userId, request.TfeId);

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
            var existingTfe = await _proposalRepository.GetTfeProposalByUserIdAsync(request.UserId, request.TfeId);
            if(existingTfe == null)
                return new TfeProposalUpdateResponse { Success = false, Message = "No preovious propose." };
            if(existingTfe.Status.Equals(ProposalStatus.Pending))
                return new TfeProposalUpdateResponse { Success = false, Message = "Invalid proposal status." };
            if (request.IsInterested)
                existingTfe.Status = ProposalStatus.Accepted;
            else
                existingTfe.Status = ProposalStatus.Rejected;
            _proposalRepository.UpdateTfeProposalAsync(existingTfe);
            return new TfeProposalUpdateResponse { Success = true, Message = "TFE proposal updated successfully." };
        }
    }
}
