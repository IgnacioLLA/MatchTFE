using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UserService.Data;
using UserService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    var frontendOrigin = GetConfiguredUrl("FRONTEND_ORIGIN", "http://localhost:5000");

    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins(frontendOrigin)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    UserServiceDependencyInjector.RegisterDependencies(containerBuilder);
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]!)),
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.ContainsKey("AccessToken"))
            {
                context.Token = context.Request.Cookies["AccessToken"];
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers().AddControllersAsServices();

// DbContext configuration
var dbConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION")
                         ?? builder.Configuration.GetConnectionString("DbConnection");
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(dbConnectionString, x =>
        x.MigrationsHistoryTable("__UserMigrationsHistory")));

var app = builder.Build();

// Configure the HTTP request pipeline.
// app.UseHttpsRedirection();

app.UseCors("AllowBlazor");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    await MigrateDatabaseAsync(db);
}

app.Run();

static string GetConfiguredUrl(string environmentVariableName, string fallbackUrl)
    => Environment.GetEnvironmentVariable(environmentVariableName)
       ?? fallbackUrl;

static async Task MigrateDatabaseAsync(UserDbContext db)
{
    const int maxAttempts = 20;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            db.Database.Migrate();
            return;
        }
        catch when (attempt < maxAttempts)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }

    db.Database.Migrate();
}
