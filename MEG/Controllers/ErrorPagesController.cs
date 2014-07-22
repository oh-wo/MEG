using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MEG.Models;

namespace MEG.Controllers
{
    public class ErrorPagesController : Controller
    {
        //
        // GET: /ErrorPages/

        public ActionResult Oops()
        {
            //string x = Request.Url.ToString().Split('?')[1].ToString();
            return View();
        }

        [HttpPost]
        public JsonResult SubmitUserErrorMessage(string UserErrorMessage, string ErrorPath)
        {
            bool Success = false;

            try
            {
                System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();
                message.To.Add("obodley@gmail.com");
                message.Subject = "MEG Website - ErrorMessage";
                message.From = new System.Net.Mail.MailAddress("obodley@gmail.com");
                string UserName = "";
                    string UserEmail= "";
                if (User.Identity.IsAuthenticated)
                {
                    Guid ID = new Guid(User.Identity.Name);
                    using(MegEntities db = new MegEntities()){
                        if (db.MUsers.Any(u => u.ID == ID))
                        {
                            MUser thisUser = db.MUsers.First(u => u.ID == ID);
                            UserEmail = thisUser.Email;
                            UserName = thisUser.FirstName +" "+ thisUser.LastName;
                        }
                    }

                }
                message.Body = "User name: " + UserName + "\n User email: " + UserEmail + "\n Error Path: " + ErrorPath + "\n User Message: " + UserErrorMessage + "";
                var client = new System.Net.Mail.SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new System.Net.NetworkCredential("obodley@gmail.com", "rngdrjbkvaczzkpu"),
                    EnableSsl = true
                };
                client.Send(message);
                Success = true;
            }
            catch (Exception ex)
            {

            }

            return Json(Success);
        }

    }
}
