using AuthService.Controllers;
using AuthService.Data;
using AuthService.Repositories;
using AuthService.Service;
using Autofac;
using MatchTFE.AuthService.Repositories;
using Microsoft.EntityFrameworkCore;
using TFELibrary.Data;

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
