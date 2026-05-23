using Autofac.Extensions.DependencyInjection;
using MatchTFE.Client.Handlers;
using MatchTFE.Client.Infrastructure;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddTransient<CookieHandler>();
builder.Services.AddTransient<AuthInterceptor>();

var gatewayBaseUrl = Environment.GetEnvironmentVariable("GATEWAY_BASE_URL");
if (string.IsNullOrWhiteSpace(gatewayBaseUrl))
{
    var hostBaseUri = new Uri(builder.HostEnvironment.BaseAddress);
    gatewayBaseUrl = $"{hostBaseUri.Scheme}://{hostBaseUri.Host}:5080/";
}

builder.Services.AddHttpClient("CleanAPI", client =>
{
    client.BaseAddress = new Uri(gatewayBaseUrl);
})
.AddHttpMessageHandler<CookieHandler>();

builder.Services.AddHttpClient("GatewayAPI", client =>
{
    client.BaseAddress = new Uri(gatewayBaseUrl);
})
.AddHttpMessageHandler<CookieHandler>()
.AddHttpMessageHandler<AuthInterceptor>();

builder.ConfigureContainer(new AutofacServiceProviderFactory(containerBuilder =>
{
    MatchTFEDependencyInjector.RegisterDependencies(containerBuilder);
}));

builder.Services.AddMudServices();

await builder.Build().RunAsync();
