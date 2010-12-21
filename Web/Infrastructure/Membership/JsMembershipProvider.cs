using System;
using System.Collections.Generic;
using System.Web.Security;
using System.Configuration.Provider;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Web.Configuration;
using Dal.Repositories.Abstract;
using Domain.Entities;
using Web.Infrastructure.IoC;

namespace Web.Infrastructure.Membership
{
    public sealed class JsMembershipProvider : MembershipProvider
    {
        private IUsers users;

        #region Private
        private const int newPasswordLength = 8;
        private const string eventSource = "JsMembershipProvider";
        private const string eventLog = "Application";
        private const string exceptionMessage = "An exception occurred. Please check the Event Log.";
        private string _applicationName;
        private bool _enablePasswordReset;
        private bool _enablePasswordRetrieval;
        private bool _requiresQuestionAndAnswer;
        private bool _requiresUniqueEmail;
        private int _maxInvalidPasswordAttempts;
        private int _passwordAttemptWindow;
        private MembershipPasswordFormat _passwordFormat;
        // Used when determining encryption key values.
        private MachineKeySection _machineKey;
        private int _minRequiredNonAlphanumericCharacters;
        private int _minRequiredPasswordLength;
        private string _passwordStrengthRegularExpression;
        #endregion

        #region Public Properties
        public override string ApplicationName {
            get { return _applicationName; }
            set { _applicationName = value; }
        }

        public override bool EnablePasswordReset {
            get { return _enablePasswordReset; }
        }


        public override bool EnablePasswordRetrieval {
            get { return _enablePasswordRetrieval; }
        }


        public override bool RequiresQuestionAndAnswer {
            get { return _requiresQuestionAndAnswer; }
        }


        public override bool RequiresUniqueEmail {
            get { return _requiresUniqueEmail; }
        }


        public override int MaxInvalidPasswordAttempts {
            get { return _maxInvalidPasswordAttempts; }
        }


        public override int PasswordAttemptWindow {
            get { return _passwordAttemptWindow; }
        }


        public override MembershipPasswordFormat PasswordFormat {
            get { return _passwordFormat; }
        }


        public override int MinRequiredNonAlphanumericCharacters {
            get { return _minRequiredNonAlphanumericCharacters; }
        }

        public override int MinRequiredPasswordLength {
            get { return _minRequiredPasswordLength; }
        }

        public override string PasswordStrengthRegularExpression {
            get { return _passwordStrengthRegularExpression; }
        }

        // If false, exceptions are thrown to the caller. If true,
        // exceptions are written to the event log.
        public bool WriteExceptionsToEventLog { get; set; }

        #endregion

        #region Helper functions

        //Fn to create a Membership user from a Entities.Users class
        private MembershipUser GetMembershipUser(User user) {
            return new MembershipUser(Name,
                                      user.Username,
                                      user.Id,
                                      user.Email,
                                      user.PasswordQuestion,
                                      user.Comment,
                                      user.IsApproved,
                                      user.IsLockedOut,
                                      user.CreationDate,
                                      user.LastLoginDate,
                                      user.LastActivityDate,
                                      user.LastPasswordChangedDate,
                                      user.LastLockedOutDate);

        }

