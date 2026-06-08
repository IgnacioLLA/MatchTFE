using Autofac;
using Autofac.Extensions.DependencyInjection;
using NotificationService;
using NotificationService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    NotificationServiceDependencyInjector.RegisterDependencies(containerBuilder);
});

builder.Services.AddHostedService<EmailJob>();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
