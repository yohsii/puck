using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
namespace puck.core.Controllers
{
    public class AdminController : Controller
    {
        [HttpGet]
        public ActionResult In() {
            return View();
        }

        [HttpPost]
        public ActionResult In(puck.core.Models.LogIn user) {
            UrlHelper urlHelper = new UrlHelper(Request.RequestContext);

            if (!Membership.Providers["puck"].ValidateUser(user.Username, user.Password)) {
                user.Error = "Incorrect Login Information";
                return View(user);
            }
            FormsAuthentication.SetAuthCookie(user.Username,user.PersistentCookie);
            return RedirectToAction("Index", "Home", new { area="admin"});
        }

        public ActionResult Out() {
            FormsAuthentication.SignOut();
            return RedirectToAction("In");
        }

        //TODO renew ticket to stay signed in while using cms
        public ActionResult Renew() {
            return View();
        }

        public ActionResult Index()
        {
            return View();
        }

        
    }
}
