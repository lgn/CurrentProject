using System.Collections.Generic;
using Domain.Entities;

namespace Dal.Repositories.Abstract
{
    public interface IRoles {
        IList<Role> All(string appName);
        Role Get(string appName, string roleName);
        IList<Role> All();
        Role Get(object primaryKey);
        void Delete(Role model);
        void Update(Role model);
        void SaveChanges();
    }
}