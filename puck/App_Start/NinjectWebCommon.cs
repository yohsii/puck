[assembly: WebActivator.PreApplicationStartMethod(typeof(puck.App_Start.NinjectWebCommon), "Start")]
[assembly: WebActivator.ApplicationShutdownMethodAttribute(typeof(puck.App_Start.NinjectWebCommon), "Stop")]

namespace puck.App_Start
{
    using System;
    using System.Web;

    using Microsoft.Web.Infrastructure.DynamicModuleHelper;

    using Ninject;
    using Ninject.Web.Common;
    using System.Reflection;
    using puck.core.Abstract;
    using puck.core.Concrete;
    public static class NinjectWebCommon 
    {
        private static readonly Bootstrapper bootstrapper = new Bootstrapper();

        /// <summary>
        /// Starts the application
        /// </summary>
        public static void Start() 
        {
            DynamicModuleUtility.RegisterModule(typeof(OnePerRequestHttpModule));
            DynamicModuleUtility.RegisterModule(typeof(NinjectHttpModule));
            bootstrapper.Initialize(CreateKernel);
        }
        
        /// <summary>
        /// Stops the application.
        /// </summary>
        public static void Stop()
        {
            bootstrapper.ShutDown();
        }
        
        /// <summary>
        /// Creates the kernel that will manage your application.
        /// </summary>
        /// <returns>The created kernel.</returns>
        private static IKernel CreateKernel()
        {
            var kernel = new StandardKernel();
            kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);
            kernel.Bind<IHttpModule>().To<HttpApplicationInitializationHttpModule>();
            
            RegisterServices(kernel);
            return kernel;
        }

        /// <summary>
        /// Load your modules or register your services here!
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        private static void RegisterServices(IKernel kernel)
        {
            kernel.Load(Assembly.GetExecutingAssembly());
            kernel.Bind<I_Log>().To<Logger>().InSingletonScope();
            kernel.Bind<I_Puck_Repository>().To<Puck_Repository>().InRequestScope();
            kernel.Bind<I_Content_Indexer>().To<Content_Indexer_Searcher>().InSingletonScope();
            kernel.Bind<I_Content_Searcher>().ToMethod(x => x.Kernel.Get<I_Content_Indexer>() as I_Content_Searcher);
            kernel.Bind<I_Task_Dispatcher>().To<Dispatcher>().InSingletonScope();
            
        }        
    }
}
