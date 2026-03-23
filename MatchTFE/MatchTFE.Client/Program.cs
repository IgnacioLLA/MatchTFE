using Autofac.Extensions.DependencyInjection;
using MatchTFE.Client.Handlers;
using MatchTFE.Client.Infrastructure;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddTransient<CookieHandler>();
builder.Services.AddTransient<AuthInterceptor>();

builder.Services.AddHttpClient("CleanAPI", client =>
{
    client.BaseAddress = new Uri("http://localhost:5080/");
})
.AddHttpMessageHandler<CookieHandler>();

builder.Services.AddHttpClient("GatewayAPI", client =>
{
    client.BaseAddress = new Uri("http://localhost:5080/");
})
.AddHttpMessageHandler<CookieHandler>()
.AddHttpMessageHandler<AuthInterceptor>();

builder.ConfigureContainer(new AutofacServiceProviderFactory(containerBuilder =>
{
    MatchTFEDependencyInjector.RegisterDependencies(containerBuilder);
}));

builder.Services.AddMudServices();

await builder.Build().RunAsync();
