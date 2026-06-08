using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace NotificationService.Service
{
    public class EmailService : IEmailService
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _user;
        private readonly string _password;
        private readonly string _fromName;

        public EmailService(IConfiguration configuration)
        {
            _host     = configuration["EmailSettings:Host"]     ?? "smtp.gmail.com";
            _port     = int.Parse(configuration["EmailSettings:Port"] ?? "587");
            _user     = configuration["EmailSettings:User"]     ?? throw new InvalidOperationException("EmailSettings:User is required.");
            _password = configuration["EmailSettings:Password"] ?? throw new InvalidOperationException("EmailSettings:Password is required.");
            _fromName = configuration["EmailSettings:FromName"] ?? "MatchTFE";
        }

        public async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, _user));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_host, _port, SecureSocketOptions.StartTls, ct);
            await smtp.AuthenticateAsync(_user, _password, ct);
            await smtp.SendAsync(message, ct);
            await smtp.DisconnectAsync(true, ct);
        }
    }
}

