using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Web.Routing;
using System.Web.Security;
using MEG.Models;
using MEG.Helpers;

namespace Meg.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        //
        // GET: /Account/LogOn
        [AllowAnonymous]
        public ActionResult LogOn(string returnUrl)
        {
            //So that the user can be referred back to where they were when they click logon
            if (string.IsNullOrEmpty(returnUrl) && Request.UrlReferrer != null)
                returnUrl = Server.UrlEncode(Request.UrlReferrer.PathAndQuery);

            if (Url.IsLocalUrl(returnUrl) && !string.IsNullOrEmpty(returnUrl))
            {
                ViewBag.ReturnURL = returnUrl;
            }
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult LogOn(LogOnModel model, string returnUrl)
        {
            bool success = false;
            MUser user = new MUser();
            Int32 timeout = 60 * 2;
            if ((user = AuthenticationHelper.ValidateUser(model.Email, model.Password)) != null)
                if (AuthenticationHelper.LogInUser(user, timeout))
                    success = true;
            return RedirectToAction("Index", "Home");
        }

        public class LogOnModel
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }

        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();


            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public JsonResult JsonLogOff()
        {
            var success = false;
            try
            {
                FormsAuthentication.SignOut();
                success = true;
            }
            catch (Exception ex)
            {
            }
            return Json(success);
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Register(RegisterModel model)
        {
            using (MegEntities Db = new MegEntities())
            {
                if (Db.MUsers.Any(u => u.Email == model.Email))
                {
                    return View("EmailAlreadyExists");
                }
                else
                {
                    if (model.RegisterToken == "Meg0nl1n3")
                    {

                        // Attempt to register the user
                        AuthenticationHelper authentication = new AuthenticationHelper();
                        try
                        {
                            MUser user = new MUser();
                            user = null;

                            Guid? userID = AuthenticationHelper.CreateUser(model.Email, model.Password);

                            user = Db.MUsers.First(u => u.ID == userID);
                            AuthenticationHelper.SendVerificationEmail(user);

                        }
                        catch (Exception ex)
                        {
                        }
                    }
                    else
                    {
                        //incorrect registration token use
                        return View("IncorrectRegToken");
                    }
                    return View("EmailSent");
                }
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult IncorrectRegToken()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult EmailSent()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult EmailAlreadyExists()
        {
            return View();
        }

        public class RegisterModel
        {
            public string Email { get; set; }
            public string RegisterToken { get; set; }
            public string Password { get; set; }
        }

        [Authorize]
        public ActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            // ChangePassword will throw an exception rather
            // than return false in certain failure scenarios.
            bool changePasswordSucceeded;
            try
            {
                MembershipUser currentUser = Membership.GetUser(User.Identity.Name, true /* userIsOnline */);
                changePasswordSucceeded = currentUser.ChangePassword(model.OldPassword, model.NewPassword);
            }
            catch (Exception)
            {
                changePasswordSucceeded = false;
            }

            if (changePasswordSucceeded)
            {
                return RedirectToAction("ChangePasswordSuccess");
            }
            else
            {

            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        public class ChangePasswordModel
        {
            public string OldPassword { get; set; }
            public string NewPassword { get; set; }
        }

        [AllowAnonymous]
        public ActionResult NotAllowedUser()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult registrationConfirmation()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult VerifyEmail(Guid emailCode)
        {
            bool success = false;
            if (emailCode != null)
            {
                try
                {
                    using (MegEntities Db = new MegEntities())
                    {
                        if (Db.MUsers.Any(u => u.EmailCode == emailCode))//This isn't legit. Really need to get the userID from the page somehow??!
                        {                     //Currently just assuming that the user who clicked this link has the rights. ie mobile checks mobile no && smsCode
                            MUser user = Db.MUsers.First(u => u.EmailCode == emailCode);
                            success = true;
                            user.EmailVerified = true;
                            //reset the user's email code so can't be used again
                            user.EmailCode = Guid.NewGuid();
                        }


                        Db.SaveChanges();
                        success = true;
                    }

                }
                catch (Exception ex)
                {
                    return View("VerificationFailed");
                }
            };
            if (success)
            {

                return View("VerificationSuccess");
            }
            else
            {
                return View("VerificationFailed");
            };
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult VerificationSuccess()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult VerificationFailure()
        {
            return View();
        }
    }
}
