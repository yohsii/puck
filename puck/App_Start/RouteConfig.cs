﻿using System;
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
                        
            //add any specific routes before the following catch-all
            routes.MapRoute(
                name: "puck",
                url: "{*path}",
                defaults: new {controller="Home",action="Index",path=UrlParameter.Optional}
                ,namespaces: new string[]{"puck.Controllers"}
            );
            
        }
    }
}