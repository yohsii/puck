using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using puck.core.Models;
using Lucene.Net.Analysis;
using puck.core.Helpers;
using System.Web.Mvc;
using puck.core.Abstract;

namespace puck.core.Constants
{
    public static class PuckCache
    {
        public static List<Variant> Variants { get; set; }
        public static Dictionary<string,string> DomainRoots {get;set;}
        public static Dictionary<string, Analyzer> TypeAnalyzers { get; set; }
        public static HashSet<Analyzer> Analyzers { get; set; }
        public static Dictionary<string, string> Redirect { get; set; }

        public static void Ini() {
            var repo = DependencyResolver.Current.GetService<I_Puck_Repository>();
            
        }
    }
}
