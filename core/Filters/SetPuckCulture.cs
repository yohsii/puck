using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Threading;
using System.Globalization;
using puck.core.Constants;
using puck.core.Helpers;

namespace puck.core.Filters
{
    public class SetPuckCulture : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            string variant = ApiHelper.UserVariant();
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(variant);
        }
    }
}
