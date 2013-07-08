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
using puck.core.Entities;
using puck.core.Filters;
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
                ViewBag.Error = "Incorrect Login Information";
                return View(user);
            }
            FormsAuthentication.SetAuthCookie(user.Username,user.PersistentCookie);
            return RedirectToAction("Index", "Api", new { area="admin"});
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
                if(meta!=null)
                    pu.StartNode =new List<PuckPicker>{JsonConvert.DeserializeObject(meta.Value,typeof(PuckPicker)) as PuckPicker};
                model.Add(pu);
            }
            return View(model);
        }

        [Auth]
        public ActionResult Edit(string userName=null) {
            var model = new PuckUser();
            if (!string.IsNullOrEmpty(userName)) {
                var usr = Membership.Providers["puck"].GetUser(userName, false);
                model.UserName = userName;
                model.Email = usr.Email;
                model.Password = usr.GetPassword();
                model.PasswordConfirm = model.Password;
                model.Roles = Roles.GetRolesForUser(userName).ToList();
                var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.UserStartNode && x.Key.Equals(usr.UserName)).FirstOrDefault();
                if(meta !=null)
                    model.StartNode = JsonConvert.DeserializeObject(meta.Value, typeof(List<PuckPicker>)) as List<PuckPicker>;                
            }
            return View(model);
        }

        [HttpPost]
        [Auth]
        public JsonResult Edit(PuckUser user,bool edit)
        {
            bool success = false;
            string message = "";
            var model = new PuckUser();
            try
            {
                if (!ModelState.IsValid)
                    throw new Exception("model invalid.");
                MembershipUser muser;
                if (!edit) {
                    MembershipCreateStatus mcs;
                    muser=Membership.Providers["puck"].CreateUser(user.UserName, user.Password, user.Email, null, null, true, null, out mcs);
                    if (muser == null) {
                        message = GetErrorMessage(mcs);
                        throw new Exception(message);
                    }
                }
                muser = Membership.Providers["puck"].GetUser(user.UserName, false);
                if (muser == null)
                    throw new Exception("could not find user for edit");
                muser.Email = user.Email;
                if (!string.IsNullOrEmpty(user.NewPassword)) {
                    muser.ChangePassword(user.Password, user.NewPassword);
                }

                var roles = Roles.GetRolesForUser(muser.UserName);
                if (roles.Length > 0)
                {
                    Roles.RemoveUserFromRoles(user.UserName, roles);
                }
                if(user.Roles!=null && user.Roles.Count>0){
                    Roles.AddUserToRoles(user.UserName, user.Roles.ToArray());
                }

                if (user.StartNode == null || user.StartNode.Count==0)
                {
                    repo.GetPuckMeta().Where(x => x.Name == DBNames.UserStartNode && x.Key.Equals(user.UserName)).ToList().ForEach(x => repo.DeleteMeta(x));
                }
                else {
                    var metas =  repo.GetPuckMeta().Where(x => x.Name == DBNames.UserStartNode && x.Key.Equals(user.UserName)).ToList();
                    PuckMeta meta = null;
                    if (metas.Count > 0) {
                        meta = metas.FirstOrDefault();
                        if (metas.Count > 1) {
                            metas.Where(x => x != meta).ToList().ForEach(x=>repo.DeleteMeta(x));
                        }                        
                    }
                    if (meta == null)
                    {
                        meta = new PuckMeta();
                        meta.Name = DBNames.UserStartNode;
                        meta.Key = user.UserName;
                        repo.AddMeta(meta);
                    }
                    meta.Value = JsonConvert.SerializeObject(user.StartNode.First());
                }
                repo.SaveChanges();
                success = true;
            }
            catch (Exception ex) {
                log.Log(ex);
                success = false;
                message = ex.Message;
            }
            return Json(new {success=success,message=message }, JsonRequestBehavior.AllowGet);
        }

        [Auth]
        public JsonResult Delete(string username) {
            bool success = false;
            string message = "";
            try
            {
                Membership.Providers["puck"].DeleteUser(username, true);
                repo.GetPuckMeta().Where(x => x.Name == DBNames.UserStartNode && x.Key.Equals(username)).ToList().ForEach(x => repo.DeleteMeta(x));
                repo.SaveChanges();
                success = true;
            }
            catch (Exception ex)
            {
                log.Log(ex);
                success = false;
                message = ex.Message;
            }
            return Json(new { success = success, message = message }, JsonRequestBehavior.AllowGet);
        }

        public string GetErrorMessage(MembershipCreateStatus status)
        {
            switch (status)
            {
                case MembershipCreateStatus.DuplicateUserName:
                    return "Username already exists. Please enter a different user name.";

                case MembershipCreateStatus.DuplicateEmail:
                    return "A username for that e-mail address already exists. Please enter a different e-mail address.";

                case MembershipCreateStatus.InvalidPassword:
                    return "The password provided is invalid. Please enter a valid password value.";

                case MembershipCreateStatus.InvalidEmail:
                    return "The e-mail address provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidAnswer:
                    return "The password retrieval answer provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidQuestion:
                    return "The password retrieval question provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidUserName:
                    return "The user name provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.ProviderError:
                    return "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                case MembershipCreateStatus.UserRejected:
                    return "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                default:
                    return "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
            }
        }
    }
}
