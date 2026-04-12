using TFELibrary.Data;

namespace MatchService.Repositories
{
    public interface ITagRepository
    {
        Task<IEnumerable<Tag>> GetAllAsync();
        Task<Tag?> GetByIdAsync(int id);
        Task<Tag?> GetByNameAsync(string name);
        Task<Tag> CreateAsync(Tag tag);
        Task DeleteAsync(Tag tag);
        Task<List<Tag>> GetUserInterestsAsync(string userId);
    }
}
