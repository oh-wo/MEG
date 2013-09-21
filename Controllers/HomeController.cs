using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MEG.Helpers;
using MEG.Models;
using System.IO;
using System.Data.Entity;
using Mvc.Mailer;
using MEG.Mailers;

namespace MEG.Controllers
{
    public class HomeController : Controller
    {
        //Mailer Stuff
        private IUserMailer _userMailer = new UserMailer();
        public IUserMailer UserMailer
        {
            get { return _userMailer; }
            set{_userMailer = value;}
        }


        //
        // GET: /Home/

        public ActionResult Index()
        {
            List<Event> events = new List<Event>();
            using (MegDatabaseEntities db = new MegDatabaseEntities())
            {
                if (db.Events.Any(a => a.ID != null))
                {
                    events = db.Events.Take(20).OrderByDescending(e => e.EventDate).ToList();
                }
                else
                {
                    //there are no events
                }
            }
            return View(events);
        }
        public ActionResult Past()
        {
            List<Event> events = new List<Event>();
            using (MegDatabaseEntities db = new MegDatabaseEntities())
            {
                if (db.Events.Any(a => a.ID != null))
                {
                    events = db.Events.Where(a => a.EventDate < DateTime.UtcNow).Take(20).OrderByDescending(e => e.EventDate).ToList();
                }
                else
                {
                    //there are no events
                }
            }
            return View("Index", events);
        }
        public ActionResult Upcoming()
        {
            List<Event> events = new List<Event>();
            using (MegDatabaseEntities db = new MegDatabaseEntities())
            {
                if (db.Events.Any(a => a.ID != null))
                {
                    events = db.Events.Where(a => a.EventDate > DateTime.UtcNow).Take(20).OrderByDescending(e => e.EventDate).ToList();
                }
                else
                {
                    //there are no events
                }
            }
            return View("Index", events);
        }
        public ActionResult Article(Guid articleID)
        {
            try
            {
                using (MegDatabaseEntities db = new MegDatabaseEntities())
                {
                    Event article = db.Events.First(a => a.ID == articleID);
                    return View(article);
                }
            }
            catch (Exception ex)
            {
                return View("Index", "Home");//Should really display an error message and stay on page
            }

        }
        public ActionResult Contact()
        {
            return View();
        }
        public ActionResult About()
        {
            return View();
        }
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AuthenticationHelper.IsUser]
        public ActionResult CreateArticle(string Title)
        {
            Guid ID = Guid.NewGuid();
            try
            {
                using (MegDatabaseEntities db = new MegDatabaseEntities())
                {
                    Event article = new Event()
                    {
                        ID = ID,
                        CreatedBy = new Guid(User.Identity.Name),
                        Title = Title,
                        Created = DateTime.UtcNow,
                        SeatsTaken = 0,
                    };
                    db.Events.Add(article);
                    db.SaveChanges();


                }


            }
            catch (Exception ex)
            {

            }
            return RedirectToAction("Article", "Home", new { articleID = ID });
        }

        [HttpPost]
        [AuthenticationHelper.IsUser]
        public JsonResult DeleteArticle(Guid ArticleID)
        {
            DeleteArticleResult result = new DeleteArticleResult()
            {
                Success = false,
            };
            try
            {
                using (MegDatabaseEntities db = new MegDatabaseEntities())
                {
                    Event ev = db.Events.First(e => e.ID == ArticleID);
                    db.Events.Remove(ev);
                    db.SaveChanges();
                    result.Success = true;
                }
            }
            catch (Exception ex)
            {
                result.Message = "You can't delete this event because it has other data associated with it. You can make it inactive. This will disable emails and hide the article from the public.";
            }
            return Json(result);
        }

