using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace puck
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "admin",
                url: "admin/{controller}/{action}/{id}",
                defaults: new {controller = "Home", action = "Index",id=UrlParameter.Optional}
                ,namespaces:new string[]{"puck.core.Controllers"}    
            );
            
            routes.MapRoute(
                name: "default",
                url: "{*path}",
                defaults: new {controller="Home",action="Index",path=UrlParameter.Optional}
                ,namespaces: new string[]{"puck.Controllers"}
            );

            /*
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
                , namespaces: new string[] { "puck.Controllers" }
            );
            */
        }
    }
}