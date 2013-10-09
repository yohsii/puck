using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Data.Entity;
using puck.core.Abstract;
using puck.core.Constants;
using puck.core.Concrete;
using System.Web.WebPages;
using StackExchange.Profiling;

namespace puck
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_EndRequest()
        {
#if DEBUG
            MiniProfiler.Stop(); //stop as early as you can, even earlier with MvcMiniProfiler.MiniProfiler.Stop(discardResults: true);
#endif
        }

        protected void Application_BeginRequest()
        {
#if DEBUG
            if (Request.IsLocal) { MiniProfiler.Start(); } //or any number of other checks, up to you 
#endif
        }
    
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            AuthConfig.RegisterAuth();
            
            DisplayModeProvider.Instance.Modes.Insert(0, new DefaultDisplayMode("iPhone")
            {
                ContextCondition = (context => context.GetOverriddenUserAgent().IndexOf
                    ("iPhone", StringComparison.OrdinalIgnoreCase) >= 0)
            });

            //initiate puck
            puck.core.Bootstrap.Ini();
            //register before index event
            PuckCache.PuckIndexer.RegisterBeforeIndexHandler<puck.core.Base.BaseModel>("before",(object o,puck.core.Events.BeforeIndexingEventArgs args)=>{
                //args.Cancel=true;                
            },true);
            
            //unregister
            //PuckCache.PuckIndexer.UnRegisterAfterIndexHandler("before");
            
            //register after index event
            PuckCache.PuckIndexer.RegisterAfterIndexHandler<puck.core.Base.BaseModel>("after", (object o, puck.core.Events.IndexingEventArgs args) =>
            {
                //var node = args.Node;                
            }, false);

        }
    }
}