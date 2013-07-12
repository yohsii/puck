using System;
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
using Newtonsoft.Json;
using Ninject;
using puck.core.Entities;
namespace puck.core.Helpers
{
    public class ApiHelper
    {
        private static readonly object _savelck = new object();

        public static I_Puck_Repository Repo { get {
            return PuckCache.PuckRepo;
        } }
        public static I_Task_Dispatcher tdispatcher{get{
            return PuckCache.PuckDispatcher;
        }}
        public static I_Content_Indexer indexer{get{
            return PuckCache.PuckIndexer;
         }}


        public static object RevisionToModel(PuckRevision revision) {
            var model = JsonConvert.DeserializeObject(revision.Value, Type.GetType(revision.Type));
            var mod = model as BaseModel;
            mod.Path = revision.Path; mod.SortOrder = revision.SortOrder; mod.NodeName = revision.NodeName; mod.Published = revision.Published;
            return model;
        }
        public static BaseModel RevisionToBaseModel(PuckRevision revision)
        {
            var model = JsonConvert.DeserializeObject(revision.Value, Type.GetType(revision.Type));
            var mod = model as BaseModel;
            mod.Path = revision.Path; mod.SortOrder = revision.SortOrder; mod.NodeName = revision.NodeName; mod.Published = revision.Published;
            return mod;
        }
        public static void Sort(string path, List<string> paths) {
            lock (_savelck)
            {
                var repo = Repo;
                var qh = new QueryHelper<BaseModel>();
                var indexItems = qh.Directory(path).GetAll();
                var dbItems = repo.CurrentRevisionsByPath(path).ToList();
                indexItems.ForEach(n =>
                {
                    for (var i = 0; i < paths.Count; i++)
                    {
                        if (paths[i].ToLower().Equals(n.Path.ToLower()))
                        {
                            n.SortOrder = i;
                        }
                    }
                });
                dbItems.ForEach(n =>
                {
                    for (var i = 0; i < paths.Count; i++)
                    {
                        if (paths[i].ToLower().Equals(n.Path.ToLower()))
                        {
                            n.SortOrder = i;
                        }
                    }
                });
                repo.SaveChanges();
                indexer.Index(indexItems);
            }
        }
        public static void SetDomain(string path, string domains) {
            if (string.IsNullOrEmpty(path))
                throw new Exception("path null or empty");
            var repo = Repo;
            var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.DomainMapping).ToList();

            if (string.IsNullOrEmpty(domains))
            {
                var m = meta.Where(x => x.Key == path).ToList();
                m.ForEach(x =>
                {
                    repo.DeleteMeta(x);
                });
                if (m.Count > 0)
                    repo.SaveChanges();
            }
            else
            {
                var d = domains.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                d.ForEach(dd =>
                {
                    if (meta.Where(x => x.Value == dd && !x.Key.Equals(path)).Count() > 0)
                        throw new Exception("domain already mapped to another node, unset first.");
                });
                var m = meta.Where(x => x.Key == path).ToList();
                m.ForEach(x =>
                {
                    repo.DeleteMeta(x);
                });
                d.ForEach(x =>
                {
                    var newMeta = new PuckMeta();
                    newMeta.Name = DBNames.DomainMapping;
                    newMeta.Key = path;
                    newMeta.Value = x;
                    repo.AddMeta(newMeta);
                });
                repo.SaveChanges();
            }
            ApiHelper.UpdateDomainMappings();                
        }
        public static void SetLocalisation(string path,string variant) {
            if (string.IsNullOrEmpty(path))
                throw new Exception("path null or empty");
            var repo = Repo;
            if (string.IsNullOrEmpty(variant))
            {
                var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.PathToLocale && x.Key == path).ToList();
                meta.ForEach(x =>
                {
                    repo.DeleteMeta(x);
                });
                if (meta.Count > 0)
                    repo.SaveChanges();
            }
            else
            {
                var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.PathToLocale && x.Key == path).ToList();
                meta.ForEach(x => repo.DeleteMeta(x));
                    
