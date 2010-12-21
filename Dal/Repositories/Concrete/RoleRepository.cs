using System.Collections.Generic;
using Dal.Repositories.Abstract;
using Domain.Entities;
using NHibernate.Criterion;

namespace Dal.Repositories.Concrete
{
    public class RoleRepository : BaseRepository<Role>, IRoles
    {
        public virtual IList<Role> All(string appName) {
            return session.CreateCriteria<Role>()
                .Add(Restrictions.Eq("ApplicationName", appName))
                .List<Role>();
        }

        public virtual Role Get(string appName, string roleName) {
            return session.CreateCriteria<Role>()
                .Add(Restrictions.Eq("RoleName", roleName))
                .Add(Restrictions.Eq("ApplicationName", appName))
                .UniqueResult<Role>();
        }
    }
}
