using MatchService.Repositories;
using Microsoft.EntityFrameworkCore;
using TFELibrary.Data;
using TFELibrary.Shared;

namespace MatchService.Services;

public class TfeService : ITfeService
{
    private readonly ITfeRepository _tfeRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IProposalRepository _proposalRepository;

    public TfeService(ITfeRepository repository, ITagRepository tagRepository, IProposalRepository proposalRepository)
    {
        _tfeRepository = repository;
        _tagRepository = tagRepository;
        _proposalRepository = proposalRepository;
    }

    public async Task<TfeCreationResponse> CreateTfeAsync(TfeCreationRequest request, string authorId)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Tfe);

        CheckTfe(request.Tfe);
        ValidateExpirationDate(request.Tfe.ExpirationDate);

        request.Tfe.Status = TfeStatus.Open;
        request.Tfe.CreationDate = DateTime.UtcNow;
        var tfe = CreateTfeEntity(request.Tfe, authorId);

        try
        {
            await MapTagsAndSkillsAsync(tfe, request.Tfe);

            var createdTfe = await _tfeRepository.CreateAsync(tfe);
            var dto = CreateTfeDto(createdTfe);

            return new TfeCreationResponse { Error = new OperationResult(true, "TFE created successfully."), Tfe = dto, TfeId = createdTfe.Id };
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Ocurrió un error al guardar el TFE en la base de datos.", ex);
        }
    }

    public async Task<TfeDto?> GetTfeByIdAsync(int id)
    {
        return CreateTfeDto(await _tfeRepository.GetByIdAsync(id));
    }

    public async Task<List<TfeDto>> GetTfesByAuthorIdAsync(string authorId)
    {
        if (string.IsNullOrWhiteSpace(authorId))
            throw new ArgumentException("El ID del autor no puede estar vacío.", nameof(authorId));

        var tfes = await _tfeRepository.GetByAuthorIdAsync(authorId);
        var tfeIds = tfes.Select(t => t.Id).ToList();

        var interestedCounts = await _proposalRepository.GetInterestedCountsByTfeIdsAsync(tfeIds);

        var dtos = tfes.Select(t =>
        {
            var dto = CreateTfeDto(t);
            dto.InterestedAmount = interestedCounts.GetValueOrDefault(t.Id, 0);
            return dto;
        }).ToList();

        return dtos!;
    }

    public async Task<bool> UpdateTfeAsync(int id, TfeUpdateRequest request, string authorId)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Tfe);

        CheckTfe(request.Tfe);

        var existingTfe = await _tfeRepository.GetByIdAsync(id);

        if (existingTfe == null || existingTfe.AuthorId != authorId)
        {
            return false;
        }

        try
        {
            existingTfe.Title = request.Tfe.Title;
            existingTfe.Description = request.Tfe.Description;
            existingTfe.EstimatedDelivery = DateOnly.FromDateTime(request.Tfe.EstimatedDelivery);
            ValidateExpirationDate(request.Tfe.ExpirationDate, existingTfe.ExpirationDate);
            existingTfe.ExpirationDate = DateOnly.FromDateTime(request.Tfe.ExpirationDate);
            await MapTagsAndSkillsAsync(existingTfe, request.Tfe);
            await _tfeRepository.UpdateAsync(existingTfe);
            return true;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Ocurrió un error al actualizar el TFE en la base de datos.", ex);
        }
    }

    public async Task<bool> DeleteTfeAsync(int id, string authorId)
    {
        if (string.IsNullOrWhiteSpace(authorId))
            throw new ArgumentException("Author ID cannot be empty.");

        return await _tfeRepository.DeleteAsync(id, authorId);
    }

    public async Task<bool> DeleteTfeAsync(int id)
    {
        return await _tfeRepository.DeleteAsync(id);
    }

    public async Task<TfeRecommendedResponse> GetRecommendedTfesAsync(string userId, TfeRecommendedRequest request)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be empty.");

        if (request.Count <= 0) request.Count = 10;

        var userInterests = await _tagRepository.GetUserInterestsAsync(userId);
        var userInterestTagIds = userInterests.Select(t => t.Id).ToList();

        var tfes = await _tfeRepository.GetRecommendedTfesAsync(userId, userInterestTagIds, request.Count);
        var tfeDtos = tfes
            .Select(CreateTfeDto)
            .Where(tfeDto => tfeDto != null)
            .Select(tfeDto => tfeDto!)
            .ToList();

        return new TfeRecommendedResponse
        {
            Error = new OperationResult(true, string.Empty),
            Tfes = tfeDtos,
            TotalCount = tfeDtos.Count
        };
    }

    public async Task<OperationResult> ChangeTfeStatusAsync(int id, TfeStatus newStatus, string authorId)
    {
        if (newStatus is not (TfeStatus.Completed or TfeStatus.Cancelled))
            return new OperationResult(false, "Only Completed or Cancelled statuses are allowed.", "InvalidStatus");

        var tfe = await _tfeRepository.GetByIdAsync(id);
        if (tfe is null)
            return new OperationResult(false, "TFE not found.", "TfeNotFound");

        if (!string.Equals(tfe.AuthorId, authorId, StringComparison.Ordinal))
            return new OperationResult(false, "You do not have permission to change this TFE's status.", "Unauthorized");

        if (tfe.Status != TfeStatus.Open)
            return new OperationResult(false, "Only Open TFEs can be completed or cancelled.", "InvalidCurrentStatus");

        try
        {
            if (newStatus == TfeStatus.Cancelled)
                await _proposalRepository.ExpireProposalsByTfeIdAsync(id);

            await _tfeRepository.UpdateStatusAsync(id, newStatus);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error al cambiar el estado del TFE.", ex);
        }

        return new OperationResult(true, "TFE status updated successfully.");
    }

    public async Task<NotificationDataResponse> GetNotificationDataForUsersAsync(NotificationDataRequest request)
    {
        var oneWeekAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));

        var userIds = request.Users.Select(u => u.UserId).ToList();
        var sinceMap = request.Users.ToDictionary(
            u => u.UserId,
            u => u.LastNotificationSentAt.HasValue ? DateOnly.FromDateTime(u.LastNotificationSentAt.Value) : (DateOnly?)null);

        var pendingByUser = await _proposalRepository.GetPendingProposalsByAuthorsAsync(userIds);
        var matchesByUser = await _proposalRepository.GetNewMatchesSinceByUsersAsync(userIds, sinceMap);
        var expiredByUser = await _tfeRepository.GetExpiredTfesByAuthorsAsync(userIds);

        var data = request.Users.Select(user =>
        {
            var pendingEntities = pendingByUser.GetValueOrDefault(user.UserId, new List<TFEProposal>());
            var pendingProposals = pendingEntities
                .GroupBy(p => new { p.TfeId, p.Tfe.Title })
                .Select(g => new PendingProposalSummary
                {
                    TfeId = g.Key.TfeId,
                    TfeTitle = g.Key.Title,
                    PendingCount = g.Count()
                })
                .ToList();

            var expiredEntities = expiredByUser.GetValueOrDefault(user.UserId, new List<TFE>());
            var expiredTfes = expiredEntities
                .Select(t => new ExpiredTfeSummary { TfeId = t.Id, TfeTitle = t.Title, ExpirationDate = t.ExpirationDate })
                .ToList();

            return new UserNotificationData
            {
                UserId = user.UserId,
                PendingProposals = pendingProposals,
                TotalPendingProposals = pendingProposals.Sum(p => p.PendingCount),
                NewMatchesCount = matchesByUser.GetValueOrDefault(user.UserId, 0),
                ExpiredTfes = expiredTfes,
                ExpiredThisWeekCount = expiredTfes.Count(t => t.ExpirationDate >= oneWeekAgo)
            };
        }).ToList();

        return new NotificationDataResponse { Data = data };
    }

    // -------------------------------------------------------

    private void CheckTfe(TfeDto tfe)
    {
        if (tfe == null) throw new ArgumentNullException(nameof(tfe));
        if (string.IsNullOrWhiteSpace(tfe.Title)) throw new ArgumentException("Title is mandatory.", nameof(tfe));
        if (string.IsNullOrWhiteSpace(tfe.Description)) throw new ArgumentException("Description is mandatory.", nameof(tfe));
    }

    private static void ValidateExpirationDate(DateTime expirationDate, DateOnly? existingExpirationDate = null)
    {
        var selectedExpirationDate = DateOnly.FromDateTime(expirationDate);

        if (existingExpirationDate.HasValue && existingExpirationDate.Value == selectedExpirationDate)
        {
            return;
        }

        if (!TfeDateRules.IsValidExpirationDate(selectedExpirationDate))
        {
            throw new ArgumentException(
                $"La fecha de caducidad debe ser, como mínimo, el {TfeDateRules.MinimumExpirationDate.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CurrentCulture)}.",
                nameof(expirationDate));
        }
    }

    private TfeDto? CreateTfeDto(TFE? tfe)
    {
        if (tfe == null) return null;
        return new TfeDto
        {
            Id = tfe.Id,
            Title = tfe.Title,
            Description = tfe.Description,
            TutorName = tfe.Author.FirstName + " " + tfe.Author.LastName,
            Topics = tfe.Topics.Select(tag => new TagDto { Name = tag.Name }).ToList(),
            RequiredSkills = tfe.RequiredSkills
                .Where(skill => skill.Tag != null)
                .Select(skill => new SkillDto { Tag = skill.Tag.Name, Level = skill.Level })
                .ToList(),
            EstimatedDelivery = tfe.EstimatedDelivery.ToDateTime(TimeOnly.MinValue),
            ExpirationDate = tfe.ExpirationDate.ToDateTime(TimeOnly.MinValue),
            CreationDate = tfe.CreationDate.ToDateTime(TimeOnly.MinValue),
            Status = tfe.Status,
        };
    }

    private TFE CreateTfeEntity(TfeDto dto, string authorId)
    {
        return new TFE
        {
            AuthorId = authorId,
            Title = dto.Title,
            Description = dto.Description,
            EstimatedDelivery = DateOnly.FromDateTime(dto.EstimatedDelivery),
            ExpirationDate = DateOnly.FromDateTime(dto.ExpirationDate),
            CreationDate = DateOnly.FromDateTime(dto.CreationDate),
            Status = dto.Status
        };
    }

    private async Task MapTagsAndSkillsAsync(TFE tfeEntity, TfeDto dto)
    {
        tfeEntity.Topics = new List<Tag>();
        tfeEntity.RequiredSkills = new List<TfeRequiredSkill>();

        var topicNames = dto.Topics?.Select(t => t.Name) ?? Enumerable.Empty<string>();
        var skillNames = dto.RequiredSkills?.Select(s => s.Tag) ?? Enumerable.Empty<string>();
        var allNames = topicNames.Concat(skillNames).Distinct();

        var tagMap = await _tagRepository.GetByNamesAsync(allNames);

        if (dto.Topics != null)
        {
            foreach (var topicDto in dto.Topics)
            {
                if (!tagMap.TryGetValue(topicDto.Name, out var tag))
                    throw new ArgumentException($"Invalid '{topicDto.Name}' tag. Not found.");
                tfeEntity.Topics.Add(tag);
            }
        }

        if (dto.RequiredSkills != null)
        {
            foreach (var skillDto in dto.RequiredSkills)
            {
                if (!tagMap.TryGetValue(skillDto.Tag, out var skillTag))
                    throw new ArgumentException($"Invalid '{skillDto.Tag}' tag. Not found.");

                tfeEntity.RequiredSkills.Add(new TfeRequiredSkill
                {
                    TagId = skillTag.Id,
                    Level = skillDto.Level
                });
            }
        }
    }
}
