using Microsoft.EntityFrameworkCore;
using MatchService.Data;
using TFELibrary.Data;

namespace MatchService.Repositories
{
    public class ProposalRepository : IProposalRepository
    {
        private readonly MatchDbContext _context;

        public ProposalRepository(MatchDbContext context)
        {
            _context = context;
        }

        public async Task<bool> TfeProposalExistsAsync(string userId, int tfeId)
        {
            return await _context.TfeProposal
                .AnyAsync(tp => tp.OriginUserId == userId && tp.TfeId == tfeId);
        }

        public async Task CreateTfeProposalAsync(TFEProposal proposal)
        {
            _context.TfeProposal.Add(proposal);
            await _context.SaveChangesAsync();
        }
    }
}
