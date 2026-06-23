using TFELibrary.Data;
using TFELibrary.Shared;

namespace MatchService.Repositories;

public interface ITfeRepository
{
    Task<TFE?> GetByIdAsync(int id);
    Task<TFE> CreateAsync(TFE tfe);
    Task DeleteAsync(TFE tfe);
    Task<List<TFE>> GetByAuthorIdAsync(string authorId);
    Task UpdateAsync(TFE tfe);
    Task<bool> DeleteAsync(int id);
    Task<List<TFE>> GetRecommendedTfesAsync(string userId, List<int> userInterestTagIds, int count);
    Task UpdateStatusAsync(int id, TfeStatus status);
    Task<Dictionary<string, List<TFE>>> GetExpiredTfesByAuthorsAsync(List<string> authorIds);
}
