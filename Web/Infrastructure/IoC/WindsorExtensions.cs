using System;
using System.Reflection;
using Castle.Core;
using Castle.Windsor;
using MvcContrib;

namespace Web.Infrastructure.IoC
{

    public static class WindsorExtensions
    {
        public static IWindsorContainer RegisterControllers(
            this IWindsorContainer container, params Assembly[] assemblies) {
            foreach (Assembly assembly in assemblies) {
                container.RegisterControllers(assembly.GetExportedTypes());
            }
            return container;
        }

        private static void RegisterControllers(
            this IWindsorContainer container, params Type[] controllerTypes) {
            foreach (Type type in controllerTypes) {
                if (ControllerExtensions.IsController(type)) {
                    container.AddComponentLifeStyle(type.FullName.ToLower(),
                        type, LifestyleType.Transient);
                }
            }
        }
    }

}