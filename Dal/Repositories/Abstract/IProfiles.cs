using System;
using System.Collections.Generic;
using Domain.Entities;

namespace Dal.Repositories.Abstract
{
    public interface IProfiles {
        Profile GetByUserId(int userId);
        Profile Get(int userId, bool isAnonymous);
        IList<Profile> GetInactiveProfiles(string appName, DateTime userInactiveSinceDate, bool isAnonymous);
        IList<Profile> All();
        Profile Get(object primaryKey);
        void Delete(Profile model);
        void Update(Profile model);
        void SaveChanges();
    }
}