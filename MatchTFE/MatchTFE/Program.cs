using MatchTFE.Components;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("GatewayAPI", client =>
{
    client.BaseAddress = new Uri(GetConfiguredUrl("GATEWAY_BASE_URL", "https://apigateway/"));
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddMudServices();
builder.Services.AddLocalization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

var supportedCultures = new[] { "es-ES", "en" };
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("es-ES")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures));

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MatchTFE.Client._Imports).Assembly);


app.Run();

static string GetConfiguredUrl(string environmentVariableName, string fallbackUrl)
    => Environment.GetEnvironmentVariable(environmentVariableName)
       ?? fallbackUrl;
