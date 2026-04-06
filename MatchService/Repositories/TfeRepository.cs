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
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<TFE> CreateAsync(TFE Tfe)
        {
            _context.Tfe.Add(Tfe);
            await _context.SaveChangesAsync();

            return await _context.Tfe
                .Include(x => x.Author)
                .Include(x => x.Topics)
                .AsSingleQuery()
                .FirstAsync(x => x.Id == Tfe.Id);
        }

        public async Task DeleteAsync(TFE Tfe)
        {
            _context.Tfe.Remove(Tfe);
            await _context.SaveChangesAsync();
        }
    }
}