        //Fn that performs the checks and updates associated with password failure tracking
        private void UpdateFailureCount(string username, string failureType) {
            var windowStart = new DateTime();
            var failureCount = 0;
            User user = null;
            try {
                user = GetUserByUsername(username);

                if (user != null) {
                    if (failureType == "password") {
                        failureCount = user.FailedPasswordAttemptCount;
                        windowStart = user.FailedPasswordAttemptWindowStart;
                    }

                    if (failureType == "passwordAnswer") {
                        failureCount = user.FailedPasswordAnswerAttemptCount;
                        windowStart = user.FailedPasswordAnswerAttemptWindowStart;
                    }

                    var windowEnd = windowStart.AddMinutes(PasswordAttemptWindow);

                    if (failureCount == 0 || DateTime.Now > windowEnd) {
                        // First password failure or outside of PasswordAttemptWindow. 
                        // Start a new password failure count from 1 and a new window starting now.

                        if (failureType == "password") {
                            user.FailedPasswordAttemptCount = 1;
                            user.FailedPasswordAttemptWindowStart = DateTime.Now;
                            ;
                        }

                        if (failureType == "passwordAnswer") {
                            user.FailedPasswordAnswerAttemptCount = 1;
                            user.FailedPasswordAnswerAttemptWindowStart = DateTime.Now;
                            ;
                        }
                        users.Update(user);
                        users.SaveChanges();
                    } else {
                        if (failureCount++ >= MaxInvalidPasswordAttempts) {
                            // Password attempts have exceeded the failure threshold. Lock out
                            // the user.
                            user.IsLockedOut = true;
                            user.LastLockedOutDate = DateTime.Now;
                            users.Update(user);
                            users.SaveChanges();
                        } else {
                            // Password attempts have not exceeded the failure threshold. Update
                            // the failure counts. Leave the window the same.
                            if (failureType == "password")
                                user.FailedPasswordAttemptCount = failureCount;

                            if (failureType == "passwordAnswer")
                                user.FailedPasswordAnswerAttemptCount = failureCount;

                            users.Update(user);
                            users.SaveChanges();
                        }
                    }
                }
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog) {
                    WriteToEventLog(ex, "UpdateFailureCount");
                    throw new ProviderException("Unable to update failure count and window start." + exceptionMessage);
                }
                throw;
            }

        }

        #endregion

        #region Private Methods

        //single fn to get a membership user by key or username
        private MembershipUser GetMembershipUserByKeyOrUser(bool isKeySupplied, string username, object providerUserKey, bool userIsOnline) {
            User user;
            MembershipUser membershipUser = null;
            try {
                user = isKeySupplied ? users.Get(providerUserKey) : GetUserByUsername(username);
                if (user != null) {
                    membershipUser = GetMembershipUser(user);

                    if (userIsOnline) {
                        user.LastActivityDate = DateTime.Now;
                        users.Update(user);
                        users.SaveChanges();
                    }
                }
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(ex, "GetUser(Object, Boolean)");
                throw new ProviderException(exceptionMessage);
            }
            return membershipUser;
        }

        private User GetUserByUsername(string username) {
            User user;
            try {
                user = users.GetUserByUsername(ApplicationName, username);
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(ex, "UnlockUser");
                throw new ProviderException(exceptionMessage);
            }
            return user;
        }

