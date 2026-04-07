using MatchService.Data;
using Microsoft.EntityFrameworkCore;
using TFELibrary.Data;

namespace MatchService.Repositories
{
    public class TfeRepository : ITfeRepository
    {

        private readonly MatchDbContext _context;

        public TfeRepository(MatchDbContext context)
        {
            _context = context;
        }

        public async Task<TFE?> GetByIdAsync(int id)
        {
            return await _context.Tfe
                .Include(t => t.Author)
                .Include(t => t.Topics)
                .Include(t => t.RequiredSkills)
                    .ThenInclude(rs => rs.Tag)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<TFE> CreateAsync(TFE Tfe)
        {
            _context.Tfe.Add(Tfe);
            await _context.SaveChangesAsync();

            foreach (var skill in Tfe.RequiredSkills)
            {
                skill.TfeId = Tfe.Id;
                _context.TfeRequiredSkill.Add(skill);
            }

            if (Tfe.RequiredSkills.Count > 0)
            {
                await _context.SaveChangesAsync();
            }

            return await _context.Tfe
                .Include(x => x.Author)
                .Include(x => x.Topics)
                .Include(x => x.RequiredSkills)
                    .ThenInclude(rs => rs.Tag)
                .AsSingleQuery()
                .FirstAsync(x => x.Id == Tfe.Id);
        }

        public async Task DeleteAsync(TFE Tfe)
        {
            _context.Tfe.Remove(Tfe);
            await _context.SaveChangesAsync();
        }

        public async Task<List<TFE>> GetByAuthorIdAsync(string authorId)
        {
            return await _context.Tfe
                .Where(t => t.AuthorId == authorId)
                .Include(t => t.Author)
                .Include(t => t.Topics)
                .Include(t => t.RequiredSkills)
                    .ThenInclude(rs => rs.Tag)
                .ToListAsync();
        }
    }
}
