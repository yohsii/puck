using puck.core.Abstract;
using puck.core.Controllers;
using System.Web.Caching;
using System.Threading;
using System.Globalization;
using System.Reflection;
using System.Diagnostics;
using puck.core.Filters;
using System.Web.Mvc;

namespace puck.Controllers
{
    public class HomeController : BaseController
    {
        public ActionResult Index()
        {
            return base.Puck();
        }
    }
}