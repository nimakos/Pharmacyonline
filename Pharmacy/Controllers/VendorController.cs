using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Security;
using Pharmacy.Models;

namespace Pharmacy.Controllers
{
    public class VendorController : Controller
    {
        // GET: Vendor Registration
        [HttpGet]
        public ActionResult Registration()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registration([Bind(Exclude = "IsEmailVerified,ActivationCode")] Vendor vendor)
        {
            bool status = false;
            string message;

            // Mode Validation

            if (ModelState.IsValid)
            {
                #region Email already Exists
                var isExist = IsEmailExist(vendor.Email);
                if (isExist)
                {
                    ModelState.AddModelError("EmailExist", "Email already exists");
                    return View(vendor);

                }
                #endregion

                #region  Generate Activation Code
                vendor.ActivationCode = Guid.NewGuid(); // περναει εναν κωδικό στο πεδιο του χρήστη 
                #endregion


                #region Password Hashing
                vendor.Password = Crypto.Hash(vendor.Password);
                vendor.ConfirmPassword = Crypto.Hash(vendor.ConfirmPassword);
                #endregion
                vendor.IsEmailVerified = false;


                #region Save to Database
                using (CompanyEntities4 dc = new CompanyEntities4())
                {
                    dc.Vendors.Add(vendor);  //παίρνει τις τιμες από τα texboxes 
                    dc.SaveChanges();  //και τα αποθηκευει στη βάση
                 
                    //Send Email to User
                    SendVerificationLinkEmail(vendor.Email, vendor.ActivationCode.ToString());
                    message = "Registration successfully done. Account activation link" +
                              "has been sent to your email id:" + vendor.Email;
                    status = true;
                }
                #endregion
            }
            else
            {
                message = "Invalid Request";
            }

            ViewBag.Message = message;
            ViewBag.Status = status;

            return View(vendor);
        }


        [HttpGet]
        public ActionResult VerifyAccount(string id) //αυτη η μέθοδος καλείται στη γραμμή 194
        {
            bool status = false;
            using (CompanyEntities4 dc = new CompanyEntities4())
            {
                dc.Configuration.ValidateOnSaveEnabled = false; //this line i have added here to avoid 
                //confirm password does not match issue on save changes
                var v = dc.Vendors.FirstOrDefault(a => a.ActivationCode == new Guid(id));
                if (v != null)
                {
                    v.IsEmailVerified = true;
                    dc.SaveChanges();
                    status = true;
                }
                else
                {
                    ViewBag.Message = "Invalid Request";
                }
            }
            ViewBag.Status = status;
            return View();
        }

        [NonAction]
        public bool IsEmailExist(string emailId)
        {
            using (CompanyEntities4 dc = new CompanyEntities4())
            {
                var v = dc.Vendors.FirstOrDefault(a => a.Email == emailId);
                return v != null;
            }
        }

        //το emailID είναι αυτο που δίνει ο χρήστης, και το activationCode περνιεται αυτόματα στη βάση αρχικά
        //και στη συνέχεια στέλνεται στον χρήστη μέσω mail(URL) για να το ξαναστείλει πάλι πίσω και 
        //να διαπιστωθεί εαν είναι ο ίδιος χρήστης
        [NonAction]
        public void SendVerificationLinkEmail(string emailId, string activationCode)
        {
            var verifyUrl = "/Vendor/VerifyAccount/" + activationCode;
            if (Request.Url != null)
            {
                var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyUrl);

                var fromEmail = new MailAddress("nimakos21@gmail.com", "Verification Code");
                var toEmail = new MailAddress(emailId);
                var fromEmailPassword = "29071982"; //Replace with actual password
                string subject = "Your account is successfully created!";

                string body = "<br/><br/> We are excited to tell you that your account is" +
                              "Successfully created. Please click on the below link to verify your account" +
                              "<br/><br/><a href = '" + link + "'>" + link + "</a>";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromEmail.Address, fromEmailPassword)
                };

                using (var message = new MailMessage(fromEmail, toEmail)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                    smtp.Send(message);
            }
        }


        //Vendor Login
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(VendorLogin login,string returnUrl = "")
        {
            
            string message;
            using (CompanyEntities4 dc = new CompanyEntities4())
            {
                //To a.EmailID = είναι το mail που υπαρχει στη βαση μας, Το login.EmailID = είναι το mail που εχει πληκτρολογησει η χρήστης
                var v = dc.Vendors.FirstOrDefault(a => a.Email == login.Email); //επιστρεφει ΟΛΑ τα στοιχεια του χρήστη με το συγκεκριμένο email
                if (v != null && v.IsEmailVerified) //εαν υπάρχει αυτό το mail και αρα και ο χρήστης..., και το mail εχει επιβεβαιωθει
                {
                    //Συκρινε το password που πληκτρολογησε ο χρήσης(login.PassWord) με το password που υπάρχει στη βάση(v.PassWord) 
                    if (string.CompareOrdinal(Crypto.Hash(login.Password), v.Password) == 0)
                    {

                        //εαν εχει τσεκαριστει το login.Rememberme(TRUE), τοτε ο χρόνος αναμονής θα είναι 525600 αλλιως θα είναι 20 min(να αποσυνδεθεις δηλ σε 20 min)
                        //SOS ΚΡΑΤΑΕΙ ΤΟ SESSION
                        int timeout = login.RememberMe ? 525600 : 20;  //525600 min = 1 year
                        var ticket = new FormsAuthenticationTicket(login.Email, login.RememberMe, timeout);//το ticket θα εχει ονομα(στην περιπτωση μας το email), εαν θα ειναι μονιμο, και ο χρονος λήξης του
                        var encrypted = FormsAuthentication.Encrypt(ticket); //το κρυπτογραφούμε
                        var cookie =
                            new HttpCookie(FormsAuthentication.FormsCookieName, encrypted)// πέρνουμε μονο το όνομα (το mail στην περιπτωση μας), και την τιμη του(κρυπτογραφημενη)
                            {
                                Expires = DateTime.Now.AddMinutes(timeout),//ποτε λήγει
                                HttpOnly = true
                            };
                        Response.Cookies.Add(cookie);
                      
                        if (Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        else
                        {

                           return RedirectToAction("Index", "Home");
                            
                        }
                    }
                    else
                    {
                        message = "Invalid credential provided";
                    }

                }
                else
                {
                    message = "Invalid credential provided1";
                }
            }


            ViewBag.Message = message;

            return View();
        }


        //Logout
        [Authorize]
        [HttpPost]
        public ActionResult Logout() //η μέθοδος καλείται απο το αρχειο viewpage1.cshtml
        {
            FormsAuthentication.SignOut();  //αφαιρεί το ticket
            return RedirectToAction("Login", "Vendor");
        }


    }
}