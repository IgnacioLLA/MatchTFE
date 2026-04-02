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

        public async Task<TagDto> CreateTagAsync(TagCreationRequest dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Tag.Name))
                throw new ArgumentException("El nombre del tag no puede estar vacío.");
            var tag = new Tag { Name = dto.Tag.Name };
            try
            {
                return CreateTagDto(await _tagRepository.CreateAsync(tag));
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException($"Ya existe un tag con el nombre '{tag.Name}'.");
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
                Name = tag?.Name
            };
        }   
    }
}
