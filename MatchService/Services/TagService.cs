using MatchService.Repositories;
using Microsoft.EntityFrameworkCore;
using TFELibrary.Data;
using TFELibrary.Shared;

namespace MatchService.Services
{
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
            return CreateTagDto(await _tagRepository.GetByIdAsync(id));
        }

        public async Task<TagCreationResponse> CreateTagAsync(TagCreationRequest dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Tag.Name))
                throw new ArgumentException("El nombre del tag no puede estar vacío.");
            var tag = new Tag { Name = dto.Tag.Name };
            try
            {
                tag = await _tagRepository.CreateAsync(tag);
                return new TagCreationResponse { Tag = CreateTagDto(tag), TagId = tag.Id };
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException($"Ya existe un tag con el nombre '{tag.Name}'.");
            }
        }

        public async Task<bool> UpdateTagAsync(int id, TagUpdateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("El nombre del tag no puede estar vacío.");

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
                throw new InvalidOperationException($"Ya existe un tag con el nombre '{request.Name}'.");
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
}
