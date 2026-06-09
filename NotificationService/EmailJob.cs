using NotificationService.Service;

namespace NotificationService
{
    public class EmailJob : BackgroundService
    {
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailJob> _logger;

        public EmailJob(INotificationService notificationService, IConfiguration configuration, ILogger<EmailJob> logger)
        {
            _notificationService = notificationService;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            var delay = GetInitialDelay();
            _logger.LogInformation("EmailJob scheduled. Next run in {Delay}.", delay);
            await Task.Delay(delay, ct);

            var interval = GetInterval();

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("EmailJob starting notification run.");
                    await _notificationService.SendWeeklyNotificationsAsync(ct);
                    _logger.LogInformation("EmailJob completed notification run.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "EmailJob failed during notification run.");
                }

                await Task.Delay(interval, ct);
            }
        }

        private TimeSpan GetInitialDelay()
        {
            if (int.TryParse(_configuration["EmailJob:InitialDelaySeconds"], out var seconds))
                return TimeSpan.FromSeconds(seconds);

            return GetDelayUntilNextMonday();
        }

        private TimeSpan GetInterval()
        {
            if (int.TryParse(_configuration["EmailJob:IntervalSeconds"], out var seconds))
                return TimeSpan.FromSeconds(seconds);

            return TimeSpan.FromDays(7);
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

