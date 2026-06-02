using TFELibrary.Data;

namespace MatchService.Repositories;

public interface ITfeRepository
{
    Task<TFE?> GetByIdAsync(int id);
    Task<TFE> CreateAsync(TFE tfe);
    Task DeleteAsync(TFE tfe);
    Task<List<TFE>> GetByAuthorIdAsync(string authorId);
    Task UpdateAsync(TFE tfe);
    Task<bool> DeleteAsync(int id, string authorId);
    Task<List<TFE>> GetRecommendedTfesAsync(string userId, List<int> userInterestTagIds, int count);
}
