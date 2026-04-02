using MatchService.Repositories;
using Microsoft.EntityFrameworkCore;
using TFELibrary.Data;

namespace MatchService.Services
{
    public class TagService : ITagService
    {
        private readonly ITagRepository _tagRepository;

        public TagService(ITagRepository tagRepository)
        {
            _tagRepository = tagRepository;
        }

        public async Task<IEnumerable<Tag>> GetAllTagsAsync()
        {
            return await _tagRepository.GetAllAsync();
        }

        public async Task<Tag?> GetTagByIdAsync(int id)
        {
            return await _tagRepository.GetByIdAsync(id);
        }

        public async Task<Tag> CreateTagAsync(Tag tag)
        {
            if (string.IsNullOrWhiteSpace(tag.Name))
                throw new ArgumentException("El nombre del tag no puede estar vacío.");

            try
            {
                return await _tagRepository.CreateAsync(tag);
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
    }
}
