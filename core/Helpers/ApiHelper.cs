﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using puck.core.Base;
using System.Web;
using System.Threading.Tasks;
using puck.core.Abstract;
using System.Web.Mvc;
using puck.core.Concrete;
using System.Text.RegularExpressions;
using puck.core.Models;
using puck.core.Constants;
using System.Globalization;

namespace puck.core.Helpers
{
    public class ApiHelper
    {
        public static I_Puck_Repository repo { get {
            return DependencyResolver.Current.GetService<I_Puck_Repository>();
        } }

        public static List<string> FieldGroups(string type=null) {
            var result = new List<string>();
            var fieldGroups = repo.GetPuckMeta().Where(x => x.Name.StartsWith(DBNames.FieldGroups)).ToList();
            fieldGroups.ForEach(x =>
            {
                string typeName = x.Name.Replace(DBNames.FieldGroups, "");
                string groupName = x.Key;
                string FieldName = x.Value;
                result.Add(string.Concat(typeName, ":", groupName, ":", FieldName));
            });
            if (!string.IsNullOrEmpty(type)) {
                var targetType = Type.GetType(type);
                var baseTypes = BaseTypes(targetType);
                baseTypes.Add(targetType);
                result = result
                    .Where(x => baseTypes
                        .Any(xx => xx.AssemblyQualifiedName.Equals(x.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[0])))
                        .ToList();
            }
            return result;
        }

        public static List<Variant> Variants() {
            var allVariants = AllVariants();
            var results = new List<Variant>();
            //var languageMeta = repo.GetPuckMeta().Where(x => x.Name == DBNames.Settings && x.Key == DBKeys.DefaultLanguage).FirstOrDefault();
            var allLanguageMetas = repo.GetPuckMeta().Where(x => x.Name == DBNames.Settings && x.Key== DBKeys.Languages).ToList();
            /*if (languageMeta != null){
                allLanguageMetas.Insert(0,languageMeta);
            }*/
            for(var i =0;i<allLanguageMetas.Count;i++) {
                var language = allLanguageMetas[i];
                if (language != null)
                {
                    var variant = allVariants.Where(x => x.Key.ToLower().Equals(language.Value.ToLower())).FirstOrDefault();
                    if (variant != null)
                    {
                        variant.IsDefault = i==0;
                        results.Add(variant);
                    }
                }
            }
            return results;
        }
        public static List<Variant> AllVariants() {
            var results = new List<Variant>();
            foreach (var ci in CultureInfo.GetCultures(CultureTypes.AllCultures))
            {
                string specName = "(none)";
                try
                {
                    specName = CultureInfo.CreateSpecificCulture(ci.Name).Name;
                }
                catch { }
                results.Add(new Variant { FriendlyName=ci.EnglishName,IsDefault=false,Key=ci.Name.ToLower()});
            }
            return results;
        }
        public static IEnumerable<Type> FindDerivedClasses(Type baseType, List<Type> excluded=null,bool inclusive=false) {
            excluded = excluded ?? new List<Type>();
            var types=AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(x =>(x != baseType || inclusive) && baseType.IsAssignableFrom(x) && !excluded.Contains(x));
            return types;
        }
        public static string DirOfPath(string s) {
            if (s.EndsWith("/"))
                return s;
            string result =s.Substring(0,s.LastIndexOf("/")+1);
            return result;
        }
        public static string ToVirtualPath(string p) {
            Regex r = new Regex(Regex.Escape(HttpContext.Current.Server.MapPath("~/")), RegexOptions.Compiled);
            p = r.Replace(p, "~/", 1).Replace("\\","/");
            return p;
        }
        public static List<FileInfo> Views(string[] excludePaths=null) {
            if (excludePaths==null)
                excludePaths= new string[]{};
            for (var i = 0; i < excludePaths.Length; i++) {
                excludePaths[i] = HttpContext.Current.Server.MapPath(excludePaths[i]);
            }
            var templateDirPath =HttpContext.Current.Server.MapPath("~/Views");
            var viewFiles = new DirectoryInfo(templateDirPath).EnumerateFiles("*.cshtml", SearchOption.AllDirectories)
                .Where(x=>!excludePaths.Any(y=>x.FullName.ToLower().StartsWith(y.ToLower())))
                .ToList();
            return viewFiles;
        }
        public static string TypeChain(Type type, string chain = "")
        {
            chain += type.FullName + " ";
            if (type.BaseType != null)
                chain = TypeChain(type.BaseType, chain);
            return chain.TrimEnd();
        }
        public static List<Type> BaseTypes(Type start,List<Type> result=null,bool excludeSystemObject = true) {
            result = result ?? new List<Type>();
            if (start.BaseType == null)
                return result;
            if (start.BaseType != typeof(Object) || !excludeSystemObject)
                result.Add(start.BaseType);
            return BaseTypes(start.BaseType,result);
        }
        public static void SetCulture(string path = null) {
            if (path == null)
                path = HttpContext.Current.Request.Url.AbsolutePath;
        }

        public static List<I_Puck_Task> Tasks { get {
            var result = new List<I_Puck_Task>();
            //get tasks from db
            return result;
        }}
        
        public static List<Type> Models(bool inclusive=false) { 
            return FindDerivedClasses(typeof(BaseModel),null,inclusive).ToList();
        }
        

    }
}