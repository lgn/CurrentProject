using System.Collections.Generic;

namespace Domain.Entities
{
    public class Role
    {
        public virtual int Id { get; private set; }
        public virtual string RoleName { get; set; }
        public virtual string ApplicationName { get; set; }
        public virtual IList<User> UsersInRole { get; set; }

        public Role()
        {
            UsersInRole = new List<User>();
        }
       
    }
}
