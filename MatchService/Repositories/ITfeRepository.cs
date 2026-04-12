using Microsoft.EntityFrameworkCore;
using TFELibrary.Data;
using TFELibrary.Shared;

namespace MatchService.Repositories
{
    public interface ITfeRepository
    {
        Task<TFE?> GetByIdAsync(int id);
        Task<TFE> CreateAsync(TFE tag);
        Task DeleteAsync(TFE tag);
        Task<List<TFE>> GetByAuthorIdAsync(string authorId);
        Task UpdateAsync(TFE tfe);
        Task<bool> DeleteAsync(int id, string authorId);

        Task<List<TFE>> GetRecommendedTfesAsync(string userId, List<int> userInterestTagIds, int count);
    }
}
