namespace NotificationService.Service
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct);
    }
}
