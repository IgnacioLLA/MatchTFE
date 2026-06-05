using Autofac.Extensions.DependencyInjection;
using MatchTFE.Client.Handlers;
using MatchTFE.Client.Infrastructure;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

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

builder.Services.AddMudServices();

await builder.Build().RunAsync();
