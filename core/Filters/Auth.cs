using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace puck.core.Filters
{
    public class Auth:AuthorizeAttribute
    {
        private string LoginUrl;
        //TODO check current user has access to content requested
        private bool CheckPath(string path) {
            return true;
        }
        protected override bool AuthorizeCore(System.Web.HttpContextBase httpContext)
        {
            return base.AuthorizeCore(httpContext) && CheckPath("");
        }
        protected override void HandleUnauthorizedRequest(AuthorizationContext context) {
            UrlHelper urlHelper = new UrlHelper(context.RequestContext);
            if (context.HttpContext.Request.IsAuthenticated)
            {
                context.Result = new JsonResult
                {
                    Data = new
                    {
                        Error = "NotAuthorized",
                        LogOnUrl = urlHelper.Action("In","Account")
                    }, JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }
            else {
                context.RequestContext.HttpContext.Response.Redirect(urlHelper.Action("In", "Account"), true);
            }
        }
    }
}
