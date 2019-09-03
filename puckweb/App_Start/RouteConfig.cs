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
            routes.MapMvcAttributeRoutes();
            routes.MapRoute(
                name: "default",
                url: "{controller}/{action}"
                , defaults: new { controller = "Home", action = "Index"}
                , constraints: new { controller = "Debugging|Account" }
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
