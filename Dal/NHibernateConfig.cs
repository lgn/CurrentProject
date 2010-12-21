using System;
using Dal.Mappings;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;

namespace Dal
{
    public class NHibernateConfig
    {
        public static ISessionFactory SessionFactory { get; private set; }

        public static void Init(IPersistenceConfigurer databaseConfig,
            Action<Configuration> schemaConfiguration) {
            SessionFactory = Fluently.Configure()
                .Database(databaseConfig)
                .Mappings(m => m.FluentMappings
                                   .AddFromAssemblyOf<ClassMappings>())
                .ExposeConfiguration(schemaConfiguration)
                .BuildSessionFactory();
        }

        public static ISession CreateAndOpenSession(){
            return SessionFactory.OpenSession();
        }
    }
}