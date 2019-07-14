﻿using System;
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
                url: "debugging/{action}/{id}",
                defaults: new { controller = "debugging", action = "Index", id = UrlParameter.Optional }
            );

            /*routes.MapRoute(
                name: "signup",
                url: "signup/{action}/{id}",
                defaults: new { controller = "signup", action = "Index", id = UrlParameter.Optional }
            );*/
            //add any specific routes before the following catch-all
            routes.MapRoute(
                name: "puck",
                url: "{*path}",
                defaults: new { controller = "Home", action = "Index", path = UrlParameter.Optional }
                , namespaces: new string[] { "puck.Controllers" }
            );
        }
    }
}