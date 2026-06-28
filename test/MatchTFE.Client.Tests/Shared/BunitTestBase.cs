using System.Net;
using System.Net.Http.Json;
using MatchTFE.Client.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Moq;
using Moq.Protected;
using MudBlazor;
using MudBlazor.Services;

namespace MatchTFE.Client.Tests.Shared;

public abstract class BunitTestBase : Bunit.BunitContext
{
    protected Mock<IStringLocalizer<SharedResources>> LocalizerMock { get; }

    protected BunitTestBase()
    {
        LocalizerMock = new Mock<IStringLocalizer<SharedResources>>();
        LocalizerMock
            .Setup(l => l[It.IsAny<string>()])
            .Returns<string>(key => new LocalizedString(key, key));
        LocalizerMock
            .Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns<string, object[]>((key, _) => new LocalizedString(key, key));

        Services.AddSingleton(LocalizerMock.Object);
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>
    /// Registra un HttpClient falso para "GatewayAPI" que devuelve la respuesta indicada.
    /// Debe llamarse antes de RenderComponent.
    /// </summary>
    protected void SetupGatewayApi(HttpStatusCode statusCode, object? body = null)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(() =>
            {
                var r = new HttpResponseMessage(statusCode);
                if (body != null) r.Content = JsonContent.Create(body);
                return Task.FromResult(r);
            });

        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        Services.AddSingleton(factory.Object);
    }

    /// <summary>
    /// Registra un HttpClient falso para "GatewayAPI" que lanza una excepción de red.
    /// </summary>
    protected void SetupGatewayApiThrows()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        Services.AddSingleton(factory.Object);
    }

    /// <summary>
    /// Registra un HttpClient falso que devuelve cada respuesta de la lista en orden,
    /// una por llamada HTTP. Útil cuando OnInitializedAsync hace varias peticiones distintas.
    /// </summary>
    protected void SetupGatewayApiSequence(params (HttpStatusCode status, object? body)[] steps)
    {
        var handler = new Mock<HttpMessageHandler>();
        var seq = handler.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());

        foreach (var step in steps)
        {
            var capturedStatus = step.status;
            var capturedBody = step.body;
            seq = seq.Returns(() =>
            {
                var r = new HttpResponseMessage(capturedStatus);
                if (capturedBody != null) r.Content = JsonContent.Create(capturedBody);
                return Task.FromResult(r);
            });
        }

        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        Services.AddSingleton(factory.Object);
    }

    /// <summary>
    /// Registra un ISnackbar mockeado, sobreescribiendo el registrado por AddMudServices.
    /// </summary>
    protected Mock<ISnackbar> SetupSnackbar()
    {
        var mock = new Mock<ISnackbar>();
        Services.AddSingleton(mock.Object);
        return mock;
    }
}
