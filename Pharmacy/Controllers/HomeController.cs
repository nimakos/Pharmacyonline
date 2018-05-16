using System.Web.Mvc;

namespace Pharmacy.Controllers
{
    public class HomeController : Controller
    {
        [Authorize]
        public ActionResult Index(string login)
        {
            return View();
        }

        [Authorize]
        public ActionResult About()
        {
            ViewBag.Message = "Σχετικά με εμάς";

            return View();
        }

        [Authorize]
        public ActionResult Contact()
        {
            ViewBag.Message = "Στοιχεία Επικοινωνίας";
            return View();
        }
     
    }
}