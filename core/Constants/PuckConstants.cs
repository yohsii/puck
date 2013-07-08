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
namespace puck.core.Constants
{
    public static class PuckRoles
    {
        public static string Create = "create";
        public static string Delete = "delete";
        public static string Unpublish = "unpublish";
        public static string Localisation = "localisation";
        public static string Domain = "domain";
        public static string Revert = "revert";
        public static string Cache = "cache";
        public static string Settings = "settings";
        public static string Tasks = "tasks";
        public static string Users = "users";
        public static string Publish = "publish";
    }
    public static class FieldKeys
    {
        public static string PuckDefaultField = "";
        public static string PuckValue = "_puckvalue";
        public static string PuckTypeChain = "typechain";
        public static string PuckType = "type";
        public static string ID = "id";
        public static string Path = "path";
        public static string Variant = "variant";
        public static string TemplatePath = "templatepath";
    }
    public static class DBNames
    {
        public static string EditorSettings = "editorsettings";
        public static string Redirect301 = "redirect301:";
        public static string Redirect302 = "redirect302:";
        public static string PathToLocale = "pathtolocale";
        public static string Settings = "settings";
        public static string FieldGroups = "fieldgroups:";
        public static string DomainMapping = "domainmapping";
        public static string TypeAllowedTypes = "typeallowedtypes";
        public static string Tasks = "task";
        public static string CachePolicy = "cache";
        public static string CacheExclude = "cacheexclude";
        public static string UserStartNode = "userstartnode";
    }
    public static class DBKeys
    {
        public static string ObjectCacheMinutes = "objectcachemin";
        public static string Languages = "languages";
        public static string DefaultLanguage = "defaultlanguage";
        public static string EnableLocalePrefix = "enablelocaleprefix";
    }
    public static class CacheKeys {
        public static string PrefixTemplateExist = "fexist:";
    }    
    public static class FieldSettings
    {
        public static Dictionary<Type, Type> DefaultPropertyTransformers = new Dictionary<Type, Type>
        {
            {typeof(DateTime),typeof(DateTransformer)}
        };
        public static Field.Index FieldIndexSetting = Field.Index.NOT_ANALYZED;
        public static Field.Store FieldStoreSetting = Field.Store.YES;

    }
    public static class PuckCache
    {
        public static string Path404 = string.IsNullOrEmpty(ConfigurationManager.AppSettings["Puck404Path"]) ? "~/views/Puck404.cshtml" : ConfigurationManager.AppSettings["Puck404Path"];
        public static string Path500 = string.IsNullOrEmpty(ConfigurationManager.AppSettings["Puck500Path"]) ? "~/views/Puck500.cshtml" : ConfigurationManager.AppSettings["Puck500Path"];
        public static bool Debug = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["PuckDebug"]) && ConfigurationManager.AppSettings["PuckDebug"].ToLower() == bool.TrueString.ToLower();
        public static bool UpdateTaskLastRun = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["PuckUpdateTaskLastRun"]) && ConfigurationManager.AppSettings["PuckUpdateTaskLastRun"].ToLower() == bool.TrueString.ToLower();
        public static bool UpdateRecurringTaskLastRun = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["PuckUpdateRecurringTaskLastRun"]) && ConfigurationManager.AppSettings["PuckUpdateRecurringTaskLastRun"].ToLower() == bool.TrueString.ToLower();
        public static bool TaskCatchUp = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["PuckTaskCatchUp"]) && ConfigurationManager.AppSettings["PuckTaskCatchUp"].ToLower() == bool.TrueString.ToLower();
        public static int RedirectOuputCacheMinutes = 10;
        public static int DefaultOutputCacheMinutes = 0;
        public static string SystemVariant = "en-GB";
        public static List<Variant> Variants { get; set; }
        public static Dictionary<string,string> DomainRoots {get;set;}
        public static Dictionary<string, string> PathToLocale { get; set; }
        public static Dictionary<string, Analyzer> TypeAnalyzers { get; set; }
        public static Dictionary<string, string> Redirect301 { get; set; }
        public static Dictionary<string, string> Redirect302 { get; set; }
        public static Dictionary<string, int> TypeOutputCache { get; set; }
        public static HashSet<string> OutputCacheExclusion { get; set; }
        public static IKernel NinjectKernel { get; set; }
        public static I_Puck_Repository PuckRepo { get { return NinjectKernel.Get<I_Puck_Repository>("T"); } }
        public static I_Log PuckLog { get { return NinjectKernel.Get<I_Log>(); } }
        public static List<Analyzer> Analyzers { get; set; }
        public static Dictionary<Type, Analyzer> AnalyzerForModel { get; set; }
    }

    
}
