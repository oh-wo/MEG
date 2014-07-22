using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using System.IO;
using System.Web.UI;
using System.Data.Entity;
using MEG.Models;
using System.Net.Mail;

namespace MEG.Helpers
{
    public class MailHelper
    {
        public bool SendReservationSuccessEmail(ControllerContext context, Guid reservationID)
        {
            bool success = false;

            using (MegEntities db = new MegEntities())
            {
                try
                {
                    Reservation res = db.Reservations.FirstOrDefault(r => r.ID == reservationID);

                    var SmtpClient = new SmtpClient();
                    SmtpClient.Credentials = new System.Net.NetworkCredential("meg@fastmail.fm", "Mechanical");
                    SmtpClient.EnableSsl = true;
                    MailMessage mm = new MailMessage()
                    {
                        Body = ConvertViewToHtml(context, "ConfirmationEmail", res),
                        From = new System.Net.Mail.MailAddress("meg@mechanical-engineering-group.org.nz", "MEG", System.Text.Encoding.Default),
                        Subject = String.Format("MEG Reservation - {0}", res.Event.Title),
                        IsBodyHtml = true,
                    };
                    mm.To.Add(new MailAddress(res.Email));
                    SmtpClient.Send(mm);
                    success = true;
                }
                catch (Exception ex)
                {


                }
            }
            return success;
        }

        public static string ConvertViewToHtml(ControllerContext context, string viewName, object model)
        {
            if (string.IsNullOrEmpty(viewName))
                viewName = context.RouteData.GetRequiredString("action");

            var viewData = new ViewDataDictionary(model);

            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(context, viewName);
                var viewContext = new ViewContext(context, viewResult.View, viewData, new TempDataDictionary(), sw);
                viewResult.View.Render(viewContext, sw);

                return sw.GetStringBuilder().ToString();
            }
        }
    }
}