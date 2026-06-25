using MatchService.Data;
using Microsoft.EntityFrameworkCore;
using TFELibrary.Data;
using TFELibrary.Shared;

namespace MatchService.Repositories;

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

    public async Task<TFEProposal?> GetTfeProposalByUserIdAsync(string userId, int tfeId)
    {
        return await _context.TfeProposal
            .Include(t => t.OriginUser)
            .Include(t => t.Tfe)
            .FirstOrDefaultAsync(t => t.TfeId == tfeId && userId == t.OriginUserId);
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

    public async Task UpdateTfeProposalAsync(TFEProposal proposal)
    {
        _context.TfeProposal.Update(proposal);
        await _context.SaveChangesAsync();
    }

    public async Task ExpireProposalsByTfeIdAsync(int tfeId)
    {
        await _context.TfeProposal
            .Where(p => p.TfeId == tfeId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, ProposalStatus.Expired));
    }

    public async Task<List<AcceptedMatchDto>> GetAcceptedMatchesForUserAsync(string userId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var matches = await _context.TfeProposal
            .Where(tp => (tp.Tfe.AuthorId == userId || tp.OriginUserId == userId)
                      && tp.Status == ProposalStatus.Accepted
                      && tp.Tfe.ExpirationDate >= today
                      && tp.Tfe.Status == TfeStatus.Open)
            .Include(tp => tp.OriginUser)
            .Include(tp => tp.Tfe)
            .ThenInclude(t => t.Author)
            .AsNoTracking()
            .ToListAsync();

        var result = new List<AcceptedMatchDto>();

        foreach (var proposal in matches)
        {
            var matchedUser = proposal.Tfe.AuthorId == userId
                ? proposal.OriginUser
                : proposal.Tfe.Author;

            result.Add(new AcceptedMatchDto
            {
                MatchedUserId = matchedUser.UserId,
                MatchedUserFullName = $"{matchedUser.FirstName} {matchedUser.LastName}",
                MatchedUserEmail = matchedUser.Email,
                MatchedUserRole = matchedUser.Role,
                MatchedUserAcademicYear = matchedUser.AcademicYear,
                MatchedUserDepartment = matchedUser.Department,
                TfeId = proposal.Tfe.Id,
                TfeTitle = proposal.Tfe.Title,
                TfeTutorName = $"{proposal.Tfe.Author.FirstName} {proposal.Tfe.Author.LastName}",
                MatchDate = proposal.CreationDate.ToDateTime(TimeOnly.MinValue),
                Status = ProposalStatus.Accepted
            });
        }

        return result.DistinctBy(m => new { m.MatchedUserId, m.TfeId }).ToList();
    }

    public async Task<Dictionary<string, List<TFEProposal>>> GetPendingProposalsByAuthorsAsync(List<string> authorIds)
    {
        var proposals = await _context.TfeProposal
            .Where(tp => authorIds.Contains(tp.Tfe.AuthorId) && tp.Status == ProposalStatus.Pending)
            .Include(tp => tp.Tfe)
            .AsNoTracking()
            .ToListAsync();

        return proposals
            .GroupBy(tp => tp.Tfe.AuthorId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public async Task<Dictionary<string, int>> GetNewMatchesSinceByUsersAsync(List<string> userIds, Dictionary<string, DateOnly?> sinceMap)
    {
        var proposals = await _context.TfeProposal
            .Where(tp => userIds.Contains(tp.OriginUserId) && tp.Status == ProposalStatus.Accepted)
            .Select(tp => new { tp.OriginUserId, tp.CreationDate })
            .AsNoTracking()
            .ToListAsync();

        return userIds.ToDictionary(
            userId => userId,
            userId =>
            {
                var since = sinceMap.GetValueOrDefault(userId);
                return proposals.Count(tp =>
                    tp.OriginUserId == userId &&
                    (!since.HasValue || tp.CreationDate >= since.Value));
            });
    }
}
