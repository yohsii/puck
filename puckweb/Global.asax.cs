using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Ninject;
using Ninject.Web.Common.WebHost;
using puck.core.Abstract;
using puck.core.Concrete;
using puck.core.Constants;
using puck.core.Entities;
using puck.core.Identity;
using StackExchange.Profiling;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.WebPages;

namespace puckweb
{
    public class MvcApplication : NinjectHttpApplication //System.Web.HttpApplication
    {
        /*
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }*/
        protected override IKernel CreateKernel()
        {
            var kernel = new StandardKernel();
            RegisterServices(kernel);
            return kernel;
        }

        /// <summary>
        /// Load your modules or register your services here!
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        private void RegisterServices(IKernel kernel)
        {
            // e.g. kernel.Load(Assembly.GetExecutingAssembly());
            kernel.Load(Assembly.GetExecutingAssembly());
            kernel.Bind<I_Log>().To<Logger>().InSingletonScope();
            //kernel.Bind<I_Puck_Repository>().To<Puck_Repository>().WhenInjectedInto<IController>().InRequestScope();
            //kernel.Bind<I_Puck_Repository>().To<Puck_Repository>().InRequestScope().Named("R");
            //kernel.Bind<I_Puck_Repository>().To<Puck_Repository>().InTransientScope().Named("T");
            kernel.Bind<I_Puck_Repository>().To<Puck_Repository>().InTransientScope();
            kernel.Bind<I_Content_Indexer>().To<Content_Indexer_Searcher>().InSingletonScope();
            kernel.Bind<I_Content_Searcher>().ToMethod(x => x.Kernel.Get<I_Content_Indexer>() as I_Content_Searcher);
            kernel.Bind<I_Task_Dispatcher>().To<Dispatcher>().InSingletonScope();

            kernel.Bind<PuckRoleManager>().ToMethod(context =>
            {
                var cbase = new HttpContextWrapper(HttpContext.Current);
                return cbase.GetOwinContext().Get<PuckRoleManager>();
            });

            kernel.Bind<PuckUserManager>().ToMethod(context =>
            {
                var userManager = new PuckUserManager(new UserStore<PuckUser>(new PuckContext()));
                //var cbase = new HttpContextWrapper(HttpContext.Current);
                //return cbase.GetOwinContext().Get<PuckUserManager>();
                return userManager;
            });

            kernel.Bind<PuckSignInManager>().ToMethod(context =>
            {
                var cbase = new HttpContextWrapper(HttpContext.Current);
                return cbase.GetOwinContext().Get<PuckSignInManager>();
            });

            kernel.Bind<IAuthenticationManager>().ToMethod(context => {
                var cbase = new HttpContextWrapper(HttpContext.Current);
                return cbase.GetOwinContext().Authentication;
            });
            PuckCache.NinjectKernel = kernel;
        }
        protected override void OnApplicationStarted()
        {
            base.OnApplicationStarted();
            SetUpMiniProfilter();
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            DisplayModeProvider.Instance.Modes.Insert(0, new DefaultDisplayMode("iPhone")
            {
                ContextCondition = (context => context.GetOverriddenUserAgent()==null?false
                : context.GetOverriddenUserAgent().IndexOf("iPhone", StringComparison.OrdinalIgnoreCase) >= 0)
            });

            //initiate puck
            puck.core.Bootstrap.Ini();
            //register before index event
            PuckCache.PuckIndexer.RegisterBeforeIndexHandler<puck.core.Base.BaseModel>("before", (object o, puck.core.Events.BeforeIndexingEventArgs args) => {
                //args.Cancel=true;                
            }, true);

            //unregister
            //PuckCache.PuckIndexer.UnRegisterAfterIndexHandler("before");

            //register after index event
            PuckCache.PuckIndexer.RegisterAfterIndexHandler<puck.core.Base.BaseModel>("after", (object o, puck.core.Events.IndexingEventArgs args) =>
            {
                //var node = args.Node;                
            }, false);

        }

        protected void Application_BeginRequest()
        {
            if (Request.IsLocal) // Example of conditional profiling, you could just call MiniProfiler.StartNew();
            {
                MiniProfiler.StartNew();
            }
        }

        protected void Application_EndRequest()
        {
            MiniProfiler.Current?.Stop(); // Be sure to stop the profiler!
        }
        protected void SetUpMiniProfilter()
        {
            MiniProfiler.Configure(new MiniProfilerOptions
            {
                // Sets up the route to use for MiniProfiler resources:
                // Here, ~/profiler is used for things like /profiler/mini-profiler-includes.js
                RouteBasePath = "~/profiler",

                // Example of using SQLite storage instead
                //Storage = new SqliteMiniProfilerStorage(ConnectionString),

                PopupRenderPosition = RenderPosition.Right,  // defaults to left
                PopupMaxTracesToShow = 10,                   // defaults to 15

                // ResultsAuthorize (optional - open to all by default):
                // because profiler results can contain sensitive data (e.g. sql queries with parameter values displayed), we
                // can define a function that will authorize clients to see the JSON or full page results.
                // we use it on http://stackoverflow.com to check that the request cookies belong to a valid developer.
                ResultsAuthorize = request => request.IsLocal,

                // ResultsListAuthorize (optional - open to all by default)
                // the list of all sessions in the store is restricted by default, you must return true to allow it
                ResultsListAuthorize = request =>
                {
                    // you may implement this if you need to restrict visibility of profiling lists on a per request basis
                    return true; // all requests are legit in this example
                },

                // Stack trace settings
                StackMaxLength = 256, // default is 120 characters

                // (Optional) You can disable "Connection Open()", "Connection Close()" (and async variant) tracking.
                // (defaults to true, and connection opening/closing is tracked)
                TrackConnectionOpenClose = true
            });
        }
    }
}
