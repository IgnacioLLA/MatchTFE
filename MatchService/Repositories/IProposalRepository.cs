using TFELibrary.Data;

namespace MatchService.Repositories
{
    public interface IProposalRepository
    {
        Task<bool> TfeProposalExistsAsync(string userId, int tfeId);
        Task CreateTfeProposalAsync(TFEProposal proposal);
    }
}
