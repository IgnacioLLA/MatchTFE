using Autofac.Extensions.DependencyInjection;
using MatchTFE.Client.Handlers;
using MatchTFE.Client.Infrastructure;
using MatchTFE.Client.Localization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using System.Globalization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddTransient<CookieHandler>();
builder.Services.AddTransient<AuthInterceptor>();
builder.Services.AddTransient<ForbiddenInterceptor>();

var gatewayBaseUrl = builder.HostEnvironment.BaseAddress;

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
.AddHttpMessageHandler<AuthInterceptor>()
.AddHttpMessageHandler<ForbiddenInterceptor>();

builder.ConfigureContainer(new AutofacServiceProviderFactory(containerBuilder =>
{
    MatchTFEDependencyInjector.RegisterDependencies(containerBuilder);
}));

builder.Services.AddLocalization();
builder.Services.AddMudServices();
builder.Services.AddSingleton<MudLocalizer, AppMudLocalizer>();

var host = builder.Build();

var js = host.Services.GetRequiredService<IJSRuntime>();
var storedCulture = await js.InvokeAsync<string?>("blazorCulture.get");
var culture = new CultureInfo(string.IsNullOrWhiteSpace(storedCulture) ? "es-ES" : storedCulture);
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RunAsync();
