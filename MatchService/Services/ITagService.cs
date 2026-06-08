using TFELibrary.Shared;

namespace MatchService.Services
{
    public interface ITagService
    {
        Task<IEnumerable<TagDto>> GetAllTagsAsync();
        Task<TagDto?> GetTagByIdAsync(int id);
        Task<TagCreationResponse> CreateTagAsync(TagCreationRequest request);
        Task<bool> UpdateTagAsync(int id, TagUpdateRequest request);
        Task<bool> DeleteTagAsync(int id);
    }
}
