using AuthService.Controllers;
using AuthService.Repositories;
using AuthService.Service;
using Autofac;
using MatchTFE.AuthService.Repositories;

namespace AuthService.Infrastructure
{
    public class AuthServiceDependencyInjector
    {
        public static void RegisterDependencies(ContainerBuilder builder)
        {
            builder.RegisterType<AuthController>().As<IAuthController>().InstancePerLifetimeScope();
            builder.RegisterType<Service.AuthService>().As<IAuthService>().InstancePerLifetimeScope();
            builder.RegisterType<AuthRepository>().As<IAuthRepository>().InstancePerLifetimeScope();
        }
    }
}
