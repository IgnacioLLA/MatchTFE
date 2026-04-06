using TFELibrary.Shared;

namespace MatchService.Services
{
    public interface ITfeService
    {
        Task<TfeCreationResponse> CreateTfeAsync(TfeCreationRequest request, string authorId);
        Task<TfeDto?> GetTfeByIdAsync(int id);
    }
}
