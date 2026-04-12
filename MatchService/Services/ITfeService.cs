using TFELibrary.Shared;

namespace MatchService.Services
{
    public interface ITfeService
    {
        Task<TfeCreationResponse> CreateTfeAsync(TfeCreationRequest request, string authorId);
        Task<TfeDto?> GetTfeByIdAsync(int id);
        Task<List<TfeDto>> GetTfesByAuthorIdAsync(string authorId);
        Task<bool> UpdateTfeAsync(int id, TfeUpdateRequest request, string authorId);
    }
}
