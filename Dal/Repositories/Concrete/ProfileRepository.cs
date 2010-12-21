using System;
using System.Collections.Generic;
using Dal.Repositories.Abstract;
using Domain.Entities;
using NHibernate.Criterion;

namespace Dal.Repositories.Concrete
{
    public class ProfileRepository : BaseRepository<Profile>, IProfiles
    {
        public virtual Profile GetByUserId(int userId) {
            return session.CreateCriteria<Profile>()
                .Add(Restrictions.Eq("Users_Id", userId))
                .UniqueResult<Profile>();
        }

        public virtual Profile Get(int userId, bool isAnonymous) {
            return session.CreateCriteria<Profile>()
                .Add(Restrictions.Eq("Users_Id", userId))
                .Add(Restrictions.Eq("IsAnonymous", isAnonymous))
                .UniqueResult<Profile>();
        }

        public virtual IList<Profile> GetInactiveProfiles(string appName, DateTime userInactiveSinceDate, bool isAnonymous) {
            return session.CreateCriteria<Profile>()
                .Add(Restrictions.Eq("ApplicationName",appName))
                .Add(Restrictions.Le("LastActivityDate", userInactiveSinceDate))
                .Add(Restrictions.Eq("IsAnonymous", isAnonymous))
                .List<Profile>();
        }
    }
}
