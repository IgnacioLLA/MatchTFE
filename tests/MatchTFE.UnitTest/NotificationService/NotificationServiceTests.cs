using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Json;
using TFELibrary.Shared;
using IEmailService = NotificationService.Service.IEmailService;
using SUT = NotificationService.Service.NotificationService;

namespace MatchTFE.UnitTest.Notifications;

[TestClass]
public class NotificationServiceTests
{
    private Mock<IEmailService> _emailMock = null!;
    private Mock<IHttpClientFactory> _httpFactoryMock = null!;
    private Mock<IConfiguration> _configMock = null!;
    private Mock<ILogger<SUT>> _loggerMock = null!;
    private Mock<HttpMessageHandler> _userHandlerMock = null!;
    private Mock<HttpMessageHandler> _matchHandlerMock = null!;
    private SUT _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _emailMock       = new Mock<IEmailService>();
        _httpFactoryMock = new Mock<IHttpClientFactory>();
        _configMock      = new Mock<IConfiguration>();
        _loggerMock      = new Mock<ILogger<SUT>>();
        _userHandlerMock  = new Mock<HttpMessageHandler>();
        _matchHandlerMock = new Mock<HttpMessageHandler>();

        _configMock.Setup(c => c["JwtSettings:Secret"]).Returns("test-secret-key-that-is-at-least-32-characters-long");
        _configMock.Setup(c => c["JwtSettings:Issuer"]).Returns("test-issuer");
        _configMock.Setup(c => c["JwtSettings:Audience"]).Returns("test-audience");

        _httpFactoryMock
            .Setup(f => f.CreateClient("UserServiceClient"))
            .Returns(() => new HttpClient(_userHandlerMock.Object) { BaseAddress = new Uri("http://userservice/") });
        _httpFactoryMock
            .Setup(f => f.CreateClient("MatchServiceClient"))
            .Returns(() => new HttpClient(_matchHandlerMock.Object) { BaseAddress = new Uri("http://matchservice/") });

