using Autofac;
using Autofac.Core;
using MatchService.Controllers;
using MatchService.Services;
using MatchService.Repositories;

namespace MatchService.Infrastructure
{
    public class MatchServiceDependencyInjector
    {
        public static void RegisterDependencies(ContainerBuilder builder)
        {
            builder.RegisterType<MatchController>().As<IMatchController>().InstancePerLifetimeScope();

            builder.RegisterType<Services.TagService>().As<ITagService>().InstancePerLifetimeScope();
            builder.RegisterType<TagRepository>().As<ITagRepository>().InstancePerLifetimeScope();

            builder.RegisterType<Services.TfeService>().As<ITfeService>().InstancePerLifetimeScope();
            builder.RegisterType<TfeRepository>().As<ITfeRepository>().InstancePerLifetimeScope();
        }
    }
}