        public class DeleteArticleResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }

        public class ArticleMods
        {
            public string Field { get; set; }
            public string Value { get; set; }
            public Guid ArticleID { get; set; }
        }

        [HttpPost]
        [AuthenticationHelper.IsUser]
        public JsonResult UploadImage(string filename)
        {
            ImageLoadSuccess ils = new ImageLoadSuccess()
            {
                success = false,
            };
            if (Request.Files.Count > 0)
            {
                try
                {
                    string path = "/Content/Uploads/";
                    string fileExtension = System.IO.Path.GetExtension(Request.Files[0].FileName);
                    string filenameWithExtension = String.Concat(filename, fileExtension);
                    Request.Files[0].SaveAs(Path.Combine(Server.MapPath("/Content/Uploads"), filenameWithExtension));
                    using (MegDatabaseEntities db = new MegDatabaseEntities())
                    {
                        Guid id = new Guid(filename);
                        if (db.Events.Any(e => e.ID == id))
                        {
                            Event article = db.Events.First(e => e.ID == id);
                            article.HasImage = true;
                            article.ImageExtension = fileExtension;
                            db.SaveChanges();
                            ils.success = true;
                            ils.url = filenameWithExtension;
                        }
                        else
                        {
                            //article doesn't exit; weird!
                        }
                    }
                    ils.success = true;
                }
                catch (Exception ex)
                {

                }

            }
            return Json(ils);
        }
        private class ImageLoadSuccess
        {
            public bool success { get; set; }
            public string url { get; set; }
        }

        private class ArticleUpdateSuccess
        {
            public bool Success { get; set; }
            public string Value { get; set; }//In case some data needs to be returned
        }
        [HttpPost]
        [AuthenticationHelper.IsUser]
        public JsonResult UpdateArticle(ArticleMods articleMods)
        {
            ArticleUpdateSuccess aus = new ArticleUpdateSuccess()
            {
                Success = false,
            };
            using (MegDatabaseEntities db = new MegDatabaseEntities())
            {
                if (db.Events.Any(e => e.ID == articleMods.ArticleID))
                {
                    try
                    {
                        Event article = db.Events.First(e => e.ID == articleMods.ArticleID);
                        switch (articleMods.Field)
                        {
                            case "Title":
                                article.Title = articleMods.Value;
                                break;
                            case "Body":
                                article.Body = articleMods.Value;
                                break;
                            case "Presenter":
                                article.Presenter = articleMods.Value;
                                break;
                            case "EventDate":
                                if (article.EventDate == null)
                                {
                                    article.EventDate = DateTime.Parse(articleMods.Value);
                                }
                                else
                                {
                                    //Need to update the reminder date too
                                    DateTime oldEventDate = article.EventDate.Value;
                                    article.EventDate = DateTime.Parse(articleMods.Value);
                                    if (article.ReminderSendDate == null)
                                    {
                                        article.ReminderSendDate = article.EventDate.Value.AddDays(-1);//default is 1 day behind reminder
                                    }
                                    else
                                    {
                                        //Already has a reminder set so update this to keep same offset
                                        int offset = (oldEventDate - article.ReminderSendDate.Value).Days;
                                        article.ReminderSendDate = article.EventDate.Value.AddDays(-offset);
                                    }
                                }
                                break;
                            case "StartTime":
                                article.StartTime = DateTime.Parse(articleMods.Value).TimeOfDay.TotalHours;
                                break;
                            case "EndTime":
                                article.EndTime = DateTime.Parse(articleMods.Value).TimeOfDay.TotalHours;
                                break;
                            case "Venue01":
                                article.Venue1 = articleMods.Value;
                                break;
                            case "Venue02":
                                article.Venue2 = articleMods.Value;
                                break;
                            case "Venue03":
                                article.Venue3 = articleMods.Value;
                                break;
                            case "Venue04":
                                article.Venue4 = articleMods.Value;
                                break;
                            case "TotalSeats":
                                article.TotalSeats = Int32.Parse(articleMods.Value);
                                break;
                            case "reminderEmailOffset":
                                article.ReminderSendDate = article.EventDate.Value.AddDays(-Int32.Parse(articleMods.Value));
                                aus.Value = article.ReminderSendDate.Value.ToLongDateString();
                                break;
                        }
                        db.SaveChanges();
                        aus.Success = true;
                    }
                    catch (Exception ex)
                    {
                        //something went wrong!! 

                    }
                }
                else
                {
                    //no article with that id found... move on.
                }
            }
            return Json(aus);
        }


        [AuthenticationHelper.IsUser]
        public ActionResult Applicants()
        {
            List<Applicant> applicants = new List<Applicant>();
            using (MegDatabaseEntities db = new MegDatabaseEntities())
            {
                applicants = db.Applicants.Take(20).ToList();
            }
            return View(applicants);
        }

        [HttpPost]
        public JsonResult CreateApplicant(ClientApplicant appl)
        {
            //Saves the applicant to the database
            //Checks made clientside with javascript, so all variables should be filled here
            bool success = false;
            string date = appl.DOB;
            using (MegDatabaseEntities db = new MegDatabaseEntities())
            {
                Applicant app = new Applicant()
                {
                    ID = Guid.NewGuid(),
                    //DateOfUpload = DateTime.UtcNow,
                    FirstName = appl.FirstName,
                    LastName = appl.LastName,
                    PersonalAddress = appl.PersonalAddress,
                    BusinessAddress = appl.BusinessAddress,
                    PreferredAddress = appl.PreferredAddress,
                    BusinessEmail = appl.BusinessEmail,
                    BusinessPhone = appl.BusinessPhone,
                    AcademicQualifications = appl.AcademicQualifications,
                    Employeer = appl.Employeer,
                    LearnedSocieties = appl.LearnedSocieties,
                    PersonalEmail = appl.PersonalEmail,
                    PersonalPhone = appl.PersonalPhone,
                    PreferredPhone = appl.PreferredPhone,
                    PreferredEmail = appl.PreferredEmail,
                    WorkExperience = appl.WorkExperience,

                    DOB = DateTime.Parse(date),
                };
                try
                {
                    db.Applicants.Add(app);
                    db.SaveChanges();
                }
                catch
                {

                }
                success = true;

            }
            return Json(success);
        }

        public class ClientApplicant
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string DOB { get; set; }
            public string PersonalAddress { get; set; }
            public string BusinessAddress { get; set; }
            public string PreferredAddress { get; set; }
            public string PersonalPhone { get; set; }
            public string BusinessPhone { get; set; }
            public string PreferredPhone { get; set; }
            public string PersonalEmail { get; set; }
            public string BusinessEmail { get; set; }
            public string PreferredEmail { get; set; }
            public string AcademicQualifications { get; set; }
            public string WorkExperience { get; set; }
            public string Employeer { get; set; }
            public string LearnedSocieties { get; set; }
        }

        [HttpPost]
        [AuthenticationHelper.IsUser]
        public JsonResult EditApplicant(ApplicantData appData)
        {
            bool success = false;

            using (MegDatabaseEntities db = new MegDatabaseEntities())
            {
                if (db.Applicants.Any())
                {
                    //Applicant exists - make some changes
                    Applicant app = db.Applicants.First(a => a.ID == appData.ID);
                    switch (appData.DataType)
                    {
                        case "firstName": app.FirstName = appData.Data;
                            break;
                        case "lastName": app.LastName = appData.Data;
                            break;
                        case "personalAddress": app.PersonalAddress = appData.Data;
                            break;
                        case "businessAddress": app.BusinessAddress = appData.Data;
                            break;
                        case "preferredAddress": app.PreferredAddress = appData.Data;
                            break;
                        case "personalEmail": app.PersonalEmail = appData.Data;
                            break;
                        case "businessEmail": app.BusinessEmail = appData.Data;
                            break;
                        case "personalPhone": app.PersonalPhone = appData.Data;
                            break;
                        case "businessPhone": app.BusinessPhone = appData.Data;
                            break;
                        case "preferredEmail": app.PreferredEmail = appData.Data;
                            break;
                        case "acaQual": app.AcademicQualifications = appData.Data;
                            break;
                        case "workExp": app.WorkExperience = appData.Data;
                            break;
                        case "employeer": app.Employeer = appData.Data;
                            break;
                        case "learnedSocieties": app.LearnedSocieties = appData.Data;
                            break;
                        case "dob": app.DOB = DateTime.Parse(appData.Data);
                            break;

                    }
                    db.SaveChanges();
                    success = true;
                }
                else
                {
                    //no applicant exists - werid!
                }

            }
            return Json(success);
        }
        public class ApplicantData
        {
            public Guid ID { get; set; }
            public string Data { get; set; }
            public string DataType { get; set; }
        }

        [AuthenticationHelper.IsUser]
        public ActionResult Reservations()
        {
            List<Event> events = new List<Event>();
            using (MegDatabaseEntities meg = new MegDatabaseEntities())
            {
                events = meg.Events.Where(e => e.EventDate.Value.Year == DateTime.Now.Year).Include("Reservations").ToList();
                
                return View(events);
            }

        }

        [HttpPost]
        public JsonResult CreateReservation(Guid EventID, string Name, string Email, int NoSeats, bool MemberMeg, bool MemberIpenz, bool MemberImeche)
        {//This has to be open to the public shouldn't have an isauthorized attribute
            ReservationResult resRes = new ReservationResult();
            resRes.Success = false;
            
            using (MegDatabaseEntities db = new MegDatabaseEntities())
            {
                Reservation res = new Reservation()
                {
                    Name = Name,
                    NoSeats = NoSeats,
                    Email = Email.Trim(),
                    MemberImeche = MemberImeche,
                    MemberMeg = MemberMeg,
                    MemberIpenz = MemberIpenz,
                    Timestamp = DateTime.UtcNow,
                    ID = Guid.NewGuid(),
                    EventID = EventID,
                };
                Event e = db.Events.First(ev => ev.ID == EventID);
                if (e.SeatsTaken == null) { e.SeatsTaken = 0; };
                resRes.OnEmailList = db.MailingLists.Any(ml => ml.Email == Email.Trim());
                e.SeatsTaken += NoSeats;
                db.Reservations.Add(res);
                db.SaveChanges();
                resRes.Success = true;
                MailHelper mh = new MailHelper();
                mh.SendReservationSuccessEmail(res.ID);

                HttpContext.Response.Cookies.Add(new HttpCookie(EventID.ToString(), true.ToString()));

            }
            return Json(resRes);
        }
        private class ReservationResult
        {
            public bool Success { get; set; }
            public bool OnEmailList { get; set; }
        }
        [HttpPost]
        public JsonResult DeleteReservation(Guid EventID, Guid ReservationID)
        {//This has to be open to the public shouldn't have an isauthorized attribute
            bool success = false;
            
            using (MegDatabaseEntities db = new MegDatabaseEntities())
            {
                if (db.Reservations.Any(r => r.ID == ReservationID && r.EventID==EventID))
                {
                    try
                    {
                        Reservation res = db.Reservations.First(r => r.ID == ReservationID);
                        Event ev = db.Events.First(e => e.ID == EventID);
                        ev.SeatsTaken -= res.NoSeats;
                        db.Reservations.Remove(res);
                        db.SaveChanges();
                        success = true;
                    }
                    catch (Exception ex)
                    {

                    }
                }
                else
                {
                    //improper data input
                }
            }

            return Json(success);
        }

        [HttpPost]
        public JsonResult AddToMailingList(string Name, string Email)
        {
            bool success = false;
            using (MegDatabaseEntities db = new MegDatabaseEntities())
            {
                if (!db.MailingLists.Any(e => e.Email == Email.Trim()))
                {
                    //Email doesn't already exist - add it
                    MailingList ml = new MailingList()
                    {
                        Name = Name,
                        Email = Email.Trim(),
                        Added = DateTime.UtcNow,
                        ID = Guid.NewGuid(),
                    };
                    try
                    {
                        db.MailingLists.Add(ml);
                        db.SaveChanges();
                        success = true;
                    }
                    catch (Exception ex)
                    {

                    }
                }
                else
                {
                    //Email already exists. Return success even though this isn't strictly correct
                    success = true;
                }
            }
            return Json(success);
        }

        public PartialViewResult ReservationBox(Guid EventID)
        {
            Event ev = new Event();
            using (MegDatabaseEntities db = new MegDatabaseEntities())
            {
                ev = db.Events.First(e => e.ID == EventID);
            }
            return PartialView(ev);
        }

        public ActionResult ReservationDetails(Reservation res)
        {
            return View(res);
        }

        public ActionResult TestReservationDetails()
        {
            UserMailer.Welcome().Send();

            return RedirectToAction("Index");
        }
        private int PageSize = 50;
        [AuthenticationHelper.IsUser]
        public ActionResult MailingList()
        {
            MailingData mailData = new MailingData()
            {
                MailingList = new List<MailingList>(),
            };
            using (MegDatabaseEntities db = new MegDatabaseEntities())
            {
                List<MailingList> allMailing = db.MailingLists.OrderBy(a=>a.Name).ToList();
                mailData.MailingList = allMailing.Take(PageSize).ToList();
                mailData.ContainsFirst = true;//Always returns the first
                mailData.ContainsLast = allMailing.Last() == mailData.MailingList.Last();//May not contain the absolute last
            }
            return View(mailData);
        }

        public class MailingData
        {
            public bool ContainsFirst { get; set; }
            public bool ContainsLast { get; set; }
            public List<MailingList> MailingList { get; set; }
        }

        [AuthenticationHelper.IsUser]
        public JsonResult GetMoreMailing(int Direction, Guid IndexID)
        {
            MailingData mData = new MailingData(){
                MailingList = new List<MailingList>(),
            };
            using (MegDatabaseEntities db = new MegDatabaseEntities())
            {
                //Get the index of the IndexID sent
                List<MailingList> allMailing = db.MailingLists.OrderBy(a=>a.Name).ToList();
                MailingList indexItem = allMailing.FirstOrDefault(ml => ml.ID == IndexID);
                int index = allMailing.IndexOf(indexItem);
                
                switch (Direction)
                {
                    case 0://User requesting previous
                        mData.MailingList = allMailing.Skip((index-PageSize)>0?(index-PageSize):0).Take(PageSize).ToList();
                        mData.ContainsFirst = allMailing.First() == mData.MailingList.First();//May not contain the absolute first
                        mData.ContainsLast = allMailing.Last() == mData.MailingList.Last();//May not contain the absolute last
                        break;
                    case 1://User requesting next
                        mData.MailingList = allMailing.Skip(index+1).Take(PageSize).ToList();
                        mData.ContainsFirst = allMailing.First() == mData.MailingList.First();//May not contain the absolute first
                        mData.ContainsLast = allMailing.Last() == mData.MailingList.Last();//May not contain the absolute last
                        break;
                    default: return Json("Error");
                }

            }

            return Json(mData);
        }

        [AuthenticationHelper.IsUser]
        [HttpPost]
        public JsonResult EditAMailingList(MailListData mailData)
        {
            bool success = false;

            using (MegDatabaseEntities db = new MegDatabaseEntities())
            {
                if (db.MailingLists.Any(ml=>ml.ID==mailData.ID))
                {
                    //Applicant exists - make some changes
                    MailingList mailList = db.MailingLists.First(ml => ml.ID == mailData.ID);
                    switch (mailData.DataType)
                    {
                        case "Name":  mailList.Name = mailData.Data;
                            break;
                        case "Email":  mailList.Email = mailData.Data;
                            break;
                        case "Added":  mailList.Added = DateTime.Parse(mailData.Data);
                            break;

                    }
                    db.SaveChanges();
                    success = true;
                }
                else
                {
                    //no mailingList exists - werid!
                }

            }
            return Json(success);
        }

        [AuthenticationHelper.IsUser]
         [HttpPost]
        public JsonResult DeleteFromMailingList(Guid mailingID)
        {
            bool _result = false;
            using (MegDatabaseEntities db = new MegDatabaseEntities())
            {
                if (db.MailingLists.Any(ml => ml.ID == mailingID))
                {
                    MailingList mailList = db.MailingLists.First(ml => ml.ID == mailingID);
                    db.MailingLists.Remove(mailList);
                    try
                    {
                        db.SaveChanges();
                        _result = true;
                    }
                    catch (Exception ex)
                    {

                    }

                }
            }
            return Json(_result);
        }

        [AuthenticationHelper.IsUser]
        [HttpPost]
        public JsonResult ChangeEventActivity(Guid eID, bool state)
        {
            bool success = false;
            using (MegDatabaseEntities db = new MegDatabaseEntities())
            {
                if (db.Events.Any(e => e.ID == eID))
                {
                    Event article = db.Events.First(e => e.ID == eID);
                    article.Active = state;

                }
                else
                {
                    //no event with that id - weird
                }
                try
                {
                    db.SaveChanges();
                    success = true;
                }
                catch (Exception ex)
                {

                }
            }
            return Json(success);
        }


        public class MailListData
        {
            public Guid ID { get; set; }
            public string Data { get; set; }
            public string DataType { get; set; }
        }
    }
}
