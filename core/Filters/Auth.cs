using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Newtonsoft.Json;
using puck.core.Abstract;
using puck.core.Constants;
using puck.core.Models;

namespace puck.core.Filters
{
    public class Auth:AuthorizeAttribute
    {
        private I_Puck_Repository repo = PuckCache.PuckRepo;
        
        private bool CheckPath(string path,string username) {
            var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.UserStartNode && x.Key == username).FirstOrDefault();
            if (meta != null) { 
                var puckpick = JsonConvert.DeserializeObject(meta.Value,typeof(PuckPicker)) as PuckPicker;
                if (puckpick != null) {
                    var startNode = repo.GetPuckRevision().Where(x=>x.Id==puckpick.Id).FirstOrDefault();
                    if (startNode != null) {
                        return path.ToLower().StartsWith(startNode.Path.ToLower());
                    }
                }
            }
            return true;
        }
        protected override bool AuthorizeCore(System.Web.HttpContextBase httpContext)
        {
            bool result = base.AuthorizeCore(httpContext);
            if (httpContext.Request.QueryString.AllKeys.Contains("p_path"))
            {
                string username = httpContext.User.Identity.Name;
                result = result && CheckPath(httpContext.Request["p_path"],username);
            }
            return result;
        }
        protected override void HandleUnauthorizedRequest(AuthorizationContext context) {
            UrlHelper urlHelper = new UrlHelper(context.RequestContext);
            if (context.HttpContext.Request.IsAjaxRequest())
            {
                context.Result = new JsonResult
                {
                    Data = new
                    {
                        succes=false,
                        message = "Not authorized - log back in",
                        LogOnUrl = urlHelper.Action("In","Account")
                    }, JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }
            else {
                context.RequestContext.HttpContext.Response.Redirect(urlHelper.Action("In", "Admin"), true);
            }
        }
    }
}
