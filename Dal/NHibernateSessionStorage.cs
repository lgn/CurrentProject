using System;
using System.Web;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;

namespace Dal
{
    public class NHibernateSessionStorage
    {
        private const string CURRENT_SESSION_KEY = "nhibernate.current_session";
        private static object lockObject = new object();
        private static bool wasNHibernateInitialized;

        public static ISession RetrieveSession() {
            var context = HttpContext.Current;
            if (!context.Items.Contains(CURRENT_SESSION_KEY))
                OpenCurrent();
            var session = context.Items[CURRENT_SESSION_KEY] as ISession;
            return session;
        }

        private static void OpenCurrent() {
            var session = NHibernateConfig.CreateAndOpenSession();
            var context = HttpContext.Current;
            context.Items[CURRENT_SESSION_KEY] = session;
        }

        public static void DisposeCurrent() {
            if (!HttpContext.Current.Items.Contains(CURRENT_SESSION_KEY))
                return;
            var session = RetrieveSession();
            if (session != null && session.IsOpen)
                session.Close();
            var context = HttpContext.Current;
            context.Items.Remove(CURRENT_SESSION_KEY);
        }

        public static ITransaction Transaction {
            get { return RetrieveSession().Transaction; }
        }

        public static void InitializeNHibernate() {
            if (!wasNHibernateInitialized) {
                lock (lockObject) {
                    if (!wasNHibernateInitialized) {
                        NHibernateConfig.Init(
                            MsSqlConfiguration.MsSql2008
                            .ConnectionString(builder => builder
                            .FromConnectionStringWithKey("DevDb")),
                            UpdateDatabase());
                        wasNHibernateInitialized = true;
                    }
                }
            }
        }

        private static Action<Configuration> UpdateDatabase() {
            return config => new SchemaUpdate(config).Execute(false, true);
        }
    }
}