using AuthService.Controllers;
using AuthService.Service;
using AuthService.Repositories;
using Autofac;
using MatchTFE.AuthService.Repositories;

namespace AuthService.Infrastructure
{
    public class AuthServiceDependencyInjector
    {
        public static void RegisterDependencies(ContainerBuilder builder)
        {
            builder.RegisterType<AuthController>().As<IAuthController>().SingleInstance();
            builder.RegisterType<Service.AuthService>().As<IAuthService>().SingleInstance();
            builder.RegisterType<AuthRepository>().As<IAuthRepository>().SingleInstance();
        }
    }
}
