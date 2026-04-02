using TFELibrary.Data;

namespace MatchService.Services
{
    public interface ITagService
    {
        Task<IEnumerable<Tag>> GetAllTagsAsync();
        Task<Tag?> GetTagByIdAsync(int id);
        Task<Tag> CreateTagAsync(Tag tag);
        Task<bool> DeleteTagAsync(int id);
    }
}
