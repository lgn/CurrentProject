using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Security;
using System.Configuration.Provider;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using Dal.Repositories.Abstract;
using Domain.Entities;
using Web.Infrastructure.IoC;

namespace Web.Infrastructure.Membership
{
    public sealed class JsRoleProvider : RoleProvider
    {
        private IRoles roles;
        private IUsers users;

        #region private
        private const string eventSource = "JsRoleProvider";
        private const string eventLog = "Application";
        private const string exceptionMessage = "An exception occurred. Please check the Event Log.";
        private string _applicationName;

        #endregion

        #region Properties
        public override string ApplicationName {
            get { return _applicationName; }
            set { _applicationName = value; }
        }

        public bool WriteExceptionsToEventLog { get; set; }
        #endregion

        #region Helper Functions
        // A helper function to retrieve config values from the configuration file
        private static string GetConfigValue(string configValue, string defaultValue) {
            if (String.IsNullOrEmpty(configValue))
                return defaultValue;

            return configValue;
        }

        private static void WriteToEventLog(Exception e, string action) {
            var log = new EventLog {Source = eventSource, Log = eventLog};

            var message = exceptionMessage + "\n\n";
            message += "Action: " + action + "\n\n";
            message += "Exception: " + e;

            log.WriteEntry(message);
        }
        #endregion

        #region Private Methods
        //get a role by name
        private Role GetRole(string roleName) {
            Role role = null;
            try {
                role = roles.Get(ApplicationName, roleName);
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(ex, "GetRole");
                else {
                    throw;
                }
            }
            return role;
        }

        #endregion

        #region Public Methods

