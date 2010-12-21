using System;
using System.Web.Mvc;
using System.Web.Routing;
using Dal;
using Web.Infrastructure;
using Web.Infrastructure.IoC;

namespace Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters) {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes) {
            RouteDefinitions.AddRoutes(routes);
        }

        protected void Application_Start() {
            WindsorBootStrapper.Initialize();
            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
        }

        private void Application_BeginRequest(object sender, EventArgs e) {
            NHibernateSessionStorage.InitializeNHibernate();
        }

        private void Application_EndRequest(object sender, EventArgs e) {
            NHibernateSessionStorage.DisposeCurrent();
        }
    }
}