        _sut = new SUT(_emailMock.Object, _httpFactoryMock.Object, _configMock.Object, _loggerMock.Object);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void SetupPendingUsers(List<UserNotificationDto> users)
    {
        var body = new PendingNotificationsResponse
        {
            Error = new OperationResult(true, string.Empty),
            Users = users
        };
        _userHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) });
    }

    private void SetupMatchData(List<UserNotificationData> data)
    {
        _matchHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new NotificationDataResponse { Data = data })
            });
    }

    private void SetupMarkSent(HttpStatusCode statusCode = HttpStatusCode.NoContent)
    {
        _userHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Put),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode));
    }

    private static UserNotificationDto MakeUser(string id = "user-1") => new()
    {
        UserId = id,
        Email = $"{id}@test.com",
        FirstName = "Test",
        LastName = "User",
        NotificationFrequency = NotificationFrequency.Weekly
    };

    private static UserNotificationData NoData(string userId) => new()
    {
        UserId = userId,
        PendingProposals = new(),
        ExpiredTfes = new()
    };

    private static UserNotificationData WithPending(string userId) => new()
    {
        UserId = userId,
        PendingProposals = new() { new PendingProposalSummary { TfeId = 1, TfeTitle = "TFE Test", PendingCount = 3 } },
        TotalPendingProposals = 3,
        ExpiredTfes = new()
    };

    private static UserNotificationData WithNewMatches(string userId) => new()
    {
        UserId = userId,
        PendingProposals = new(),
        NewMatchesCount = 2,
        ExpiredTfes = new()
    };

    private static UserNotificationData WithExpired(string userId) => new()
    {
        UserId = userId,
        PendingProposals = new(),
        ExpiredTfes = new() { new ExpiredTfeSummary { TfeId = 1, TfeTitle = "Expired TFE", ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)) } },
        ExpiredThisWeekCount = 1
    };

    // -------------------------------------------------------------------------
    // No pending users
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task SendWeeklyNotificationsAsync_WhenNoPendingUsers_DoesNotSendEmailNorMarkSent()
    {
        // Arrange
        SetupPendingUsers(new List<UserNotificationDto>());

        // Act
        await _sut.SendWeeklyNotificationsAsync(CancellationToken.None);

        // Assert
        _emailMock.Verify(e => e.SendEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _userHandlerMock.Protected().Verify("SendAsync",
            Times.Never(),
            ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Put),
            ItExpr.IsAny<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // User with no activity
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task SendWeeklyNotificationsAsync_WhenUserHasNothingToReport_DoesNotSendEmailButStillCallsMarkSent()
    {
        // Arrange
        var user = MakeUser();
        SetupPendingUsers(new() { user });
        SetupMatchData(new() { NoData(user.UserId) });
        SetupMarkSent();

        // Act
        await _sut.SendWeeklyNotificationsAsync(CancellationToken.None);

        // Assert
        _emailMock.Verify(e => e.SendEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _userHandlerMock.Protected().Verify("SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Put),
            ItExpr.IsAny<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Send triggers
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task SendWeeklyNotificationsAsync_WhenUserHasPendingProposals_SendsEmail()
    {
        // Arrange
        var user = MakeUser();
        SetupPendingUsers(new() { user });
        SetupMatchData(new() { WithPending(user.UserId) });
        SetupMarkSent();

        // Act
        await _sut.SendWeeklyNotificationsAsync(CancellationToken.None);

        // Assert
        _emailMock.Verify(e => e.SendEmailAsync(
            user.Email, $"{user.FirstName} {user.LastName}",
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task SendWeeklyNotificationsAsync_WhenUserHasNewMatches_SendsEmail()
    {
        // Arrange
        var user = MakeUser();
        SetupPendingUsers(new() { user });
        SetupMatchData(new() { WithNewMatches(user.UserId) });
        SetupMarkSent();

        // Act
        await _sut.SendWeeklyNotificationsAsync(CancellationToken.None);

        // Assert
        _emailMock.Verify(e => e.SendEmailAsync(
            user.Email, It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task SendWeeklyNotificationsAsync_WhenUserHasExpiredTfes_SendsEmail()
    {
        // Arrange
        var user = MakeUser();
        SetupPendingUsers(new() { user });
        SetupMatchData(new() { WithExpired(user.UserId) });
        SetupMarkSent();

        // Act
        await _sut.SendWeeklyNotificationsAsync(CancellationToken.None);

        // Assert
        _emailMock.Verify(e => e.SendEmailAsync(
            user.Email, It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -------------------------------------------------------------------------
    // Multiple users
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task SendWeeklyNotificationsAsync_WithMultipleUsers_SendsEmailOnlyToUsersWithDataAndCallsMarkSentOnce()
    {
        // Arrange
        var user1 = MakeUser("user-1");
        var user2 = MakeUser("user-2");
        SetupPendingUsers(new() { user1, user2 });
        SetupMatchData(new() { WithPending(user1.UserId), NoData(user2.UserId) });
        SetupMarkSent();

        // Act
        await _sut.SendWeeklyNotificationsAsync(CancellationToken.None);

        // Assert
        _emailMock.Verify(e => e.SendEmailAsync(
            user1.Email, It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _emailMock.Verify(e => e.SendEmailAsync(
            user2.Email, It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _userHandlerMock.Protected().Verify("SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Put),
            ItExpr.IsAny<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Failures
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task SendWeeklyNotificationsAsync_WhenEmailFails_ContinuesAndCallsMarkSent()
    {
        // Arrange
        var user = MakeUser();
        SetupPendingUsers(new() { user });
        SetupMatchData(new() { WithPending(user.UserId) });
        SetupMarkSent();
        _emailMock.Setup(e => e.SendEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP timeout"));

        // Act
        await _sut.SendWeeklyNotificationsAsync(CancellationToken.None);

        // Assert
        _userHandlerMock.Protected().Verify("SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Put),
            ItExpr.IsAny<CancellationToken>());
    }

    [TestMethod]
    public async Task SendWeeklyNotificationsAsync_WhenMatchServiceFails_DoesNotSendEmails()
    {
        // Arrange
        var user = MakeUser();
        SetupPendingUsers(new() { user });
        SetupMarkSent();
        _matchHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        // Act
        await _sut.SendWeeklyNotificationsAsync(CancellationToken.None);

        // Assert
        _emailMock.Verify(e => e.SendEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task SendWeeklyNotificationsAsync_WhenMarkSentFails_EmailWasSentAndNoExceptionThrown()
    {
        // Arrange
        var user = MakeUser();
        SetupPendingUsers(new() { user });
        SetupMatchData(new() { WithPending(user.UserId) });
        SetupMarkSent(HttpStatusCode.InternalServerError);

        // Act
        await _sut.SendWeeklyNotificationsAsync(CancellationToken.None);

        // Assert
        _emailMock.Verify(e => e.SendEmailAsync(
            user.Email, It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
