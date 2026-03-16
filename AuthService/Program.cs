using AuthService.Data;
using AuthService.Infrastructure;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var dbConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION")
                         ?? builder.Configuration.GetConnectionString("DbConnection");
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(dbConnectionString));

// Esta opción funciona en .NET pero habría que definirlo similar a como se definiría en el appsettings.json y hace solo la comprobación de mirar en las var de entorno y luego el appsettings. Debería definir la cadena así: ConnectionStrings__SharedDbConnection : ...
// var dbConnectionString = builder.Configuration.GetConnectionString("SharedDbConnection");

builder.Services.AddIdentityCore<MatchUser>(options =>
{
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<AuthDbContext>();

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    AuthServiceDependencyInjector.RegisterDependencies(containerBuilder);
});

builder.Services.AddControllers().AddControllersAsServices();

//builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AuthDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al migrar la base de datos.");
    }
}

app.Run();
