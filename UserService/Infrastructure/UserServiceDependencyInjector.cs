using Autofac;
using UserService.Controllers;
using UserService.Repositories;
using UserService.Service;

namespace UserService.Infrastructure
{
    public class UserServiceDependencyInjector
    {
        public static void RegisterDependencies(ContainerBuilder builder)
        {
            builder.RegisterType<UserController>().As<IUserController>().InstancePerLifetimeScope();
            builder.RegisterType<Service.UserService>().As<IUserService>().InstancePerLifetimeScope();
            builder.RegisterType<UserProfileRepository>().As<IUserProfileRepository>().InstancePerLifetimeScope();
        }
    }
}
