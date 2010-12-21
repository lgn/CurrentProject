using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate;

namespace Dal.Repositories.Concrete
{
    public abstract class BaseRepository<T>
    {
        protected readonly ISession session;

        protected BaseRepository() {
            this.session = NHibernateConfig.CreateAndOpenSession();
        }

        protected BaseRepository(ISession session) {
            this.session = session;
        }

        public virtual IList<T> All() {
            return session.CreateCriteria(typeof(T)).List<T>();
        }

        public virtual T Get(object primaryKey) {
            return session.Get<T>(primaryKey);
        }

        public virtual void Delete(T model) {
            session.Delete(model);
        }

        public virtual void Update(T model) {
            session.SaveOrUpdate(model);
        }

        public virtual void SaveChanges() {
            session.Flush();
        }
    }
}
