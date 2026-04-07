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
    }
}
