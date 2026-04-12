using TFELibrary.Data;

namespace MatchService.Repositories
{
    public interface IProposalRepository
    {
        Task<bool> TfeProposalExistsAsync(string userId, int tfeId);
        Task CreateTfeProposalAsync(TFEProposal proposal);
        Task<Dictionary<int, int>> GetInterestedCountsByTfeIdsAsync(List<int> tfeIds);
    }
}
