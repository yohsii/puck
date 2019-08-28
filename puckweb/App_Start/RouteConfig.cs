using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace puckweb
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "debugging",
                url: "{controller}/{action}/{id}"
                , defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
                , constraints: new { controller = "Debugging|Signup|Account" }
            );

            //add any specific routes before the following catch-all. to route to a specific controller, add the controller name to the constraints, separated with pipe |
            routes.MapRoute(
                name: "puck",
                url: "{*path}",
                defaults: new { controller = "Home", action = "Index", path = UrlParameter.Optional }
                
            );
        }
    }
}
