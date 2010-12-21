using System;
using System.Collections.Generic;
using Dal.Repositories.Abstract;
using Domain.Entities;
using NHibernate.Criterion;

namespace Dal.Repositories.Concrete
{
    public class UserRepository : BaseRepository<User>, IUsers
    {
        public virtual IList<User> All(string appName) {
            return session.CreateCriteria<User>()
                .Add(Restrictions.Eq("ApplicationName", appName))
                .List<User>();
        }

        public virtual User GetUserByEmail(string appName, string email) {
            return session.CreateCriteria<User>()
                .Add(Restrictions.Eq("Email", email))
                .UniqueResult<User>();
        }

        public virtual User GetUserByUsername(string appName, string username) {
            return session.CreateCriteria<User>()
                .Add(Restrictions.Eq("Username", username))
                .Add(Restrictions.Eq("ApplicationName", appName))
                .UniqueResult<User>();
        }

        public virtual IList<User> GetUsersByUsername(string appName, string username) {
            return session.CreateCriteria<User>()
                .Add(Restrictions.Like("Username", username))
                .Add(Restrictions.Eq("ApplicationName", appName))
                .List<User>();
        }

        public virtual IList<User> GetUsersByEmail(string appName, string email) {
            return session.CreateCriteria<User>()
                .Add(Restrictions.Like("Email", email))
                .Add(Restrictions.Eq("ApplicationName", appName))
                .List<User>();
        }

        public virtual int TotalRecords(string appName) {
            return (Int32)session.CreateCriteria<User>()
                .Add(Restrictions.Eq("ApplicationName", appName))
                .SetProjection(Projections.Count("Id")).UniqueResult();
        }

        public virtual int UsersOnline(string appName, DateTime compareTime) {
            return (Int32)session.CreateCriteria<User>()
                .Add(Restrictions.Eq("ApplicationName", appName))
                .Add(Restrictions.Gt("LastActivityDate", compareTime))
                .SetProjection(Projections.Count("Id")).UniqueResult();
        }
    }
}