        private IList<User> GetUsers() {
            IList<User> userList;
            try {
                userList = users.All(ApplicationName);

            } catch (Exception ex) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(ex, "GetUsers");
                throw new ProviderException(exceptionMessage);
            }
            return userList;
        }

        private IList<User> GetUsersByUsername(string username) {
            IList<User> userList = null;
            try {
                userList = users.GetUsersByUsername(ApplicationName, username);

            } catch (Exception ex) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(ex, "GetUsersMatchByUsername");
                throw new ProviderException(exceptionMessage);
            }
            return userList;
        }

        private IList<User> GetUsersByEmail(string email) {
            IList<User> userList = null;
            try {
                userList = users.GetUsersByEmail(ApplicationName, email);

            } catch (Exception ex) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(ex, "GetUsersMatchByEmail");
                throw new ProviderException(exceptionMessage);
            }
            return userList;
        }
        #endregion

        #region Public methods

        // Initilaize the provider 
        public override void Initialize(string name, NameValueCollection config) {
            // Initialize values from web.config.
            if (config == null)
                throw new ArgumentNullException("config");

            if (name == null || name.Length == 0)
                name = "JsMemebershipProvider";

            if (String.IsNullOrEmpty(config["description"])) {
                config.Remove("description");
                config.Add("description", "Nhibernate Membership provider");
            }
            // Initialize the abstract base class.
            base.Initialize(name, config);

            _applicationName = GetConfigValue(config["applicationName"], System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
            _maxInvalidPasswordAttempts = Convert.ToInt32(GetConfigValue(config["maxInvalidPasswordAttempts"], "5"));
            _passwordAttemptWindow = Convert.ToInt32(GetConfigValue(config["passwordAttemptWindow"], "10"));
            _minRequiredNonAlphanumericCharacters = Convert.ToInt32(GetConfigValue(config["minRequiredNonAlphanumericCharacters"], "1"));
            _minRequiredPasswordLength = Convert.ToInt32(GetConfigValue(config["minRequiredPasswordLength"], "7"));
            _passwordStrengthRegularExpression = Convert.ToString(GetConfigValue(config["passwordStrengthRegularExpression"], ""));
            _enablePasswordReset = Convert.ToBoolean(GetConfigValue(config["enablePasswordReset"], "true"));
            _enablePasswordRetrieval = Convert.ToBoolean(GetConfigValue(config["enablePasswordRetrieval"], "true"));
            _requiresQuestionAndAnswer = Convert.ToBoolean(GetConfigValue(config["requiresQuestionAndAnswer"], "false"));
            _requiresUniqueEmail = Convert.ToBoolean(GetConfigValue(config["requiresUniqueEmail"], "true"));
            WriteExceptionsToEventLog = Convert.ToBoolean(GetConfigValue(config["writeExceptionsToEventLog"], "true"));


            var temp_format = config["passwordFormat"] ?? "Hashed";

            switch (temp_format) {
                case "Hashed":
                    _passwordFormat = MembershipPasswordFormat.Hashed;
                    break;
                case "Encrypted":
                    _passwordFormat = MembershipPasswordFormat.Encrypted;
                    break;
                case "Clear":
                    _passwordFormat = MembershipPasswordFormat.Clear;
                    break;
                default:
                    throw new ProviderException("Password format not supported.");
            }


            //Encryption skipped
            Configuration cfg =
                            WebConfigurationManager.OpenWebConfiguration(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
            _machineKey = (MachineKeySection)cfg.GetSection("system.web/machineKey");

            if (_machineKey.ValidationKey.Contains("AutoGenerate"))
                if (PasswordFormat != MembershipPasswordFormat.Clear)
                    throw new ProviderException("Hashed or Encrypted passwords are not supported with auto-generated keys.");
            users = WindsorBootStrapper.Container.Kernel.Resolve<IUsers>();
        }

        // Change password for a user
        public override bool ChangePassword(string username, string oldPwd, string newPwd) {
            User user;

            if (!ValidateUser(username, oldPwd))
                return false;

            var args = new ValidatePasswordEventArgs(username, newPwd, true);

            OnValidatingPassword(args);

            if (args.Cancel)
                if (args.FailureInformation != null)
                    throw args.FailureInformation;
                else
                    throw new MembershipPasswordException("Change password canceled due to new password validation failure.");

            try {
                user = GetUserByUsername(username);

                if (user != null) {
                    user.Password = EncodePassword(newPwd);
                    user.LastPasswordChangedDate = DateTime.Now;
                    users.Update(user);
                    users.SaveChanges();
                    return true;
                }
            } catch (Exception e) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(e, "ChangePassword");
                throw new ProviderException(exceptionMessage);

            }
            return false;
        }

        // Change Password Question And Answer for a user
        public override bool ChangePasswordQuestionAndAnswer(string username,
                      string password,
                      string newPwdQuestion,
                      string newPwdAnswer) {
            User user;
            if (!ValidateUser(username, password))
                return false;

            try {
                user = GetUserByUsername(username);
                if (user != null) {
                    user.PasswordQuestion = newPwdQuestion;
                    user.PasswordAnswer = newPwdAnswer;
                    users.Update(user);
                    users.SaveChanges();
                    return true;
                }
            } catch (Exception e) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(e, "ChangePasswordQuestionAndAnswer");
                throw new ProviderException(exceptionMessage);
            }
            return false;
        }

        // Create a new Membership user 
        public override MembershipUser CreateUser(string username,
                 string password,
                 string email,
                 string passwordQuestion,
                 string passwordAnswer,
                 bool isApproved,
                 object providerUserKey,
                 out MembershipCreateStatus status) {
            var args = new ValidatePasswordEventArgs(username, password, true);

            OnValidatingPassword(args);
            if (args.Cancel) {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }

            if (RequiresUniqueEmail && GetUserNameByEmail(email) != "") {
                status = MembershipCreateStatus.DuplicateEmail;
                return null;
            }

            var u = GetUser(username, false);

            if (u == null) {
                var createDate = DateTime.Now;
                var user = new User() {
                    Username = username,
                    Password = EncodePassword(password),
                    Email = email,
                    PasswordQuestion = passwordQuestion,
                    PasswordAnswer = EncodePassword(passwordAnswer),
                    IsApproved = isApproved,
                    Comment = "",
                    CreationDate = createDate,
                    LastPasswordChangedDate = createDate,
                    LastActivityDate = createDate,
                    ApplicationName = _applicationName,
                    IsLockedOut = false,
                    LastLockedOutDate = createDate,
                    FailedPasswordAttemptCount = 0,
                    FailedPasswordAttemptWindowStart = createDate,
                    FailedPasswordAnswerAttemptCount = 0,
                    FailedPasswordAnswerAttemptWindowStart = createDate
                };

                try {
                    users.Update(user);
                    users.SaveChanges();
                    status = MembershipCreateStatus.Success;

                    // Note: not sure if I need this
                    // status = MembershipCreateStatus.UserRejected;
                } catch (Exception e) {
                    status = MembershipCreateStatus.ProviderError;
                    if (WriteExceptionsToEventLog)
                        WriteToEventLog(e, "CreateUser");
                }
                return GetUser(username, false);
            } else
                status = MembershipCreateStatus.DuplicateUserName;
            return null;
        }

        // Delete a user 
        public override bool DeleteUser(string username, bool deleteAllRelatedData) {
            try {
                var user = GetUserByUsername(username);
                if (user != null) {
                    users.Delete(user);
                    users.SaveChanges();
                    return true;
                }
            } catch (Exception e) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(e, "DeleteUser");
                throw new ProviderException(exceptionMessage);
            }
            return false;
        }

        // Get all users in db
        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords) {
            var userList = new MembershipUserCollection();
            totalRecords = 0;
            IList<User> allusers = null;
            var counter = 0;
            var startIndex = pageSize * pageIndex;
            var endIndex = startIndex + pageSize - 1;

            try {
                totalRecords = users.TotalRecords(ApplicationName);

                if (totalRecords <= 0) { return userList; }

                allusers = GetUsers();
                foreach (var user in allusers) {
                    if (counter >= endIndex)
                        break;
                    if (counter >= startIndex) {
                        var mu = GetMembershipUser(user);
                        userList.Add(mu);
                    }
                    counter++;
                }
            } catch (Exception e) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(e, "GetAllUsers");
                throw new ProviderException(exceptionMessage);
            }
            return userList;
        }

        // Gets a number of online users
        public override int GetNumberOfUsersOnline() {
            var onlineSpan = new TimeSpan(0, System.Web.Security.Membership.UserIsOnlineTimeWindow, 0);
            var compareTime = DateTime.Now.Subtract(onlineSpan);
            int numOnline;
            try {
                numOnline = users.UsersOnline(ApplicationName, compareTime);
            } catch (Exception e) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(e, "GetNumberOfUsersOnline");
                throw new ProviderException(exceptionMessage);
            }
            return numOnline;
        }

        // Get a password fo a user
        public override string GetPassword(string username, string answer) {
            string password;
            string passwordAnswer;

            if (!EnablePasswordRetrieval)
                throw new ProviderException("Password Retrieval Not Enabled.");

            if (PasswordFormat == MembershipPasswordFormat.Hashed)
                throw new ProviderException("Cannot retrieve Hashed passwords.");

            try {
                var user = GetUserByUsername(username);

                if (user == null)
                    throw new MembershipPasswordException("The supplied user name is not found.");

                if (user.IsLockedOut)
                    throw new MembershipPasswordException("The supplied user is locked out.");

                password = user.Password;
                passwordAnswer = user.PasswordAnswer;

            } catch (Exception e) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(e, "GetPassword");
                throw new ProviderException(exceptionMessage);
            }

            if (RequiresQuestionAndAnswer && !CheckPassword(answer, passwordAnswer)) {
                UpdateFailureCount(username, "passwordAnswer");
                throw new MembershipPasswordException("Incorrect password answer.");
            }

            if (PasswordFormat == MembershipPasswordFormat.Encrypted)
                password = UnEncodePassword(password);
            return password;
        }

        // Get a membership user by username
        public override MembershipUser GetUser(string username, bool userIsOnline) {
            return GetMembershipUserByKeyOrUser(false, username, 0, userIsOnline);
        }

        //  // Get a membership user by key ( in our case key is int)
        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline) {
            return GetMembershipUserByKeyOrUser(true, string.Empty, providerUserKey, userIsOnline);
        }

        //Unlock a user given a username 
        public override bool UnlockUser(string username) {
            User user;
            var unlocked = false;
            try {
                user = GetUserByUsername(username);

                if (user != null) {
                    user.LastLockedOutDate = System.DateTime.Now;
                    user.IsLockedOut = false;
                    users.Update(user);
                    users.SaveChanges();
                    unlocked = true;
                }
            } catch (Exception e) {
                WriteToEventLog(e, "UnlockUser");
                throw new ProviderException(exceptionMessage);
            }
            return unlocked;
        }

        //Gets a membehsip user by email
        public override string GetUserNameByEmail(string email) {
            User user;
            try {
                user = users.GetUserByEmail(ApplicationName, email);
            } catch (Exception e) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(e, "GetUserNameByEmail");
                throw new ProviderException(exceptionMessage);
            }
            return user == null ? string.Empty : user.Username;
        }

        // Reset password for a user
        public override string ResetPassword(string username, string answer) {
            int rowsAffected = 0;
            User user;

            if (!EnablePasswordReset)
                throw new NotSupportedException("Password reset is not enabled.");

            if (answer == null && RequiresQuestionAndAnswer) {
                UpdateFailureCount(username, "passwordAnswer");
                throw new ProviderException("Password answer required for password reset.");
            }

            string newPassword =
                            System.Web.Security.Membership.GeneratePassword(newPasswordLength, MinRequiredNonAlphanumericCharacters);


            var args = new ValidatePasswordEventArgs(username, newPassword, true);

            OnValidatingPassword(args);

            if (args.Cancel)
                if (args.FailureInformation != null)
                    throw args.FailureInformation;
                else
                    throw new MembershipPasswordException("Reset password canceled due to password validation failure.");

            var passwordAnswer = "";
            try {
                user = GetUserByUsername(username);
                if (user == null)
                    throw new MembershipPasswordException("The supplied user name is not found.");

                if (user.IsLockedOut)
                    throw new MembershipPasswordException("The supplied user is locked out.");

                if (RequiresQuestionAndAnswer && !CheckPassword(answer, passwordAnswer)) {
                    UpdateFailureCount(username, "passwordAnswer");
                    throw new MembershipPasswordException("Incorrect password answer.");
                }

                user.Password = EncodePassword(newPassword);
                user.LastPasswordChangedDate = DateTime.Now;
                user.Username = username;
                user.ApplicationName = ApplicationName;
                users.Update(user);
                users.SaveChanges();
                rowsAffected = 1;
            } catch (Exception e) {
                if (WriteExceptionsToEventLog)
                    WriteToEventLog(e, "ResetPassword");
                throw new ProviderException(exceptionMessage);
            }
            if (rowsAffected > 0)
                return newPassword;
            throw new MembershipPasswordException("User not found, or user is locked out. Password not Reset.");
        }

        // Update a user information 
        public override void UpdateUser(MembershipUser membershipUser) {
            User user;
            try {
                user = GetUserByUsername(membershipUser.UserName);
                if (user != null) {
                    user.Email = membershipUser.Email;
                    user.Comment = membershipUser.Comment;
                    user.IsApproved = membershipUser.IsApproved;
                    users.Update(user);
                    users.SaveChanges();
                }
            } catch (Exception e) {
                if (WriteExceptionsToEventLog) {
                    WriteToEventLog(e, "UpdateUser");
                    throw new ProviderException(exceptionMessage);
                }
            }
        }

        // Validates as user
        public override bool ValidateUser(string username, string password) {
            var isValid = false;
            User user;
            try {
                user = GetUserByUsername(username);
                if (user == null)
                    return false;
                if (user.IsLockedOut)
                    return false;

                if (CheckPassword(password, user.Password)) {
                    if (user.IsApproved) {
                        isValid = true;
                        user.LastLoginDate = DateTime.Now;
                        users.Update(user);
                        users.SaveChanges();
                    }
                } else
                    UpdateFailureCount(username, "password");
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog) {
                    WriteToEventLog(ex, "ValidateUser");
                    throw new ProviderException(exceptionMessage);
                } else
                    throw ex;
            }
            return isValid;
        }

        //Find users by a name, note : does not do a like search
        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords) {
            IList<User> allusers = null;
            var users = new MembershipUserCollection();
            var counter = 0;
            var startIndex = pageSize * pageIndex;
            var endIndex = startIndex + pageSize - 1;
            totalRecords = 0;

            try {
                allusers = GetUsersByUsername(usernameToMatch);
                if (allusers == null)
                    return users;
                if (allusers.Count > 0)
                    totalRecords = allusers.Count;
                else
                    return users;

                foreach (var u in allusers) {
                    if (counter >= endIndex)
                        break;
                    if (counter >= startIndex) {
                        var mu = GetMembershipUser(u);
                        users.Add(mu);
                    }
                    counter++;
                }
            } catch (Exception e) {
                if (WriteExceptionsToEventLog) {
                    WriteToEventLog(e, "FindUsersByName");
                    throw new ProviderException(exceptionMessage);
                } else
                    throw e;
            }
            return users;
        }

        // Search users by email , NOT a Like match
        public override MembershipUserCollection FindUsersByEmail(string email, int pageIndex, int pageSize, out int totalRecords) {
            IList<User> allusers = null;
            var users = new MembershipUserCollection();
            var counter = 0;
            var startIndex = pageSize * pageIndex;
            var endIndex = startIndex + pageSize - 1;
            totalRecords = 0;
            try {
                allusers = GetUsersByEmail(email);
                if (allusers == null)
                    return users;
                if (allusers.Count > 0)
                    totalRecords = allusers.Count;
                else
                    return users;

                foreach (var u in allusers) {
                    if (counter >= endIndex)
                        break;
                    if (counter >= startIndex) {
                        var mu = GetMembershipUser(u);
                        users.Add(mu);
                    }
                    counter++;
                }
            } catch (Exception ex) {
                if (WriteExceptionsToEventLog) {
                    WriteToEventLog(ex, "FindUsersByEmail");
                    throw new ProviderException(exceptionMessage);
                } else
                    throw ex;
            }
            return users;
        }

        #endregion

        // Todo: Try and refactor these out to MembershipUtils
        #region New Utils

        private string GetConfigValue(string configValue, string defaultValue) {
            return String.IsNullOrEmpty(configValue) ? defaultValue : configValue;
        }

        private bool CheckPassword(string password, string dbpassword) {
            var pass1 = password;
            var pass2 = dbpassword;

            switch (PasswordFormat) {
                case MembershipPasswordFormat.Encrypted:
                    pass2 = UnEncodePassword(dbpassword);
                    break;
                case MembershipPasswordFormat.Hashed:
                    pass1 = EncodePassword(password);
                    break;
                default:
                    break;
            }
            return pass1 == pass2;
        }

        private string EncodePassword(string password) {
            var encodedPassword = password;

            switch (PasswordFormat) {
                case MembershipPasswordFormat.Clear:
                    break;
                case MembershipPasswordFormat.Encrypted:
                    encodedPassword =
                      Convert.ToBase64String(EncryptPassword(Encoding.Unicode.GetBytes(password)));
                    break;
                case MembershipPasswordFormat.Hashed:
                    var hash = new HMACSHA1 {
                        Key = HexToByte(_machineKey.ValidationKey)
                    };
                    encodedPassword =
                      Convert.ToBase64String(hash.ComputeHash(Encoding.Unicode.GetBytes(password)));
                    break;
                default:
                    throw new ProviderException("Unsupported password format.");
            }
            return encodedPassword;
        }

        // UnEncodePassword :Decrypts or leaves the password clear based on the PasswordFormat.
        private string UnEncodePassword(string encodedPassword) {
            string password = encodedPassword;

            switch (PasswordFormat) {
                case MembershipPasswordFormat.Clear:
                    break;
                case MembershipPasswordFormat.Encrypted:
                    password =
                      Encoding.Unicode.GetString(DecryptPassword(Convert.FromBase64String(password)));
                    break;
                case MembershipPasswordFormat.Hashed:
                    throw new ProviderException("Cannot unencode a hashed password.");
                default:
                    throw new ProviderException("Unsupported password format.");
            }

            return password;
        }

        // Converts a hexadecimal string to a byte array. Used to convert encryption key values from the configuration.    
        private byte[] HexToByte(string hexString) {
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }

        private void WriteToEventLog(Exception e, string action) {
            var log = new EventLog { Source = eventSource, Log = eventLog };

            var message = "An exception occurred communicating with the data source.\n\n";
            message += "Action: " + action + "\n\n";
            message += "Exception: " + e.ToString();

            log.WriteEntry(message);
        }

        #endregion
    }
}
