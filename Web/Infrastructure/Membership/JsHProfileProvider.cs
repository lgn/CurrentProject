using System;
using System.Linq;
using System.Web.Security;
using System.Configuration.Provider;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Web.Profile;
using Dal.Repositories.Abstract;
using Domain.Entities;
using Web.Infrastructure.IoC;

namespace Web.Infrastructure.Membership
{
    public sealed class JsProfileProvider : ProfileProvider
    {
        private IUsers users;
        private IProfiles profiles;

        #region private
        private const string eventSource = "JsProfileProvider";
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
        private string GetConfigValue(string configValue, string defaultValue) {
            if (String.IsNullOrEmpty(configValue))
                return defaultValue;

            return configValue;
        }

        private void WriteToEventLog(Exception e, string action) {
            EventLog log = new EventLog();
            log.Source = eventSource;
            log.Log = eventLog;

            string message = exceptionMessage + "\n\n";
            message += "Action: " + action + "\n\n";
            message += "Exception: " + e.ToString();

            log.WriteEntry(message);
        }
        #endregion

        #region Private Methods
        //get a role by name
        private Profile GetProfile(string username, bool isAuthenticated) {
            Profile profile = null;
            //Is authenticated and IsAnonmous are opposites,so flip sign,IsAuthenticated = true -> notAnonymous
            var isAnonymous = !isAuthenticated;
            try {
                var user = users.GetUserByUsername(ApplicationName, username);

                if (user != null) {
                    profile = profiles.Get(user.Id, isAnonymous);
                }
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(ex, "GetProfileWithIsAuthenticated");
                else
                    throw;
            }
            return profile;
        }

        private Profile GetProfile(string username) {
            Profile profile = null;
            try {
                var user = users.GetUserByUsername(ApplicationName, username);
                if (user != null) {
                    profile = profiles.GetByUserId(user.Id);
                }
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(ex, "GetProfile(username)");
                else
                    throw;
            }
            return profile;
        }

        private Profile GetProfile(int id) {
            Profile profile = null;
            try {
                var user = users.Get(id);

                if (user != null) {
                    profile = profiles.GetByUserId(user.Id);
                } else
                    throw new ProviderException("Membership User does not exist");
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(ex, "GetProfile(id)");
                else
                    throw;
            }
            return profile;
        }

        private Profile CreateProfile(string username, bool isAnonymous) {
            var profile = new Profile();
            var profileCreated = false;
            try {
                var user = users.GetUserByUsername(ApplicationName, username);
                if (user != null) {
                    profile.Users_Id = user.Id;
                    profile.IsAnonymous = isAnonymous;
                    profile.LastUpdatedDate = DateTime.Now;
                    profile.LastActivityDate = DateTime.Now;
                    profile.ApplicationName = ApplicationName;
                    profiles.Update(profile);
                    profiles.SaveChanges();
                    profileCreated = true;
                } else
                    throw new ProviderException("Membership User does not exist.Profile cannot be created.");
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(ex, "GetProfile");
                else
                    throw;
            }
            return profileCreated ? profile : null;
        }

        private bool IsMembershipUser(string username) {
            var hasMembership = false;
            try {
                var user = users.GetUserByUsername(ApplicationName, username);
                if (user != null) //membership user exits so create a profile
                    hasMembership = true;
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(ex, "GetProfile");
                else
                    throw;
            }
            return hasMembership;
        }

        private bool IsUserInCollection(MembershipUserCollection uc, string username) {
            var isInColl = false;
            foreach (var u in from MembershipUser u in uc where u.UserName.Equals(username) select u) {
                isInColl = true;
            }
            return isInColl;
        }

        private void UpdateActivityDates(string username, bool isAuthenticated, bool activityOnly) {
            //Is authenticated and IsAnonmous are opposites,so flip sign,IsAuthenticated = true -> notAnonymous
            var isAnonymous = !isAuthenticated;
            var activityDate = DateTime.Now;

            var profile = GetProfile(username, isAuthenticated);
            if (profile == null)
                throw new ProviderException("User Profile not found");
            try {
                if (activityOnly) {
                    profile.LastActivityDate = activityDate;
                    profile.IsAnonymous = isAnonymous;
                } else {
                    profile.LastActivityDate = activityDate;
                    profile.LastUpdatedDate = activityDate;
                    profile.IsAnonymous = isAnonymous;
                }
                profiles.Update(profile);
                profiles.SaveChanges();
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog) {
                    WriteToEventLog(ex, "UpdateActivityDates");
                    throw new ProviderException(exceptionMessage);
                }
                throw;
            }
        }

