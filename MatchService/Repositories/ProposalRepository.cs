using Microsoft.EntityFrameworkCore;
using MatchService.Data;
using TFELibrary.Data;
using TFELibrary.Shared;

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

        public async Task<Dictionary<int, int>> GetInterestedCountsByTfeIdsAsync(List<int> tfeIds)
        {
            if (!tfeIds.Any()) return new Dictionary<int, int>();

            return await _context.TfeProposal
                .Where(tp => tfeIds.Contains(tp.TfeId) && tp.Status == ProposalStatus.Pending)
                .GroupBy(tp => tp.TfeId)
                .Select(g => new { TfeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TfeId, x => x.Count);
        }
    }
}
