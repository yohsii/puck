using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using puck.core.Constants;
using puck.core.Base;
using Newtonsoft.Json;
using puck.core.Abstract;
using puck.core.Controllers;
using System.Web.Caching;
using System.Threading;
using System.Globalization;
using System.Reflection;
using System.Diagnostics;
using puck.core.Filters;
using StackExchange.Profiling;
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
