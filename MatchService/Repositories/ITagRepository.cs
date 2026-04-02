using TFELibrary.Data;

namespace MatchService.Repositories
{
    public interface ITagRepository
    {
        Task<IEnumerable<Tag>> GetAllAsync();
        Task<Tag?> GetByIdAsync(int id);
        Task<Tag> CreateAsync(Tag tag);
        Task DeleteAsync(Tag tag);
    }
}
