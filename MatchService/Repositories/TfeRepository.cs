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

        public async Task<TFE> CreateAsync(TFE tfe)
        {
            var skills = tfe.RequiredSkills.ToList();
            tfe.RequiredSkills.Clear();

            _context.Tfe.Add(tfe);
            await _context.SaveChangesAsync();

            foreach (var skill in skills)
            {
                skill.TfeId = tfe.Id;
                _context.TfeRequiredSkill.Add(skill);
            }

            await _context.SaveChangesAsync();

            return await _context.Tfe
                .Include(x => x.Author)
                .Include(x => x.Topics)
                .Include(x => x.RequiredSkills)
                    .ThenInclude(rs => rs.Tag)
                .AsSingleQuery()
                .FirstAsync(x => x.Id == tfe.Id);
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

        public async Task UpdateAsync(TFE tfe)
        {
            var skillsToSave = tfe.RequiredSkills?.ToList() ?? new List<TfeRequiredSkill>();

            var oldSkills = _context.TfeRequiredSkill.Where(rs => rs.TfeId == tfe.Id);
            _context.TfeRequiredSkill.RemoveRange(oldSkills);

            tfe.RequiredSkills?.Clear();
            _context.Tfe.Update(tfe);
            await _context.SaveChangesAsync();

            if (skillsToSave.Any())
            {
                foreach (var skill in skillsToSave)
                {
                    var newSkill = new TfeRequiredSkill
                    {
                        TfeId = tfe.Id,
                        TagId = skill.TagId,
                        Level = skill.Level
                    };
                    _context.TfeRequiredSkill.Add(newSkill);
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}
