
using Autofac;
using NotificationService.Service;

namespace NotificationService.Infrastructure
{
    public class NotificationServiceDependencyInjector
    {
        public static void RegisterDependencies(ContainerBuilder builder)
        {
            builder.RegisterType<EmailService>().As<IEmailService>().SingleInstance();
            builder.RegisterType<NotificationService.Service.NotificationService>().As<INotificationService>().SingleInstance();
        }
    }
}
