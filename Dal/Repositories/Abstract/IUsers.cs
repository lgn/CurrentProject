using System;
using System.Collections.Generic;
using Domain.Entities;

namespace Dal.Repositories.Abstract
{
    public interface IUsers {
        IList<User> All(string appName);
        User GetUserByEmail(string appName, string email);
        User GetUserByUsername(string appName, string username);
        IList<User> GetUsersByUsername(string appName, string username);
        IList<User> GetUsersByEmail(string appName, string email);
        int TotalRecords(string appName);
        int UsersOnline(string appName, DateTime compareTime);
        IList<User> All();
        User Get(object primaryKey);
        void Delete(User model);
        void Update(User model);
        void SaveChanges();
    }
}