        public override void Initialize(string name, NameValueCollection config) {
            // Initialize values from web.config.

            if (config == null)
                throw new ArgumentNullException("config");

            if (name == null || name.Length == 0)
                name = "JsRoleProvider";

            if (String.IsNullOrEmpty(config["description"])) {
                config.Remove("description");
                config.Add("description", "Nhibernate Role provider");
            }

            // Initialize the abstract base class.
            base.Initialize(name, config);

            _applicationName = GetConfigValue(config["applicationName"], System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
            WriteExceptionsToEventLog = Convert.ToBoolean(GetConfigValue(config["writeExceptionsToEventLog"], "true"));

            users = WindsorBootStrapper.Container.Kernel.Resolve<IUsers>();
            roles = WindsorBootStrapper.Container.Kernel.Resolve<IRoles>();
        }
        //adds a user collection toa roles collection
        public override void AddUsersToRoles(string[] usernames, string[] rolenames) {
            User user = null;
            foreach (var role in rolenames.Where(role => !RoleExists(role))) {
                throw new ProviderException(String.Format("Role name {0} not found.", role));
            }

            foreach (var username in usernames) {
                if (username.Contains(","))
                    throw new ArgumentException(String.Format("User names {0} cannot contain commas.", username));
                //is user not exiting //throw exception

                foreach (var role in rolenames.Where(role => IsUserInRole(username, role))) {
                    throw new ProviderException(String.Format("User {0} is already in role {1}.", username, role));
                }
            }


            try {
                foreach (var username in usernames) {
                    foreach (var rolename in rolenames) {
                        //get the user
                        user = users.GetUserByUsername(ApplicationName, username);

                        if (user != null) {
                            //get the role first from db
                            var role = roles.Get(ApplicationName, rolename);
                            user.AddRole(role);
                        }
                    }
                    users.Update(user);
                }
                users.SaveChanges();
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(ex, "AddUsersToRoles");
                else
                    throw;
            }
        }
        //create  a new role with a given name
        public override void CreateRole(string rolename) {
            if (rolename.Contains(","))
                throw new ArgumentException("Role names cannot contain commas.");

            if (RoleExists(rolename))
                throw new ProviderException("Role name already exists.");


            try {
                var role = new Role { ApplicationName = ApplicationName, RoleName = rolename };
                roles.Update(role);
                roles.SaveChanges();
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(ex, "CreateRole");
                else
                    throw;
            }
        }
        //delete a role with given name
        public override bool DeleteRole(string rolename, bool throwOnPopulatedRole) {
            var deleted = false;
            if (!RoleExists(rolename))
                throw new ProviderException("Role does not exist.");

            if (throwOnPopulatedRole && GetUsersInRole(rolename).Length > 0)
                throw new ProviderException("Cannot delete a populated role.");
            try {
                var role = GetRole(rolename);
                roles.Delete(role);
                roles.SaveChanges();
                deleted = true;

            } catch (Exception ex){
                if (WriteExceptionsToEventLog) {
                    WriteToEventLog(ex, "DeleteRole");
                    return deleted;
                }
                throw;
            }
            return deleted;
        }
        //get an array of all the roles
        public override string[] GetAllRoles() {
            var sb = new StringBuilder();
            try {
                var allroles = roles.All(ApplicationName);
                foreach (var r in allroles) {
                    sb.Append(r.RoleName + ",");
                }
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(ex, "GetAllRoles");
                else
                    throw;
            }
            if (sb.Length > 0) {
                // Remove trailing comma.
                sb.Remove(sb.Length - 1, 1);
                return sb.ToString().Split(',');
            }
            return new string[0];
        }
        //Get roles for a user by username
        public override string[] GetRolesForUser(string username) {
            User user = null;
            var sb = new StringBuilder();

            try {
                user = users.GetUserByUsername(ApplicationName, username);
                if (user != null) {
                    var userRoles = user.Roles;
                    foreach (var r in userRoles) {
                        sb.Append(r.RoleName + ",");
                    }
                }
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(ex, "GetRolesForUser");
                else
                    throw;
            }

            if (sb.Length > 0) {
                // Remove trailing comma.
                sb.Remove(sb.Length - 1, 1);
                return sb.ToString().Split(',');
            }
            return new string[0];
        }
        //Get users in a givenrolename
        public override string[] GetUsersInRole(string rolename) {
            var sb = new StringBuilder();
            try {
                var role = roles.Get(ApplicationName, rolename);

                var userList = role.UsersInRole;

                foreach (var u in userList) {
                    sb.Append(u.Username + ",");
                }
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(ex, "GetUsersInRole");
                else
                    throw;
            }
            if (sb.Length > 0) {
                // Remove trailing comma.
                sb.Remove(sb.Length - 1, 1);
                return sb.ToString().Split(',');
            }
            return new string[0];
        }
        //determine is a user has a given role
        public override bool IsUserInRole(string username, string rolename) {
            var userIsInRole = false;
            User user = null;
            IList<Role> userRoles = null;
            var sb = new StringBuilder();
            try {
                user = users.GetUserByUsername(ApplicationName, username);
                if (user != null) {
                    userRoles = user.Roles;
                    userIsInRole = userRoles.Any(r => r.RoleName.Equals(rolename));
                }
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(ex, "IsUserInRole");
                else
                    throw;
            }
            return userIsInRole;
        }
        //remeove users from roles
        public override void RemoveUsersFromRoles(string[] usernames, string[] rolenames) {
            User user = null;
            foreach (var rolename in rolenames.Where(rolename => !RoleExists(rolename))) {
                throw new ProviderException(String.Format("Role name {0} not found.", rolename));
            }
            foreach (var username in usernames) {
                foreach (var rolename in rolenames.Where(rolename => !IsUserInRole(username, rolename))) {
                    throw new ProviderException(String.Format("User {0} is not in role {1}.", username, rolename));
                }
            }
            //get user , get his roles , the remove the role and save   
            try {
                foreach (var username in usernames) {
                    user = users.GetUserByUsername(ApplicationName, username);
                    var rolestodelete = new List<Role>();
                    foreach (var rolename in rolenames) {
                        var roleList = user.Roles;
                        rolestodelete.AddRange(roleList.Where(r => r.RoleName.Equals(rolename)));
                    }
                    foreach (var rd in rolestodelete)
                        user.RemoveRole(rd);
                    users.Update(user);
                }
                users.SaveChanges();
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(ex, "RemoveUsersFromRoles");
                else
                    throw;
            }
        }

        //boolen to check if a role exists given a role name
        public override bool RoleExists(string rolename) {
            var exists = false;
            try {
                var role = roles.Get(ApplicationName, rolename);
                if (role != null)
                    exists = true;

            } catch (Exception ex) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(ex, "RoleExists");
                else
                    throw;
            }
            return exists;
        }
        //find users that beloeng to a particular role , given a username, Note : does not do a LIke search
        public override string[] FindUsersInRole(string rolename, string usernameToMatch) {
            var sb = new StringBuilder();
            try {
                var role = roles.Get(ApplicationName, rolename);
                var userList = role.UsersInRole;
                if (userList != null) {
                    foreach (var u in userList.Where(u => String.Compare(u.Username, usernameToMatch, true) == 0)) {
                        sb.Append(u.Username + ",");
                    }
                }
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(ex, "FindUsersInRole");
                else
                    throw;
            }
            if (sb.Length > 0) {
                // Remove trailing comma.
                sb.Remove(sb.Length - 1, 1);
                return sb.ToString().Split(',');
            }
            return new string[0];
        }

        #endregion
    }
}