                var newMeta = new PuckMeta();
                newMeta.Name = DBNames.PathToLocale;
                newMeta.Key = path;
                newMeta.Value = variant;
                repo.AddMeta(newMeta);
                repo.SaveChanges();
            }
            ApiHelper.UpdatePathLocaleMappings();                
        }
        public static void Publish(Guid id,string variant,List<string> descendants,bool publish) {
            lock (_savelck)
            {
                var repo = Repo;
                //get items to delete
                var repoQ = repo.GetPuckRevision().Where(x => x.Id == id && x.Current);
                if (!string.IsNullOrEmpty(variant))
                    repoQ = repoQ.Where(x=>x.Variant.ToLower().Equals(variant.ToLower()));
                //return as models
                var repoItems = repoQ.ToList().Select(x=>{
                    var mod = RevisionToBaseModel(x);
                    mod.Published=publish;
                    x.Published=publish;
                    x.Value = JsonConvert.SerializeObject(mod);
                    return mod;
                }).ToList();
                
                if (repoItems.Count == 0)
                    throw new Exception("no results with ID:" + id + " Variant:"+variant+" to publish");

                var parentVariants = repo.CurrentRevisionParent(repoItems.First().Path).ToList();
                //can't publish if parent not published unless root item
                if (parentVariants.Count == 0 && repoItems.First().Path.Count(c=>c=='/')>1)
                        throw new Exception("you cannot publish an item if it has no parent");
                
                if (descendants.Count>0)
                    repoItems.AddRange(
                        repo.CurrentRevisionDescendants(repoItems.First().Path).Where(x=>descendants.Contains(x.Variant.ToLower())).ToList().Select(x=>{
                            var mod = RevisionToBaseModel(x);
                            mod.Published = publish;
                            x.Published = publish;
                            x.Value = JsonConvert.SerializeObject(mod);
                            return mod;
                    }).ToList());
                repo.SaveChanges();
                indexer.Index(repoItems);
            }                
        }
        public static void Delete(Guid id, string variant = null) {
            lock (_savelck)
            {
                //remove from index
                var repo = Repo;
                var qh = new QueryHelper<BaseModel>();
                var toDeleteQ = qh.And().ID(id);
                if (!string.IsNullOrEmpty(variant))
                    toDeleteQ.Field(x => x.Variant, variant);
                var toDelete = toDeleteQ.GetAll();

                var variants = new List<BaseModel>();
                if (toDelete.Count > 0)
                {
                    variants = toDelete.First().Variants<BaseModel>();
                    if (variants.Count == 0)
                    {
                        var descendants = toDelete.First().Descendants<BaseModel>();
                        toDelete.AddRange(descendants);
                    }
                }

                toDelete.Delete();
                
                //remove from repo
                var repoItemsQ = repo.GetPuckRevision().Where(x => x.Id == id && x.Current);
                if (!string.IsNullOrEmpty(variant))
                    repoItemsQ = repoItemsQ.Where(x => x.Variant.ToLower().Equals(variant.ToLower()));
                var repoItems = repoItemsQ.ToList();
                var repoVariants = new List<PuckRevision>();
                if (repoItems.Count > 0)
                {
                    repoVariants = repo.CurrentRevisionVariants(repoItems.First().Id, repoItems.First().Variant).ToList();
                    if (variants.Count == 0)
                    {
                        var descendants = repo.CurrentRevisionDescendants(repoItems.First().Path).ToList();
                        repoItems.AddRange(descendants);
                    }
                }
                repoItems.ForEach(x => repo.DeleteRevision(x));
                
                //remove localisation setting
                string lookUpPath=string.Empty;
                if (repoItems.Any())
                    lookUpPath = repoItems.First().Path;
                else if (toDelete.Any())
                    lookUpPath = toDelete.First().Path;

                if (!string.IsNullOrEmpty(lookUpPath))
                {
                    var lmeta = new List<PuckMeta>();
                    if(repoVariants.Any())
                        lmeta = repo.GetPuckMeta().Where(x => x.Name == DBNames.PathToLocale && x.Key.Equals(lookUpPath.ToLower())).ToList();
                    else
                        lmeta = repo.GetPuckMeta().Where(x => x.Name == DBNames.PathToLocale && x.Key.StartsWith(lookUpPath.ToLower())).ToList();
                    lmeta.ForEach(x =>
                    {
                        repo.DeleteMeta(x);
                    });
                    
                    //remove domain mappings
                    var dmeta = new List<PuckMeta>();
                    if(repoVariants.Any())
                        dmeta =repo.GetPuckMeta().Where(x => x.Name == DBNames.DomainMapping && x.Key.Equals(lookUpPath.ToLower())).ToList();
                    else
                        dmeta =repo.GetPuckMeta().Where(x => x.Name == DBNames.DomainMapping && x.Key.StartsWith(lookUpPath)).ToList();
                    dmeta.ForEach(x =>
                    {
                        repo.DeleteMeta(x);
                    });
                }
                ApiHelper.UpdateDomainMappings();
                ApiHelper.UpdatePathLocaleMappings();
                repo.SaveChanges();
            }             
        }
        public static void SaveContent<T>(T mod) where T : BaseModel {
            lock (_savelck)
            {
                var repo = Repo;
                //append nodename to path, which indicates first save
                //mod.Path = mod.Path.EndsWith("/") ? mod.Path + mod.NodeName.Replace(" ","-") : mod.Path;
                //get parent check published
                var parentVariants = repo.CurrentRevisionParent(mod.Path).ToList();
                if (mod.Path.Count(x => x == '/') > 1 && parentVariants.Count() == 0)
                    throw new Exception("this is not a root node yet doesn't have a parent");
                //can't publish if parent not published
                if (mod.Path.Count(x => x == '/') > 1 && !parentVariants.Any(x => x.Published && x.Variant.ToLower().Equals(mod.Variant.ToLower())))
                    mod.Published = false;
                //get sibling nodes
                mod.Revision += 1;
                var nodeDirectory = mod.Path.Substring(0, mod.Path.LastIndexOf('/') + 1);
                mod.Path = nodeDirectory + mod.NodeName.Replace(" ","-");
                var nodesAtPath = repo.CurrentRevisionsByPath(nodeDirectory).Where(x => x.Id != mod.Id)
                    .ToList()
                    .Select(x =>
                        RevisionToBaseModel(x)
                    ).ToList().GroupByID();
                //set sort order for new content
                if (mod.SortOrder == -1)
                    mod.SortOrder = nodesAtPath.Count;
                //check node name is unique at path
                if (nodesAtPath.Any(x => x.Value.Any(y => y.Value.NodeName.ToLower().Equals(mod.NodeName))))
                    throw new Exception("Nodename exists at this path, choose another.");
                //check this is an update or create
                var original = repo.CurrentRevision(mod.Id, mod.Variant);
                var toIndex = new List<BaseModel>();
                toIndex.Add(mod);
                bool nameChanged = false;
                string originalPath = string.Empty;
                if (original != null)
                {//this must be an edit
                    //if (!original.NodeName.ToLower().Equals(mod.NodeName.ToLower()))
                    if (!original.Path.ToLower().Equals(mod.Path.ToLower()))
                    {
                        nameChanged = true;
                        originalPath = original.Path;
                    }
                }
                var variantsDb = repo.CurrentRevisionVariants(mod.Id, mod.Variant).ToList();
                //if (variantsDb.Any(x => !x.NodeName.ToLower().Equals(mod.NodeName.ToLower())))
                if (variantsDb.Any(x => !x.Path.ToLower().Equals(mod.Path.ToLower())))
                {//update path of variants
                    nameChanged = true;
                    if (string.IsNullOrEmpty(originalPath))
                        originalPath = variantsDb.First().Path;
                    variantsDb.ForEach(x => { x.NodeName = mod.NodeName; x.Path = mod.Path; });
                }

                if (nameChanged)
                {
                    var regex = new Regex(Regex.Escape(originalPath), RegexOptions.Compiled);
                    //update path of decendants
                    var descendantsDb = repo.CurrentRevisionDescendants(originalPath).ToList();
                    descendantsDb.ForEach(x => { x.Path = regex.Replace(x.Path, mod.Path, 1); });
                }
                //add revision
                var revision = new PuckRevision();
                revision.LastEditedBy = HttpContext.Current.User.Identity.Name;
                revision.CreatedBy = mod.CreatedBy;
                revision.Created = mod.Created;
                revision.Id = mod.Id;
                revision.NodeName = mod.NodeName;
                revision.Path = mod.Path;
                revision.Published = mod.Published;
                revision.Revision = mod.Revision;
                revision.SortOrder = mod.SortOrder;
                revision.TemplatePath = mod.TemplatePath;
                revision.Type = mod.Type;
                revision.TypeChain = mod.TypeChain;
                revision.Updated = mod.Updated;
                revision.Variant = mod.Variant;
                revision.Current = true;
                revision.Value = JsonConvert.SerializeObject(mod);
                repo.GetPuckRevision()
                    .Where(x => x.Id.Equals(mod.Id) && x.Variant.ToLower().Equals(mod.Variant.ToLower()) && x.Current)
                    .ToList()
                    .ForEach(x => x.Current = false);
                repo.AddRevision(revision);
                if (mod.Published)//add to lucene index
                {
                    var qh = new QueryHelper<BaseModel>();
                    var changed = false;
                    var indexOriginalPath = string.Empty;
                    //get current indexed node with same ID and VARIANT
                    var currentMod = qh.And().Field(x => x.Variant, mod.Variant)
                        .And()
                        .ID(mod.Id)
                        .Get();
                    //if node exists in index
                    if (currentMod != null)
                    {
                        //and that node currently has a different path than the node we're indexing to replace it
                        if (!mod.Path.ToLower().Equals(currentMod.Path.ToLower()))
                        {
                            //means we have changed the path - by changing the nodename
                            changed = true;
                            //set the original path so we can use it for regex replace operation for changing descendants who will otherwise have incorrect paths
                            originalPath = currentMod.Path;
                        }
                    }
                    //get nodes currently indexed which have the same ID but different VARIANT
                    var variants = mod.Variants<BaseModel>();
                    //if any of the variants have different path to the current node
                    if (variants.Any(x => !x.Path.ToLower().Equals(mod.Path.ToLower())))
                    {
                        //means we have changed the path - by changing the nodename
                        changed = true;
                        //if the original path hasn't been set already, set it for use in a regex replace operation
                        if (string.IsNullOrEmpty(indexOriginalPath))
                            indexOriginalPath = variants.First().Path;
                    }
                    //sync up all the variants so they have the same nodename and path
                    variants.ForEach(x => { x.NodeName = mod.NodeName; x.Path = mod.Path; toIndex.Add(x); });
                    //if there was a change in the path
                    if (changed)
                    {
                        //new regex which searches for the current indexed path so it can be replaced with the new one
                        var regex = new Regex(Regex.Escape(indexOriginalPath), RegexOptions.Compiled);
                        var descendants = new List<BaseModel>();
                        //get descendants - either from currently indexed version of the node we're currently saving (which may be new variant and so not currently indexed) or from its variants.
                        if (currentMod != null)
                            descendants = currentMod.Descendants<BaseModel>();
                        else if (variants.Any())
                            descendants = descendants.First().Descendants<BaseModel>();
                        //replace portion of path that has changed
                        descendants.ForEach(x => { x.Path = regex.Replace(x.Path, mod.Path, 1); toIndex.Add(x); });
                        //delete previous meta binding
                        repo.GetPuckMeta().Where(x => x.Name == DBNames.PathToLocale && x.Key.ToLower().Equals(originalPath.ToLower())).ToList()
                            .ForEach(x => x.Key = mod.Path);
                        repo.GetPuckMeta().Where(x => x.Name == DBNames.DomainMapping && x.Key.ToLower().Equals(originalPath.ToLower())).ToList()
                            .ForEach(x => x.Key = mod.Path);
                    }
                    indexer.Index(toIndex);
                }
                //if first time node saved and is root node - set locale for path
                if (variantsDb.Count==0 && (original == null) && mod.Path.Count(x => x == '/') == 1)
                {
                    var lMeta = new PuckMeta()
                    {
                        Name = DBNames.PathToLocale,
                        Key = mod.Path,
                        Value = mod.Variant
                    };
                    repo.AddMeta(lMeta);
                    //if first item - set wildcard domain mapping
                    if (nodesAtPath.Count == 0)
                    {
                        var dMeta = new PuckMeta()
                        {
                            Name = DBNames.DomainMapping,
                            Key = mod.Path,
                            Value = "*"
                        };
                        repo.AddMeta(lMeta);
                    }
                }
                repo.SaveChanges();
                UpdateDomainMappings();
                UpdatePathLocaleMappings();
            }
        }
        public static void UpdateDefaultLanguage() {
            var repo = Repo;
            var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.Settings && x.Key == DBKeys.DefaultLanguage).FirstOrDefault();
            if (meta != null && !string.IsNullOrEmpty(meta.Value))
                PuckCache.SystemVariant = meta.Value;
        }
        public static void UpdateTaskMappings()
        {
            var tasks = Tasks();
            tasks = tasks.Where(x => tdispatcher.CanRun(x)).ToList();
            tdispatcher.Tasks = tasks;
        }
        public static void UpdateRedirectMappings() {
            var repo = Repo;
            var meta301 = repo.GetPuckMeta().Where(x => x.Name == DBNames.Redirect301).ToList();
            var meta302 = repo.GetPuckMeta().Where(x => x.Name == DBNames.Redirect302).ToList();
            var map301 = new Dictionary<string, string>();
            meta301.ForEach(x =>
            {
                map301.Add(x.Key.ToLower(), x.Value.ToLower());
            });
            var map302 = new Dictionary<string, string>();
            meta302.ForEach(x =>
            {
                map302.Add(x.Key.ToLower(), x.Value.ToLower());
            });
            PuckCache.Redirect301= map301;
            PuckCache.Redirect302 = map302;
        }
        public static void UpdateCacheMappings() {
            var repo = Repo;
            var metaTypeCache = repo.GetPuckMeta().Where(x => x.Name == DBNames.CachePolicy).ToList();
            var metaCacheExclude = repo.GetPuckMeta().Where(x => x.Name == DBNames.CacheExclude).ToList();
            
            var mapTypeCache = new Dictionary<string, int>();
            metaTypeCache.ForEach(x =>
            {
                int cacheMinutes;
                if (int.TryParse(x.Value, out cacheMinutes))
                {
                    mapTypeCache.Add(x.Key, cacheMinutes);
                }
            });

            var mapCacheExclude = new HashSet<string>();
            metaCacheExclude.Where(x=>x.Value.ToLower()==bool.TrueString.ToLower()).ToList().ForEach(x =>
            {
                mapCacheExclude.Add(x.Key.ToLower());
            });
            PuckCache.TypeOutputCache = mapTypeCache;
            PuckCache.OutputCacheExclusion = mapCacheExclude;
        }
        public static void UpdateDomainMappings() {
            var repo = Repo;
            var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.DomainMapping).ToList();
            var map = new Dictionary<string, string>();
            meta.ForEach(x => {
                map.Add(x.Value.ToLower(), x.Key.ToLower());
            });
            PuckCache.DomainRoots = map;
        }
        public static void UpdatePathLocaleMappings()
        {
            var repo = Repo;
            var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.PathToLocale).ToList();
            var map = new Dictionary<string, string>();
            meta.ForEach(x =>
            {
                map.Add(x.Key.ToLower(), x.Value.ToLower());
            });
            PuckCache.PathToLocale = map;
        }
        public static String PathLocalisation(string path) {
            var repo = Repo;
            var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.PathToLocale && path.StartsWith(x.Key)).OrderByDescending(x=>x.Key.Length).FirstOrDefault();
            return meta == null ? null : meta.Value;
        }
        public static String DomainMapping(string path)
        {
            var repo = Repo;
            var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.DomainMapping && x.Key == path).ToList();
            return meta.Count == 0 ? string.Empty : string.Join(",",meta.Select(x=>x.Value));
        }
        public static List<string> FieldGroups(string type=null) {
            var repo = Repo;
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
            var repo = Repo;
            var allVariants = AllVariants();
            var results = new List<Variant>();
            var allLanguageMetas = repo.GetPuckMeta().Where(x => x.Name == DBNames.Settings && x.Key== DBKeys.Languages).ToList();
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
            if (type.BaseType != null && type.BaseType!=typeof(Object))
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
        public static List<Type> TaskTypes() {
            return FindDerivedClasses(typeof(BaseTask),null,false).ToList();
        }
        public static List<Type> EditorSettingTypes() {
            return FindDerivedClasses(typeof(I_Puck_Editor_Settings)).ToList();
        }
        public static List<I_Puck_Editor_Settings> EditorSettings()
        {
            var repo = Repo;
            var result = new List<I_Puck_Editor_Settings>();
            var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.EditorSettings).ToList();
            meta.ForEach(x =>
            {
                //key - settingsType:modelType:propertyName
                var keys = x.Key.Split(new char[] { ':' },StringSplitOptions.RemoveEmptyEntries);
                var type = Type.GetType(keys[0]);
                var instance = JsonConvert.DeserializeObject(x.Value, type) as I_Puck_Editor_Settings;
                result.Add(instance);
            });
            return result;
        }
        public static List<BaseTask> Tasks(){
            var repo = Repo;
            var result = new List<BaseTask>();
            var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.Tasks).ToList();
            meta.ForEach(x => {
                var type = Type.GetType(x.Key);
                var instance = JsonConvert.DeserializeObject(x.Value,type) as BaseTask;
                instance.ID = x.ID;
                result.Add(instance);
            });
            return result;
        }
        public static List<Type> AllowedTypes(string typeName) {
            var repo = Repo;
            var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.TypeAllowedTypes && x.Key.Equals(typeName)).ToList();
            var result = meta.Select(x=>Type.GetType(x.Value)).ToList();
            return result;
        }
        public static List<Type> Models(bool inclusive=false) {
            var excluded = new List<Type>() { typeof(PuckRevision)};
            return FindDerivedClasses(typeof(BaseModel),excluded,inclusive).ToList();
        }
        

    }
}
