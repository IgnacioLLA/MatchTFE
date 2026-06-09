using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using TFELibrary.Shared;

namespace NotificationService.Service
{
    public class NotificationService : INotificationService
    {
        private readonly IEmailService _emailService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IEmailService emailService, IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<NotificationService> logger)
        {
            _emailService = emailService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendWeeklyNotificationsAsync(CancellationToken ct)
        {
            var token = GenerateSystemToken();

            var users = await GetPendingUsersAsync(token, ct);
            if (users.Count == 0)
            {
                _logger.LogInformation("No users pending notification.");
                return;
            }

            _logger.LogInformation("Processing notifications for {Count} users.", users.Count);

            var notificationData = await GetNotificationDataAsync(users, token, ct);
            var dataByUser = notificationData.ToDictionary(d => d.UserId);

            var sentUserIds = new List<string>();

            foreach (var user in users)
            {
                if (ct.IsCancellationRequested) break;

                dataByUser.TryGetValue(user.UserId, out var data);

                if (!HasAnythingToReport(data)) continue;

                try
                {
                    var subject = "Tu resumen semanal de TFE";
                    var body = BuildEmailBody(user, data!);
                    await _emailService.SendEmailAsync(user.Email, $"{user.FirstName} {user.LastName}", subject, body, ct);
                    sentUserIds.Add(user.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send notification to user {UserId}.", user.UserId);
                }
            }

            var allProcessedIds = users.Select(u => u.UserId).ToList();
            if (allProcessedIds.Count > 0)
                await MarkNotificationsSentAsync(allProcessedIds, token, ct);

            _logger.LogInformation("Notifications sent to {Sent}/{Total} users.", sentUserIds.Count, users.Count);
        }

        // -------------------------------------------------------

        private async Task<List<UserNotificationDto>> GetPendingUsersAsync(string token, CancellationToken ct)
        {
            var client = CreateAuthorizedClient("UserServiceClient", token);
            var response = await client.GetFromJsonAsync<PendingNotificationsResponse>("api/user/notifications/pending", ct);
            return response?.Users ?? new List<UserNotificationDto>();
        }

        private async Task<List<UserNotificationData>> GetNotificationDataAsync(List<UserNotificationDto> users, string token, CancellationToken ct)
        {
            var client = CreateAuthorizedClient("MatchServiceClient", token);
            var request = new NotificationDataRequest
            {
                Users = users.Select(u => new UserNotificationContext
                {
                    UserId = u.UserId,
                    LastNotificationSentAt = u.LastNotificationSentAt
                }).ToList()
            };

            var response = await client.PostAsJsonAsync("api/match/notifications/data", request, ct);
            if (!response.IsSuccessStatusCode) return new List<UserNotificationData>();

            var result = await response.Content.ReadFromJsonAsync<NotificationDataResponse>(cancellationToken: ct);
            return result?.Data ?? new List<UserNotificationData>();
        }

        private async Task MarkNotificationsSentAsync(List<string> userIds, string token, CancellationToken ct)
        {
            var client = CreateAuthorizedClient("UserServiceClient", token);
            var response = await client.PutAsJsonAsync("api/user/notifications/mark-sent", new MarkNotificationsSentRequest { UserIds = userIds }, ct);
            if (!response.IsSuccessStatusCode)
                _logger.LogError("Failed to mark notifications as sent. Status: {StatusCode}", response.StatusCode);
        }

        private HttpClient CreateAuthorizedClient(string clientName, string token)
        {
            var client = _httpClientFactory.CreateClient(clientName);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private string GenerateSystemToken()
        {
            var secret = _configuration["JwtSettings:Secret"]!;
            var issuer = _configuration["JwtSettings:Issuer"]!;
            var audience = _configuration["JwtSettings:Audience"]!;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "notification-service"),
                    new Claim("role", "Service")
                },
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static bool HasAnythingToReport(UserNotificationData? data) =>
            data != null &&
            (data.TotalPendingProposals > 0 || data.NewMatchesCount > 0 || data.ExpiredTfes.Count > 0);

        private static string BuildEmailBody(UserNotificationDto user, UserNotificationData data)
        {
            var sections = new StringBuilder();

            if (data.PendingProposals.Count > 0)
            {
                var rows = string.Join("", data.PendingProposals.Select(p =>
                    $"<tr><td style='padding:6px 12px;'>{Encode(p.TfeTitle)}</td><td style='padding:6px 12px;text-align:center;font-weight:bold;'>{p.PendingCount}</td></tr>"));

                sections.Append($"""
                    <h3 style="color:#1976d2;margin-top:24px;">Propuestas pendientes de revisión</h3>
                    <p>Tienes <strong>{data.TotalPendingProposals}</strong> propuesta(s) pendiente(s) en total:</p>
                    <table style="border-collapse:collapse;width:100%;font-size:14px;">
                        <thead>
                            <tr style="background:#f5f5f5;">
                                <th style="padding:6px 12px;text-align:left;">TFE</th>
                                <th style="padding:6px 12px;">Pendientes</th>
                            </tr>
                        </thead>
                        <tbody>{rows}</tbody>
                    </table>
                """);
            }

            if (data.NewMatchesCount > 0)
            {
                sections.Append($"""
                    <h3 style="color:#388e3c;margin-top:24px;">Nuevos matches</h3>
                    <p>Desde tu última notificación, <strong>{data.NewMatchesCount}</strong> de tus propuestas han sido aceptadas.</p>
                """);
            }

            if (data.ExpiredTfes.Count > 0)
            {
                var items = string.Join("", data.ExpiredTfes.Select(t =>
                    $"<li>{Encode(t.TfeTitle)} <span style='color:#999;font-size:12px;'>(caducó el {t.ExpirationDate:dd/MM/yyyy})</span></li>"));

                sections.Append($"""
                    <h3 style="color:#f57c00;margin-top:24px;">TFEs caducados</h3>
                    <p>Los siguientes TFEs han caducado ({data.ExpiredThisWeekCount} esta semana):</p>
                    <ul style="padding-left:20px;">{items}</ul>
                """);
            }

            return $"""
                <!DOCTYPE html>
                <html lang="es">
                <body style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;color:#222;">
                    <h2 style="color:#1976d2;">Hola, {Encode(user.FirstName)}</h2>
                    <p>Aquí tienes tu resumen de actividad en MatchTFE:</p>
                    {sections}
                    <hr style="border:none;border-top:1px solid #eee;margin:24px 0;"/>
                    <p style="color:#888;font-size:12px;">
                        Puedes cambiar la frecuencia de estos correos desde tu perfil en la plataforma.<br/>
                        Este correo fue enviado automáticamente por MatchTFE.
                    </p>
                </body>
                </html>
                """;
        }

        private static string Encode(string s) => System.Net.WebUtility.HtmlEncode(s);
    }
}
