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
using puck.core.Identity;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.AspNet.Identity;

namespace puck.core.Controllers
{
    public class AdminController : Controller
    {
        I_Content_Indexer indexer;
        I_Content_Searcher searcher;
        I_Log log;
        I_Puck_Repository repo;
        PuckRoleManager roleManager;
        PuckUserManager userManager;
        PuckSignInManager signInManager;
        IAuthenticationManager authenticationManager;
        public AdminController(I_Content_Indexer i, I_Content_Searcher s, I_Log l, I_Puck_Repository r,PuckRoleManager rm,PuckUserManager um, PuckSignInManager sm,IAuthenticationManager authenticationManager) {
            this.indexer = i;
            this.searcher = s;
            this.log = l;
            this.repo = r;
            this.roleManager = rm;
            this.userManager = um;
            this.signInManager = sm;
            this.authenticationManager = authenticationManager;
        }

        [HttpGet]
        public ActionResult In() {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> In(puck.core.Models.LogIn user,string returnUrl) {
            var result = await this.signInManager.PasswordSignInAsync(user.Username, user.Password, user.PersistentCookie, shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:
                    if (!string.IsNullOrEmpty(returnUrl))
                        return Redirect(returnUrl);
                    else
                        return RedirectToAction("Index", "api", new { area = "admin" });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    ViewBag.Error = "Incorrect Login Information";
                    return View(user);
            }            
            
        }

        public ActionResult Out() {
            Session.Abandon();
            authenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("In");
        }

        //TODO renew ticket to stay signed in while using cms
        public ActionResult Renew() {
            return View();
        }

        [Auth(Roles =PuckRoles.Users)]
        public ActionResult Index()
        {
            var model = new List<PuckUserViewModel>();
            var puckRole = roleManager.FindByName(PuckRoles.Puck);
            var userCollection = repo.GetPuckUser().Where(x => x.Roles.Any(xx => xx.RoleId == puckRole.Id)).ToList();
            
            foreach (PuckUser pu in userCollection) {
                var puvm = new PuckUserViewModel();
                puvm.User = pu;
                puvm.Roles = userManager.GetRoles(pu.Id).ToList();
                if(pu.StartNodeId!=Guid.Empty)
                    puvm.StartNode =new List<PuckPicker>{ new PuckPicker {Id=pu.StartNodeId } };
                puvm.UserVariant = pu.UserVariant;
                
                model.Add(puvm);
            }
            return View(model);
        }

        [Auth(Roles =PuckRoles.Users)]
        public ActionResult Edit(string userName=null) {
            var model = new PuckUserViewModel();
            if (!string.IsNullOrEmpty(userName)) {
                var usr = userManager.FindByName(userName);
                model.UserName = userName;
                model.Email = usr.Email;
                model.CurrentEmail = usr.Email;
                //model.Password = usr.GetPassword();
                //model.PasswordConfirm = model.Password;
                model.Roles = userManager.GetRoles(usr.Id).ToList();
                if(usr.StartNodeId!=Guid.Empty)
                    model.StartNode = new List<PuckPicker>{ new PuckPicker { Id=usr.StartNodeId} };
                model.UserVariant = usr.UserVariant;
            }
            return View(model);
        }

        [HttpPost]
        [Auth(Roles=PuckRoles.Users)]
        public async Task<JsonResult> Edit(PuckUserViewModel user,bool edit)
        {
            bool success = false;
            string message = "";
            string startPath = "/";
            Guid startNodeId = Guid.Empty;
            var model = new PuckUserViewModel();
            try
            {
                if (!ModelState.IsValid)
                    throw new Exception("model invalid.");
                if (!edit) {
                    if (string.IsNullOrEmpty(user.Password))
                        throw new Exception("please enter a password");

                    var puser = new PuckUser
                    {
                        Email = user.Email,
                        UserName = user.UserName,
                        UserVariant = user.UserVariant,
                        StartNodeId = user.StartNode?.FirstOrDefault()?.Id ?? Guid.Empty
                    };
                    var result = userManager.Create(puser, user.Password);
                    if (!result.Succeeded) {
                        message = string.Join(" ",result.Errors);
                        throw new Exception(message);
                    }
                    if (user.Roles != null && user.Roles.Count > 0)
                    {
                        userManager.AddToRoles(puser.Id, user.Roles.ToArray());                        
                    }
                    if (!userManager.IsInRole(puser.Id, PuckRoles.Puck))
                    {
                        userManager.AddToRole(puser.Id, PuckRoles.Puck);
                    }
                    success = true;
                }
                else
                {
                    var puser = userManager.FindByEmail(user.CurrentEmail);
                    if (puser == null)
                        throw new Exception("could not find user for edit");

                    if (!puser.Email.Equals(user.Email))
                    {
                        puser.Email = user.Email;
                    }
                    if (!puser.UserName.Equals(user.UserName))
                    {
                        puser.UserName = user.UserName;
                    }

                    var roles = userManager.GetRoles(puser.Id).ToList();
                    //never remove Puck role
                    if (roles != null && roles.Contains(PuckRoles.Puck))
                    {
                        roles.RemoveAll(x => x.Equals(PuckRoles.Puck));                        
                    }
                    if (roles.Count > 0)
                    {
                        userManager.RemoveFromRoles(puser.Id, roles.ToArray());
                    }
                    if (user.Roles != null && user.Roles.Count > 0)
                    {
                        if (user.Roles.Count > 0) {
                            var rolesToAdd = user.Roles.Where(x => x != PuckRoles.Puck).ToArray();
                            userManager.AddToRoles(puser.Id, rolesToAdd);
                        }
                    }
                    if (!userManager.IsInRole(puser.Id,PuckRoles.Puck))
                    {
                        userManager.AddToRole(puser.Id, PuckRoles.Puck);                        
                    }
                    
                    if (user.StartNode == null || user.StartNode.Count == 0)
                    {
                        puser.StartNodeId = Guid.Empty;
                    }
                    else
                    {
                        Guid picked_id = user.StartNode.First().Id;
                        var revision = repo.GetPuckRevision().Where(x => x.Id == picked_id && x.Current).FirstOrDefault();
                        if (revision != null)
                            startPath = revision.Path + "/";
                        puser.StartNodeId = picked_id;
                    }
                    if (!string.IsNullOrEmpty(user.UserVariant))
                    {
                        puser.UserVariant = user.UserVariant;
                    }
                    userManager.Update(puser);

                    if (!string.IsNullOrEmpty(user.Password))
                    {
                        var token = await userManager.GeneratePasswordResetTokenAsync(puser.Id);
                        var result = await userManager.ResetPasswordAsync(puser.Id, token, user.Password);
                    }
                    startNodeId = puser.StartNodeId;
                    success = true;
                }

                
            }
            catch (Exception ex) {
                log.Log(ex);
                success = false;
                message = ex.Message;
            }
            return Json(new {success=success,message=message,startPath=startPath,startNodeId=startNodeId }, JsonRequestBehavior.AllowGet);
        }

        [Auth(Roles =PuckRoles.Users)]
        public JsonResult Delete(string username) {
            bool success = false;
            string message = "";
            try
            {
                if (username == User.Identity.Name)
                    throw new Exception("you cannot delete your own user");
                var puser = userManager.FindByName(username);
                if (puser == null)
                    throw new Exception("user not found");
                userManager.Delete(puser);    
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
    }
}
