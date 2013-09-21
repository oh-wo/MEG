using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MEG.Models;
using System.Web.Mvc;
using System.Web.Security;
using System.Net.Mail;

namespace MEG.Helpers
{
    public class AuthenticationHelper //:BaseModel
    {

        /* Specifically related to logging in/out */
        private static readonly string GuestUser = "GuestUser";

        public static bool LogInUser(MUser user, int timeout)
        {
            bool success = false;
            try
            {
                AuthenticationHelper authHelper = new AuthenticationHelper();
                //var userName = (user == null ? GuestUser : user.ID.ToString());
                var ticket = new FormsAuthenticationTicket(1, user.ID.ToString(), DateTime.Now, DateTime.Now.AddMinutes(timeout), true, "", FormsAuthentication.FormsCookiePath);
                var encTicket = FormsAuthentication.Encrypt(ticket);
                HttpContext.Current.Response.Cookies.Add(new HttpCookie(FormsAuthentication.FormsCookieName, encTicket));
                success = true;
            }
            catch (Exception ex)
            {
            }
            return success;
        }

        public static MUser GetUser(string email)
        {
            using (var Db = new MegDatabaseEntities())
            {
                return Db.MUsers.First(u => email == u.Email) ?? null;
            }
        }

        public static MUser GetUser(Guid id)
        {
            using (var Db = new MegDatabaseEntities())
            {
                return Db.MUsers.First(u => u.ID == id);
            }
        }

        public static MUser GetCurrentUser()
        {
            AuthenticationHelper authHelper = new AuthenticationHelper();
            return GetUser(CurrentSessionUserName);
        }

        private static string CurrentSessionUserName
        {
            get
            {
                using (var Db = new MegDatabaseEntities())
                {
                    if (!HttpContext.Current.User.Identity.IsAuthenticated)
                        return null;
                    return HttpContext.Current.User.Identity.Name;
                };
            }
        }

        public static bool IsUnknownUser
        {
            get { return CurrentSessionUserName == null; }
        }

        public static bool IsGuestUser
        {
            get { return CurrentSessionUserName == GuestUser; }
        }

        public static bool IsloggedIn
        {
            get
            {
                AuthenticationHelper authHelper = new AuthenticationHelper();
                return CurrentSessionUserName != null && CurrentSessionUserName != GuestUser && CurrentSessionUserName.ToString() != new Guid().ToString();
            }
        }


        //Action filter to check that the MUser is logged in (prohibit use of user section and business section)
        public class IsUserAttribute : AuthorizeAttribute
        {
            public IsUserAttribute()
            {
            }

            protected override bool AuthorizeCore(HttpContextBase httpContext)
            {
                return (IsloggedIn);
            }
            protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
            {
                if (IsloggedIn)
                {
                        filterContext.Result = new RedirectResult("/Account/NotAllowedUser");
                }
                else
                {
                    base.HandleUnauthorizedRequest(filterContext);
                };
            }
        }

        /*General account functions */
        public static bool UpdateFailedPasswordAttemptCount(Guid id)
        {
            using (var Db = new MegDatabaseEntities())
            {
                bool success = false;
                try
                {
                    MUser user = Db.MUsers.First(u => u.ID == id);
                    user.FailedPasswordAttemptCount++;
                    Db.SaveChanges();
                    success = true;
                }
                catch (Exception ex)
                {
                }
                return success;
            }
        }

        private static readonly Int32 MinRequiredPasswordLength = 6;

        public static MUser ValidateUser(string email, string password)
        {
            using (var Db = new MegDatabaseEntities())
            {
                if (!string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(email))
                {

                    MUser user = GetUser(email.Trim()) ?? null;

                    if (user != null)
                    {

                        /*if (user.FailedPasswordAttemptCount == null)
                        {
                            user.FailedPasswordAttemptCount = 1;
                            Db.SaveChanges();
                        }
                        else
                        {
                            UpdateFailedPasswordAttemptCount(user.ID);
                            Db.SaveChanges();
                        }*/
                    }
                    else
                    {

                        return null;
                    }

                    string hash = FormsAuthentication.HashPasswordForStoringInConfigFile(password.Trim(), "SHA1");
                    return Db.MUsers.FirstOrDefault(u => u.Email == email && u.Password == hash) ?? null;
                }
                return null;
            }
        }

        public static Guid? CreateUser(string email, string password)
        {
            using (var Db = new MegDatabaseEntities())
            {
                bool success = false;

                //dummy variable - only created so can be set to null so null can be returned if this fails 
                Guid? userID = new Guid();
                userID = null;

                string hash = FormsAuthentication.HashPasswordForStoringInConfigFile(password.Trim(), "SHA1");

                Random rand = new Random();
                MUser user = new MUser()
                {
                    ID = Guid.NewGuid(),
                    Email = email,
                    EmailCode = Guid.NewGuid(),
                    EmailVerified = false,
                    Password = hash,
                    Created = DateTime.UtcNow,
                    IsLockedOut = false,
                    LastActivityDate = DateTime.UtcNow,
                };

                try
                {
                    Db.MUsers.Add(user);
                    Db.SaveChanges();
                    success = true;
                }
                catch (Exception ex)
                {
                }
                return (success == true) ? user.ID : userID;
            }
        }

        public static bool ChangePassword(string email, string oldPassword, string newPassword)
        {
            using (var Db = new MegDatabaseEntities())
            {
                if ((ValidateUser(email, oldPassword) == null) || string.IsNullOrEmpty(newPassword.Trim()))
                    return false;
                MUser user = GetUser(email);
                string hash = FormsAuthentication.HashPasswordForStoringInConfigFile(newPassword.Trim(), "SHA1");

                user.Password = hash;
                Db.SaveChanges();
                return true;
            }
        }

        public static bool SendVerificationEmail(MUser user)
        {
            bool success = false;
            try
            {

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 465);
                smtp.UseDefaultCredentials = false;
                var credentials = new System.Net.NetworkCredential("hookedup@hookedup.co.nz", "M0nk3yS4nctu4ry");
                smtp.Credentials = credentials;
                smtp.EnableSsl = true;
                smtp.Port = 587;

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("hookedup@hookedup.co.nz");
                mail.To.Add(user.Email);
                mail.Subject = "Confirm your email address";
                mail.Body = "Click this link to confirm your address: http://mechanical-engineering-group.org.nz/Account/VerifyEmail/?emailCode=" + user.EmailCode;

                smtp.Send(mail);
                success = true;
            }
            catch (Exception ex)
            {
            }
            return success;
        }
    }
}