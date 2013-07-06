using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using puck.core.Models;
using puck.core.Abstract;
using puck.core.Constants;
using Newtonsoft.Json;
namespace puck.core.Controllers
{
    public class AdminController : Controller
    {
        I_Content_Indexer indexer;
        I_Content_Searcher searcher;
        I_Log log;
        I_Puck_Repository repo;
        public AdminController(I_Content_Indexer i, I_Content_Searcher s, I_Log l, I_Puck_Repository r) {
            this.indexer = i;
            this.searcher = s;
            this.log = l;
            this.repo = r;
        }

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
            var model = new List<PuckUser>();
            int totalUsers;
            var userCollection = Membership.Providers["puck"].GetAllUsers(0,int.MaxValue,out totalUsers);
            foreach (MembershipUser mu in userCollection) {
                var pu = new PuckUser();
                pu.User = mu;
                pu.Roles = Roles.GetRolesForUser(mu.UserName).ToList();
                var meta = repo.GetPuckMeta().Where(x =>x.Name== DBNames.UserStartNode && x.Key.Equals(mu.UserName)).FirstOrDefault();
                pu.StartNode =JsonConvert.DeserializeObject(meta.Value,typeof(List<PuckPicker>)) as List<PuckPicker>;
                model.Add(pu);
            }
            return View(model);
        }

        public ActionResult Edit(string userName=null) {
            var model = new PuckUser();
            if (!string.IsNullOrEmpty(userName)) {
                var usr = Membership.Providers["puck"].GetUser(userName, false);
                model.UserName = userName;
                model.Email = usr.Email;
                model.Roles = Roles.GetRolesForUser(userName).ToList();
                var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.UserStartNode && x.Key.Equals(usr.UserName)).FirstOrDefault();
                model.StartNode = JsonConvert.DeserializeObject(meta.Value, typeof(List<PuckPicker>)) as List<PuckPicker>;                
            }
            return View(model);
        }
        
    }
}
