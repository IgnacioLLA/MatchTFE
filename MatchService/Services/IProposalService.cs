using TFELibrary.Shared;

namespace MatchService.Services
{
    public interface IProposalService
    {
        Task<TfeProposalUpdateResponse> UpdateTfeProposalAsync(TfeProposalUpdateRequest request);
        Task<TfeProposalCreationResponse> CreateTfeProposalAsync(string userId, TfeProposalCreationRequest request);
    }
}
