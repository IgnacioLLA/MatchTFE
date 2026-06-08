using NotificationService.Service;

namespace NotificationService
{
    public class EmailJob : BackgroundService
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<EmailJob> _logger;

        public EmailJob(INotificationService notificationService, ILogger<EmailJob> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            var delay = GetDelayUntilNextMonday();
            _logger.LogInformation("EmailJob scheduled. Next run in {Delay}.", delay);
            await Task.Delay(delay, ct);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("EmailJob starting weekly notification run.");
                    await _notificationService.SendWeeklyNotificationsAsync(ct);
                    _logger.LogInformation("EmailJob completed weekly notification run.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "EmailJob failed during weekly notification run.");
                }

                await Task.Delay(TimeSpan.FromDays(7), ct);
            }
        }

        private static TimeSpan GetDelayUntilNextMonday()
        {
            var now = DateTime.UtcNow;
            var daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0 && now.Hour >= 8)
                daysUntilMonday = 7;

            var nextMonday = now.Date.AddDays(daysUntilMonday).AddHours(8);
            return nextMonday - now;
        }
    }
}

