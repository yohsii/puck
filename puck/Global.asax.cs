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

namespace puck
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<puck.core.Entities.PuckContext,puck.core.Migrations.Configuration>());
            
            AreaRegistration.RegisterAllAreas();
            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            AuthConfig.RegisterAuth();

            //DataAnnotationsModelValidatorProvider.AddImplicitRequiredAttributeForValueTypes = false;

            /*
            Content_Indexer_Searcher.RegisterBeforeIndexHandler<puck.areas.admin.ViewModels.Home>("doshit"
                ,(object o,puck.core.Events.BeforeIndexingEventArgs args) => { 
                    
                }
                ,true
                );
            */
            puck.core.Bootstrap.Ini();
        }
    }
}