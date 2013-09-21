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

namespace MEG.Helpers
{
    public class MailHelper
    {
        public bool SendReservationSuccessEmail(Guid reservationID)
        {
            bool success = false;
            try
            {
                using(MegDatabaseEntities db = new MegDatabaseEntities()){

                }
            }
            catch (Exception ex)
            {

            }
                return success;
        }
        }
}