namespace NotificationService.Service
{
    public interface INotificationService
    {
        Task SendWeeklyNotificationsAsync(CancellationToken ct);
    }
}
