using System;
using System.Web.Mvc;
using Castle.Core;
using Castle.Facilities.FactorySupport;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Dal;
using Dal.Repositories.Abstract;
using Dal.Repositories.Concrete;
using NHibernate;
using Web.Controllers;

namespace Web.Infrastructure.IoC
{
    public static class WindsorBootStrapper
    {
        public static IWindsorContainer Container { get; private set; }

        public static void Initialize() {
            Container = new WindsorContainer();
            RegisterControllers();
            RegisterNHibernateSessionFactory();
            RegisterRepositories();
        }

        private static void RegisterRepositories() {
            Container.Register(
                Component.For<IUsers>().ImplementedBy<UserRepository>().LifeStyle.Transient,
                Component.For<IRoles>().ImplementedBy<RoleRepository>().LifeStyle.Transient,
                Component.For<IProfiles>().ImplementedBy<ProfileRepository>().LifeStyle.Transient
                );
        }

        private static void RegisterNHibernateSessionFactory() {
            Container.AddFacility<FactorySupportFacility>();
            Container.Register(Component.For<ISession>()
                                   .UsingFactoryMethod(() => NHibernateSessionStorage.RetrieveSession())
                                   .LifeStyle.Is(LifestyleType.Transient));
        }

        private static void RegisterControllers() {
            ControllerBuilder.Current.SetControllerFactory(new WindsorControllerFactory(Container));
            Container.RegisterControllers(typeof(HomeController).Assembly);
        }
    }
}