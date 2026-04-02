using TFELibrary.Data;
using TFELibrary.Shared;

namespace MatchService.Services
{
    public interface ITagService
    {
        Task<IEnumerable<TagDto>> GetAllTagsAsync();
        Task<TagDto?> GetTagByIdAsync(int id);
        Task<TagDto> CreateTagAsync(TagCreationRequest request);
        Task<bool> DeleteTagAsync(int id);
    }
}
