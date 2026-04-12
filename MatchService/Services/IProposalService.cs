using TFELibrary.Shared;

namespace MatchService.Services
{
    public interface IProposalService
    {
        Task<TfeProposalResponse> CreateTfeProposalAsync(string userId, TfeProposalRequest request);
    }
}
