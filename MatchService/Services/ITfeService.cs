using TFELibrary.Shared;

namespace MatchService.Services;

public interface ITfeService
{
    Task<TfeCreationResponse> CreateTfeAsync(TfeCreationRequest request, string authorId);
    Task<TfeDto?> GetTfeByIdAsync(int id);
    Task<List<TfeDto>> GetTfesByAuthorIdAsync(string authorId);
    Task<bool> UpdateTfeAsync(int id, TfeUpdateRequest request, string authorId);
    Task<bool> DeleteTfeAsync(int id, string authorId);
    Task<TfeRecommendedResponse> GetRecommendedTfesAsync(string userId, TfeRecommendedRequest request);
    Task<OperationResult> ChangeTfeStatusAsync(int id, TfeStatus newStatus, string authorId);
    Task<NotificationDataResponse> GetNotificationDataForUsersAsync(NotificationDataRequest request);
}
