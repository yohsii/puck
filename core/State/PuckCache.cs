using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using puck.core.Models;
using Lucene.Net.Analysis;
using puck.core.Helpers;
using System.Web.Mvc;
using puck.core.Abstract;
using Lucene.Net.Documents;
using puck.core.Attributes;
using System.Configuration;
using Ninject;
using puck.core.Identity;
using puck.core.Services;

namespace puck.core.State
{
    public static class PuckCache
    {
        public static string SmtpFrom = "";
        public static string SmtpHost = "localhost";
        public static string EmailTemplatePublishPath = "~/app_data/notification_publish_template.txt";
        public static string EmailTemplateEditPath = "~/app_data/notification_edit_template.txt";
        public static string EmailTemplateDeletePath = "~/app_data/notification_delete_template.txt";
        public static string EmailTemplateMovePath = "~/app_data/notification_move_template.txt";
        public static string TemplateDirectory = "~/views/";
        public static string Path404 = string.IsNullOrEmpty(ConfigurationManager.AppSettings["Puck404Path"]) ? "~/views/Errors/Puck404.cshtml" : ConfigurationManager.AppSettings["Puck404Path"];
        public static string Path500 = string.IsNullOrEmpty(ConfigurationManager.AppSettings["Puck500Path"]) ? "~/views/Errors/Puck500.cshtml" : ConfigurationManager.AppSettings["Puck500Path"];
        public static bool Debug = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["PuckDebug"]) && ConfigurationManager.AppSettings["PuckDebug"].ToLower() == bool.TrueString.ToLower();
        public static bool UpdateTaskLastRun = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["PuckUpdateTaskLastRun"]) && ConfigurationManager.AppSettings["PuckUpdateTaskLastRun"].ToLower() == bool.TrueString.ToLower();
        public static bool UpdateRecurringTaskLastRun = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["PuckUpdateRecurringTaskLastRun"]) && ConfigurationManager.AppSettings["PuckUpdateRecurringTaskLastRun"].ToLower() == bool.TrueString.ToLower();
        public static bool TaskCatchUp = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["PuckTaskCatchUp"]) && ConfigurationManager.AppSettings["PuckTaskCatchUp"].ToLower() == bool.TrueString.ToLower();
        public static int RedirectOuputCacheMinutes = 1;
        public static int DefaultOutputCacheMinutes = 0;
        public static int DisplayModesCacheMinutes = 10;
        public static int MaxSyncInstructions = 100;
        public static string SystemVariant = "en-GB";
        public static Uri FirstRequestUrl = null;
        public static bool IsRepublishingEntireSite { get; set; }
        public static bool ShouldSync { get; set; }
        public static bool IsSyncQueued { get; set; }
        public static string IndexingStatus { get; set; }
        public static List<Variant> Variants { get; set; }
        public static Dictionary<string, string> DomainRoots { get; set; }
        public static Dictionary<string, string> PathToLocale { get; set; }
        public static Dictionary<string, Analyzer> TypeAnalyzers { get; set; }
        public static Dictionary<string, string> Redirect301 { get; set; }
        public static Dictionary<string, string> Redirect302 { get; set; }
        public static Dictionary<string, int> TypeOutputCache { get; set; }
        public static Dictionary<string, Type> IGeneratedToModel { get; set; }
        public static Dictionary<string, Dictionary<string, string>> TypeFields { get; set; }
        //map model type fullname to asssembly qualified name
        public static Dictionary<string, string> ModelNameToAQN { get; set; }
        public static Dictionary<string, CropInfo> CropSizes { get; set; }
        public static HashSet<string> OutputCacheExclusion { get; set; }
        public static IKernel NinjectKernel { get; set; }
        public static I_Task_Dispatcher PuckDispatcher { get { return NinjectKernel.Get<I_Task_Dispatcher>(); } }
        public static I_Content_Searcher PuckSearcher { get { return NinjectKernel.Get<I_Content_Searcher>(); } }
        public static I_Content_Indexer PuckIndexer { get { return NinjectKernel.Get<I_Content_Indexer>(); } }
        public static I_Puck_Repository PuckRepo { get { return NinjectKernel.Get<I_Puck_Repository>(); } }
        public static PuckUserManager PuckUserManager { get { return NinjectKernel.Get<PuckUserManager>(); } }
        public static PuckRoleManager PuckRoleManager { get { return NinjectKernel.Get<PuckRoleManager>(); } }
        public static ApiHelper ApiHelper { get { return NinjectKernel.Get<ApiHelper>(); } }
        public static ContentService ContentService { get { return NinjectKernel.Get<ContentService>(); } }
        public static I_Log PuckLog { get { return NinjectKernel.Get<I_Log>(); } }
        public static List<Analyzer> Analyzers { get; set; }
        public static Dictionary<Type, Analyzer> AnalyzerForModel { get; set; }

    }
}
