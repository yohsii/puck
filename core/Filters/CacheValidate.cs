using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace puck.core.Filters
{
    public class CacheValidate : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            filterContext.HttpContext.Response.Cache.AddValidationCallback(ValidateCache, null);
        }


        private static void ValidateCache(HttpContext context, object data, ref HttpValidationStatus validationStatus)
        {
            // don't serve this page from cache (need to add some conditions here, to determine whether to use or not to use the outputcache)
            //if()
            validationStatus = HttpValidationStatus.Invalid;

            // additionally, ignore the [OutputCache] for this particular request
            //context.Response.Cache.SetNoServerCaching();
            //context.Response.Cache.SetNoStore();
        }
    }
}
