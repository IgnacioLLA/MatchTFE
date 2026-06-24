using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NotificationService;
using NotificationService.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    NotificationServiceDependencyInjector.RegisterDependencies(containerBuilder);
});

builder.Services.AddHostedService<EmailJob>();

builder.Services.AddHttpClient("UserServiceClient", client =>
{
    client.BaseAddress = new Uri(GetConfiguredUrl("USER_SERVICE_BASE_URL", "http://userservice:8080/"));
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("MatchServiceClient", client =>
{
    client.BaseAddress = new Uri(GetConfiguredUrl("MATCH_SERVICE_BASE_URL", "http://matchservice:8080/"));
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                builder.Configuration["JwtSettings:Secret"]
                ?? throw new InvalidOperationException("Missing configuration: JwtSettings:Secret"))),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static string GetConfiguredUrl(string environmentVariableName, string fallbackUrl)
    => Environment.GetEnvironmentVariable(environmentVariableName) ?? fallbackUrl;
