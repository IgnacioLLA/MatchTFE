using MatchService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using TFELibrary.Data;

namespace MatchService.Repositories
{
    public class TagRepository : ITagRepository
    {
        private readonly MatchDbContext _context;

        public TagRepository(MatchDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Tag>> GetAllAsync()
        {
            return await _context.Tag.ToListAsync<Tag>();
        }

        public async Task<Tag?> GetByIdAsync(int id)
        {
            return await _context.Tag.FindAsync(id);
        }

        public async Task<Tag?> GetByNameAsync(string name)
        {
            return await _context.Tag.FirstOrDefaultAsync(t => t.Name == name);
        }

        public async Task<Dictionary<string, Tag>> GetByNamesAsync(IEnumerable<string> names)
        {
            var nameList = names.ToList();
            return await _context.Tag
                .Where(t => nameList.Contains(t.Name))
                .ToDictionaryAsync(t => t.Name);
        }

        public async Task<Tag> CreateAsync(Tag tag)
        {
            _context.Tag.Add(tag);
            await _context.SaveChangesAsync();
            return tag;
        }

        public async Task<Tag> UpdateAsync(Tag tag)
        {
            _context.Tag.Update(tag);
            await _context.SaveChangesAsync();
            return tag;
        }

        public async Task DeleteAsync(Tag tag)
        {
            _context.Tag.Remove(tag);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Tag>> GetUserInterestsAsync(string userId)
        {
            return await _context.UserInterest
                .Where(ui => ui.UserProfileId == userId)
                .Include(ui => ui.Tag)
                .Select(ui => ui.Tag)
                .ToListAsync();
        }
    }
}
