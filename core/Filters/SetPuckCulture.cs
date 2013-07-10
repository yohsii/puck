using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Threading;
using System.Globalization;
using puck.core.Constants;

namespace puck.core.Filters
{
    public class SetPuckCulture : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            string variant;
            if (filterContext.HttpContext.Session["language"] != null)
            {
                variant = filterContext.HttpContext.Session["language"] as string;
            }
            else {
                var repo = PuckCache.PuckRepo;
                var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.UserVariant && x.Key == filterContext.HttpContext.User.Identity.Name).FirstOrDefault();
                if (meta != null && !string.IsNullOrEmpty(meta.Value))
                {
                    variant = meta.Value;
                    filterContext.HttpContext.Session["language"] = meta.Value;
                }
                else {
                    variant = PuckCache.SystemVariant;
                }
            }
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(variant);
        }
    }
}
