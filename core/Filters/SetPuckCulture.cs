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
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(PuckCache.SystemVariant);
        }
    }
}
