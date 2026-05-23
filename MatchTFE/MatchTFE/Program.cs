using MatchTFE.Client.Pages;
using MatchTFE.Components;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("GatewayAPI", client =>
{
    client.BaseAddress = new Uri(GetConfiguredUrl("GATEWAY_BASE_URL", "http://apigateway/"));
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddMudServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MatchTFE.Client._Imports).Assembly);


app.Run();

static string GetConfiguredUrl(string environmentVariableName, string fallbackUrl)
    => Environment.GetEnvironmentVariable(environmentVariableName)
       ?? fallbackUrl;
