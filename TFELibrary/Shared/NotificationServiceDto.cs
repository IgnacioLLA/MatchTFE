namespace TFELibrary.Shared;

public class UserNotificationDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public NotificationFrequency NotificationFrequency { get; set; }
    public DateTime? LastNotificationSentAt { get; set; }
}

public class PendingNotificationsResponse
{
    public OperationResult Error { get; set; } = new OperationResult(false, string.Empty);
    public List<UserNotificationDto> Users { get; set; } = new();
}

public class MarkNotificationsSentRequest
{
    public List<string> UserIds { get; set; } = new();
}

// -- Notification data from MatchService --

public class NotificationDataRequest
{
    public List<UserNotificationContext> Users { get; set; } = new();
}

public class UserNotificationContext
{
    public string UserId { get; set; } = string.Empty;
    public DateTime? LastNotificationSentAt { get; set; }
}

public class NotificationDataResponse
{
    public List<UserNotificationData> Data { get; set; } = new();
}

public class UserNotificationData
{
    public string UserId { get; set; } = string.Empty;
    public List<PendingProposalSummary> PendingProposals { get; set; } = new();
    public int TotalPendingProposals { get; set; }
    public int NewMatchesCount { get; set; }
    public List<ExpiredTfeSummary> ExpiredTfes { get; set; } = new();
    public int ExpiredThisWeekCount { get; set; }
}

public class PendingProposalSummary
{
    public int TfeId { get; set; }
    public string TfeTitle { get; set; } = string.Empty;
    public int PendingCount { get; set; }
}

public class ExpiredTfeSummary
{
    public int TfeId { get; set; }
    public string TfeTitle { get; set; } = string.Empty;
    public DateOnly ExpirationDate { get; set; }
}
