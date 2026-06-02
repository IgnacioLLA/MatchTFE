using MatchService.Repositories;
using Microsoft.EntityFrameworkCore;
using TFELibrary.Data;
using TFELibrary.Shared;

namespace MatchService.Services;

public class TagService : ITagService
{
    private readonly ITagRepository _tagRepository;

    public TagService(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository;
    }

    public async Task<IEnumerable<TagDto>> GetAllTagsAsync()
    {
        return (await _tagRepository.GetAllAsync()).Select(CreateTagDto);
    }

    public async Task<TagDto?> GetTagByIdAsync(int id)
    {
        var tag = await _tagRepository.GetByIdAsync(id);
        return tag is null ? null : CreateTagDto(tag);
    }

    public async Task<TagCreationResponse> CreateTagAsync(TagCreationRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Tag.Name))
            throw new ArgumentException("Tag name cannot be empty.");
        var tag = new Tag { Name = dto.Tag.Name };
        try
        {
            tag = await _tagRepository.CreateAsync(tag);
            return new TagCreationResponse { Error = new OperationResult(true, "Tag created successfully."), Tag = CreateTagDto(tag), TagId = tag.Id };
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException($"A tag with the name '{tag.Name}' already exists.");
        }
    }

    public async Task<bool> UpdateTagAsync(int id, TagUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tag name cannot be empty.");

        var tag = await _tagRepository.GetByIdAsync(id);
        if (tag == null) return false;

        tag.Name = request.Name;
        try
        {
            await _tagRepository.UpdateAsync(tag);
            return true;
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException($"A tag with the name '{request.Name}' already exists.");
        }
    }

    public async Task<bool> DeleteTagAsync(int id)
    {
        var tag = await _tagRepository.GetByIdAsync(id);

        if (tag == null) return false;

        await _tagRepository.DeleteAsync(tag);
        return true;
    }

    private TagDto CreateTagDto(Tag? tag)
    {
        return new TagDto
        {
            Id = tag?.Id ?? 0,
            Name = tag?.Name ?? string.Empty,
        };
    }
}
