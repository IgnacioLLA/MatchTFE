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

        public async Task<TfeProposalResponse> CreateTfeProposalAsync(string userId, TfeProposalRequest request)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty.");

            var alreadyExists = await _proposalRepository.TfeProposalExistsAsync(userId, request.TfeId);
            if (alreadyExists)
                return new TfeProposalResponse { Success = false, Message = "You already have a proposal for this TFE." };

            var status = request.IsInterested
                ? ProposalStatus.Pending
                : ProposalStatus.Rejected;

            var proposal = new TFEProposal
            {
                OriginUserId = userId,
                TfeId = request.TfeId,
                Status = status,
                CreationDate = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            await _proposalRepository.CreateTfeProposalAsync(proposal);

            return new TfeProposalResponse { Success = true, Message = "Proposal created successfully." };
        }
    }
}
