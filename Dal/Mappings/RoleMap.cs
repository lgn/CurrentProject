using Domain.Entities;
using FluentNHibernate.Mapping;

namespace Dal.Mappings
{
    public class RolesMap : ClassMap<Role>
    {
        public RolesMap()
        {
            Id(x => x.Id);
            Map(x => x.RoleName);
            Map(x => x.ApplicationName);
            HasManyToMany(x => x.UsersInRole)
            .Cascade.All()
            .Inverse()
            .Table("UsersInRoles");
        }
    }
}