        private bool DeleteProfile(string username) {
            // Check for valid user name.
            if (username == null)
                throw new ArgumentNullException("User name cannot be null.");
            if (username.Contains(","))
                throw new ArgumentException("User name cannot contain a comma (,).");

            var profile = GetProfile(username);
            if (profile == null)
                return false;

            try {
                profiles.Delete(profile);
                profiles.SaveChanges();
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog) {
                    WriteToEventLog(ex, "DeleteProfile");
                    throw new ProviderException(exceptionMessage);
                }
                throw;
            }

            return true;
        }

        private bool DeleteProfile(int id) {
            // Check for valid user name.
            var profile = GetProfile(id);
            if (profile == null)
                return false;
            try {
                profiles.Delete(profile);
                profiles.SaveChanges();
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog) {
                    WriteToEventLog(ex, "DeleteProfile(id)");
                    throw new ProviderException(exceptionMessage);
                }
                throw;
            }
            return true;
        }

        private int DeleteProfilesbyId(string[] ids) {
            var deleteCount = 0;
            try {
                deleteCount = ids.Count(id => DeleteProfile(id));
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog) {
                    WriteToEventLog(ex, "DeleteProfiles(Id())");
                    throw new ProviderException(exceptionMessage);
                }
                throw;
            }
            return deleteCount;
        }

        private void CheckParameters(int pageIndex, int pageSize) {
            if (pageIndex < 0)
                throw new ArgumentException("Page index must 0 or greater.");
            if (pageSize < 1)
                throw new ArgumentException("Page size must be greater than 0.");
        }

        private ProfileInfo GetProfileInfoFromProfile(Profile p) {
            var user = users.Get(p.Users_Id);
            if (user == null)
                throw new ProviderException("The userid not found in memebership tables.GetProfileInfoFromProfile(p)");

            // ProfileInfo.Size not currently implemented.
            var pi = new ProfileInfo(user.Username,
                p.IsAnonymous, p.LastActivityDate, p.LastUpdatedDate, 0);
            return pi;
        }
        #endregion

        #region Public Methods
        public override void Initialize(string name, NameValueCollection config) {
            // Initialize values from web.config.
            if (config == null)
                throw new ArgumentNullException("config");

            if (name == null || name.Length == 0)
                name = "JsProfileProvider";

            if (String.IsNullOrEmpty(config["description"])) {
                config.Remove("description");
                config.Add("description", "Nhibernate Profile provider");
            }

            // Initialize the abstract base class.
            base.Initialize(name, config);

            _applicationName = GetConfigValue(config["applicationName"], System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
            WriteExceptionsToEventLog = Convert.ToBoolean(GetConfigValue(config["writeExceptionsToEventLog"], "true"));
            users = WindsorBootStrapper.Container.Kernel.Resolve<IUsers>();
            profiles = WindsorBootStrapper.Container.Kernel.Resolve<IProfiles>();
        }

        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection ppc) {
            var username = (string)context["UserName"];
            var isAuthenticated = (bool)context["IsAuthenticated"];

            var profile = GetProfile(username, isAuthenticated);
            // The serializeAs attribute is ignored in this provider implementation.
            var svc = new SettingsPropertyValueCollection();

            if (profile == null) {
                if (IsMembershipUser(username))
                    profile = CreateProfile(username, false);
                else
                    throw new ProviderException("Profile cannot be created. There is no membership user");
            }

            foreach (SettingsProperty prop in ppc) {
                var pv = new SettingsPropertyValue(prop);
                switch (prop.Name) {
                    case "IsAnonymous":
                        pv.PropertyValue = profile.IsAnonymous;
                        break;
                    case "LastActivityDate":
                        pv.PropertyValue = profile.LastActivityDate;
                        break;
                    case "LastUpdatedDate":
                        pv.PropertyValue = profile.LastUpdatedDate;
                        break;
                    case "Subscription":
                        pv.PropertyValue = profile.Subscription;
                        break;
                    case "Language":
                        pv.PropertyValue = profile.Language;
                        break;
                    case "FirstName":
                        pv.PropertyValue = profile.FirstName;
                        break;
                    case "LastName":
                        pv.PropertyValue = profile.LastName;
                        break;
                    case "Gender":
                        pv.PropertyValue = profile.Gender;
                        break;
                    case "BirthDate":
                        pv.PropertyValue = profile.BirthDate;
                        break;
                    case "Occupation":
                        pv.PropertyValue = profile.Occupation;
                        break;
                    case "Website":
                        pv.PropertyValue = profile.Website;
                        break;
                    case "Street":
                        pv.PropertyValue = profile.Street;
                        break;
                    case "City":
                        pv.PropertyValue = profile.City;
                        break;
                    case "State":
                        pv.PropertyValue = profile.State;
                        break;
                    case "Zip":
                        pv.PropertyValue = profile.Zip;
                        break;
                    case "Country":
                        pv.PropertyValue = profile.Country;
                        break;
                    default:
                        throw new ProviderException("Unsupported property.");
                }
                svc.Add(pv);
            }
            UpdateActivityDates(username, isAuthenticated, true);
            return svc;
        }

        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection ppvc) {
            Profile profile = null;
            // The serializeAs attribute is ignored in this provider implementation.
            var username = (string)context["UserName"];
            var isAuthenticated = (bool)context["IsAuthenticated"];

            profile = GetProfile(username, isAuthenticated) ?? CreateProfile(username, !isAuthenticated);

            foreach (SettingsPropertyValue pv in ppvc) {
                switch (pv.Property.Name) {
                    case "IsAnonymous":
                        profile.IsAnonymous = (bool)pv.PropertyValue;
                        break;
                    case "LastActivityDate":
                        profile.LastActivityDate = (DateTime)pv.PropertyValue;
                        break;
                    case "LastUpdatedDate":
                        profile.LastUpdatedDate = (DateTime)pv.PropertyValue;
                        break;
                    case "Subscription":
                        profile.Subscription = pv.PropertyValue.ToString();
                        break;
                    case "Language":
                        profile.Language = pv.PropertyValue.ToString();
                        break;
                    case "FirstName":
                        profile.FirstName = pv.PropertyValue.ToString();
                        break;
                    case "LastName":
                        profile.LastName = pv.PropertyValue.ToString();
                        break;
                    case "Gender":
                        profile.Gender = pv.PropertyValue.ToString();
                        break;
                    case "BirthDate":
                        profile.BirthDate = (DateTime)pv.PropertyValue;
                        break;
                    case "Occupation":
                        profile.Occupation = pv.PropertyValue.ToString();
                        break;
                    case "Website":
                        profile.Website = pv.PropertyValue.ToString();
                        break;
                    case "Street":
                        profile.Street = pv.PropertyValue.ToString();
                        break;
                    case "City":
                        profile.City = pv.PropertyValue.ToString();
                        break;
                    case "State":
                        profile.State = pv.PropertyValue.ToString();
                        break;
                    case "Zip":
                        profile.Zip = pv.PropertyValue.ToString();
                        break;
                    case "Country":
                        profile.Country = pv.PropertyValue.ToString();
                        break;
                    default:
                        throw new ProviderException("Unsupported property.");
                }
            }
            profiles.Update(profile);
            profiles.SaveChanges();
            UpdateActivityDates(username, isAuthenticated, false);
        }

        public override int DeleteProfiles(ProfileInfoCollection profiles) {
            var deleteCount = 0;
            try {
                deleteCount = profiles.Cast<ProfileInfo>().Count(p => DeleteProfile(p.UserName));
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog) {
                    WriteToEventLog(ex, "DeleteProfiles(ProfileInfoCollection)");
                    throw new ProviderException(exceptionMessage);
                }
                throw;
            }
            return deleteCount;
        }

        public override int DeleteProfiles(string[] usernames) {
            var deleteCount = 0;
            try {
                deleteCount = usernames.Count(user => DeleteProfile(user));
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog) {
                    WriteToEventLog(ex, "DeleteProfiles(String())");
                    throw new ProviderException(exceptionMessage);
                }
                throw;

            }
            return deleteCount;
        }

        public override int DeleteInactiveProfiles(ProfileAuthenticationOption authenticationOption, DateTime userInactiveSinceDate) {
            var userIds = "";
            var anon = false;
            switch (authenticationOption) {
                case ProfileAuthenticationOption.Anonymous:
                    anon = true;
                    break;
                case ProfileAuthenticationOption.Authenticated:
                    anon = false;
                    break;
                default:
                    break;
            }
            try {
                var profs = profiles.GetInactiveProfiles(ApplicationName, userInactiveSinceDate, anon);

                if (profs != null) {
                    userIds = profs.Aggregate(userIds, (current, p) => current + (p.Id.ToString() + ","));
                }
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(ex, "DeleteInactiveProfiles");
                else
                    throw;
            }
            if (userIds.Length > 0)
                userIds = userIds.Substring(0, userIds.Length - 1);
            return DeleteProfilesbyId(userIds.Split(','));
        }

        public override ProfileInfoCollection FindProfilesByUserName(ProfileAuthenticationOption authenticationOption, string usernameToMatch,
                                                                       int pageIndex,
                                                                       int pageSize,
                                                                       out int totalRecords) {
            CheckParameters(pageIndex, pageSize);

            return GetProfileInfo(authenticationOption, usernameToMatch, null, pageIndex, pageSize, out totalRecords);
        }

        public override ProfileInfoCollection FindInactiveProfilesByUserName(ProfileAuthenticationOption authenticationOption, string usernameToMatch,
                                                                              DateTime userInactiveSinceDate,
                                                                              int pageIndex,
                                                                              int pageSize,
                                                                              out int totalRecords) {
            CheckParameters(pageIndex, pageSize);

            return GetProfileInfo(authenticationOption, usernameToMatch, userInactiveSinceDate, pageIndex, pageSize, out totalRecords);
        }

        public override ProfileInfoCollection GetAllProfiles(ProfileAuthenticationOption authenticationOption, int pageIndex,
                                                                              int pageSize,
                                                                              out int totalRecords) {
            CheckParameters(pageIndex, pageSize);

            return GetProfileInfo(authenticationOption, null, null, pageIndex, pageSize, out totalRecords);
        }

        public override ProfileInfoCollection GetAllInactiveProfiles(ProfileAuthenticationOption authenticationOption, DateTime userInactiveSinceDate,
                                                                          int pageIndex,
                                                                          int pageSize,
                                                                          out int totalRecords) {
            CheckParameters(pageIndex, pageSize);

            return GetProfileInfo(authenticationOption, null, userInactiveSinceDate, pageIndex, pageSize, out totalRecords);
        }

        public override int GetNumberOfInactiveProfiles(ProfileAuthenticationOption authenticationOption, DateTime userInactiveSinceDate) {
            var inactiveProfiles = 0;

            var profiles =
              GetProfileInfo(authenticationOption, null, userInactiveSinceDate, 0, 0, out inactiveProfiles);

            return inactiveProfiles;
        }

        private ProfileInfoCollection GetProfileInfo(ProfileAuthenticationOption authenticationOption, string usernameToMatch,
                                                                      object userInactiveSinceDate,
                                                                      int pageIndex,
                                                                      int pageSize,
                                                                      out int totalRecords) {

            //var isAnaon = false;
            //var profilesInfoColl = new ProfileInfoCollection();
            //switch (authenticationOption) {
            //    case ProfileAuthenticationOption.Anonymous:
            //        isAnaon = true;
            //        break;
            //    case ProfileAuthenticationOption.Authenticated:
            //        isAnaon = false;
            //        break;
            //    default:
            //        break;
            //}

            //try {
            //    ICriteria cprofiles = session.CreateCriteria(typeof(Entities.Profiles));
            //    cprofiles.Add(NHibernate.Criterion.Restrictions.Eq("ApplicationName", this.ApplicationName));



            //    if (userInactiveSinceDate != null)
            //        cprofiles.Add(NHibernate.Criterion.Restrictions.Le("LastActivityDate", (DateTime)userInactiveSinceDate));

            //    cprofiles.Add(NHibernate.Criterion.Restrictions.Eq("IsAnonymous", isAnaon));


            //    IList<Entities.Profiles> profiles = cprofiles.List<Entities.Profiles>();
            //    IList<Entities.Profiles> profiles2 = null;

            //    if (profiles == null)
            //        totalRecords = 0;
            //    else if (profiles.Count < 1)
            //        totalRecords = 0;
            //    else
            //        totalRecords = profiles.Count;



            //    //IF USER NAME TO MATCH then fileter out those
            //    //Membership.FNHMembershipProvider us = new INCT.FNHProviders.Membership.FNHMembershipProvider();
            //    //us.g
            //    System.Web.Security.MembershipUserCollection uc = System.Web.Security.Membership.FindUsersByName(usernameToMatch);

            //    if (usernameToMatch != null) {
            //        if (totalRecords > 0) {
            //            foreach (Entities.Profiles p in profiles) {
            //                if (IsUserInCollection(uc, usernameToMatch))
            //                    profiles2.Add(p);
            //            }

            //            if (profiles2 == null)
            //                profiles2 = profiles;
            //            else if (profiles2.Count < 1)
            //                profiles2 = profiles;
            //            else
            //                totalRecords = profiles2.Count;
            //        } else
            //            profiles2 = profiles;
            //    } else
            //        profiles2 = profiles;




            //    if (totalRecords <= 0)
            //        return profilesInfoColl;

            //    if (pageSize == 0)
            //        return profilesInfoColl;

            //    int counter = 0;
            //    int startIndex = pageSize * (pageIndex - 1);
            //    int endIndex = startIndex + pageSize - 1;

            //    foreach (Entities.Profiles p in profiles2) {
            //        if (counter >= endIndex)
            //            break;
            //        if (counter >= startIndex) {
            //            ProfileInfo pi = GetProfileInfoFromProfile(p);
            //            profilesInfoColl.Add(pi);
            //        }
            //        counter++;
            //    }
            //} catch (Exception e) {
            //    if (WriteExceptionsToEventLog) {
            //        WriteToEventLog(e, "GetProfileInfo");
            //        throw new ProviderException(exceptionMessage);
            //    } else
            //        throw e;

            //}
            //return profilesInfoColl;
            throw new NotImplementedException();
        }


        #endregion
    }
}
