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
namespace puck.core.Constants
{
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
        public static string Redirect = "redirect";
        public static string PathToLocale = "pathtolocale";
        public static string Settings = "settings";
        public static string FieldGroups = "fieldgroups:";
        public static string DomainMapping = "domainmapping";
        public static string TypeAllowedTypes = "typeallowedtypes";
        public static string Tasks = "task";
    }
    public static class DBKeys
    {
        public static string Languages = "languages";
        public static string DefaultLanguage = "defaultlanguage";
        public static string EnableLocalePrefix = "enablelocaleprefix";
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
        public static bool Debug = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["PuckDebug"]) && ConfigurationManager.AppSettings["PuckDebug"].ToLower() == bool.TrueString.ToString() ? true : false;
        public static string SystemVariant = "en-GB";
        public static List<Variant> Variants { get; set; }
        public static Dictionary<string,string> DomainRoots {get;set;}
        public static Dictionary<string, string> PathToLocale { get; set; }
        public static Dictionary<string, Analyzer> TypeAnalyzers { get; set; }
        public static HashSet<Analyzer> Analyzers { get; set; }
        public static Dictionary<string, string> Redirect { get; set; }
        
    }

    
}
