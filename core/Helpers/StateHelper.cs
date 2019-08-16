﻿using Lucene.Net.Analysis;
using Newtonsoft.Json;
using Ninject;
using puck.core.Abstract;
using puck.core.Base;
using puck.core.Constants;
using puck.core.Entities;
using puck.core.Identity;
using puck.core.Models.EditorSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Lucene.Net.Analysis.Standard;
using puck.core.State;
using Lucene.Net.Analysis.Miscellaneous;

namespace puck.core.Helpers
{
    public static class StateHelper
    {
        public static PuckRoleManager roleManager{get{
                return PuckCache.NinjectKernel.Get<PuckRoleManager>();
        }}
        public static PuckUserManager userManager{get{
                return PuckCache.NinjectKernel.Get<PuckUserManager>();
        }}
        public static I_Puck_Repository Repo{get{
                return PuckCache.NinjectKernel.Get<I_Puck_Repository>();
        }}
        public static I_Task_Dispatcher tdispatcher{get{
                return PuckCache.PuckDispatcher;
        }}
        public static I_Content_Indexer indexer{get{
                return PuckCache.PuckIndexer;
        }}
        public static ApiHelper apiHelper { get { return PuckCache.ApiHelper; } }
        public static I_Log logger { get { return PuckCache.PuckLog; } }

        public static void UpdateCrops() {
            var settingsType = typeof(PuckImageEditorSettings);
            var modelType = typeof(BaseModel);
            var repo = Repo;
            string key = string.Concat(settingsType.AssemblyQualifiedName, ":", modelType.AssemblyQualifiedName, ":");
            var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.EditorSettings && x.Key.Equals(key)).FirstOrDefault();
            if (meta != null)
            {
                var data = JsonConvert.DeserializeObject(meta.Value, settingsType) as PuckImageEditorSettings;
                if (data != null) {
                    PuckCache.CropSizes = new Dictionary<string, Models.CropInfo>();
                    foreach (var crop in data.Crops ?? new List<Models.CropInfo>()) {
                        if(!string.IsNullOrEmpty(crop.Alias))
                            PuckCache.CropSizes[crop.Alias] = crop;
                    }
                }
            }
        }

        public static void SetGeneratedMappings()
        {
            var repo = Repo;
            var dictionary = new Dictionary<string, Type>();
            var gmods = repo.GetGeneratedModel().ToList();
            foreach (var mod in gmods)
            {
                try
                {
                    if (string.IsNullOrEmpty(mod.IFullPath) || string.IsNullOrEmpty(mod.CFullPath))
                        continue;
                    var idll = Assembly.LoadFrom(HttpContext.Current.Server.MapPath(mod.IFullPath));
                    var cdll = Assembly.LoadFrom(HttpContext.Current.Server.MapPath(mod.CFullPath));

                    AppDomain.CurrentDomain.Load(idll.GetName());
                    AppDomain.CurrentDomain.Load(cdll.GetName());

                    dictionary.Add(idll.GetTypes().First().AssemblyQualifiedName, cdll.GetTypes().First());
                }
                catch (Exception ex)
                {
                    logger.Log(ex);
                }
            }
            PuckCache.IGeneratedToModel = dictionary;
        }
        public static void SetFirstRequestUrl() {
            if (PuckCache.FirstRequestUrl==null)
                PuckCache.FirstRequestUrl = HttpContext.Current.Request.Url;
        }
        public static void UpdateTaskMappings()
        {
            var tasks = apiHelper.Tasks();
            tasks.AddRange(apiHelper.SystemTasks());
            //tasks = tasks.Where(x => tdispatcher.CanRun(x)).ToList();
            tasks.ForEach(x => x.TaskEnd += tdispatcher.HandleTaskEnd);
            tdispatcher.Tasks = tasks;
        }
        //update class hierarchies/typechains which may have changed since last run
        public static void UpdateTypeChains()
        {
            var repo = Repo;
            var excluded = new List<Type> { typeof(puck.core.Entities.PuckRevision) };
            var currentTypes = ApiHelper.FindDerivedClasses(typeof(puck.core.Base.BaseModel), excluded: excluded, inclusive: false);
            var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.TypeChain).ToList();
            var typesToUpdate = new List<Type>();
            foreach (var item in meta)
            {
                //check saved type is in currentTypes
                var type = currentTypes.FirstOrDefault(x => x.AssemblyQualifiedName.Equals(item.Key));
                if (type != null)
                {
                    var typeChain = ApiHelper.TypeChain(type);
                    var dbTypeChain = item.Value;
                    //check that typechain is the same
                    //if not, add to types to update
                    if (!typeChain.Equals(dbTypeChain))
                    {
                        typesToUpdate.Add(type);
                    }
                }
            }
            var toIndex = new List<BaseModel>();
            foreach (var type in typesToUpdate)
            {
                //get revisions whose typechains have changed
                var revisions = repo.GetPuckRevision().Where(x => x.Type.Equals(type.AssemblyQualifiedName));
                foreach (var revision in revisions)
                {
                    //update typechain in revision and in model which may need to be published
                    revision.TypeChain = ApiHelper.TypeChain(type);
                    var model = ApiHelper.RevisionToBaseModel(revision);
                    model.TypeChain = ApiHelper.TypeChain(type);
                    revision.Value = JsonConvert.SerializeObject(model);
                    if (model.Published && revision.Current)
                        toIndex.Add(model);
                }
                repo.SaveChanges();
            }
            //publish content with updated typechains
            indexer.Index(toIndex);
            //delete typechains from previous bootstrap
            meta.ForEach(x => repo.DeleteMeta(x));
            repo.SaveChanges();
            //save typechains from current bootstrap
            currentTypes.ToList().ForEach(x => {
                var newMeta = new PuckMeta
                {
                    Name = DBNames.TypeChain,
                    Key = x.AssemblyQualifiedName,
                    Value = ApiHelper.TypeChain(x)
                };
                repo.AddMeta(newMeta);
            });
            repo.SaveChanges();
        }
        public static void UpdateRedirectMappings()
        {
            var repo = Repo;
            var meta301 = repo.GetPuckMeta().Where(x => x.Name == DBNames.Redirect301).ToList();
            var meta302 = repo.GetPuckMeta().Where(x => x.Name == DBNames.Redirect302).ToList();
            var map301 = new Dictionary<string, string>();
            meta301.ForEach(x =>
            {
                //map301.Add(x.Key.ToLower(), x.Value.ToLower());
                map301[x.Key.ToLower()] = x.Value.ToLower();
            });
            var map302 = new Dictionary<string, string>();
            meta302.ForEach(x =>
            {
                //map302.Add(x.Key.ToLower(), x.Value.ToLower());
                map302[x.Key.ToLower()] = x.Value.ToLower();
            });
            PuckCache.Redirect301 = map301;
            PuckCache.Redirect302 = map302;
        }
        public static void UpdateCacheMappings()
        {
            var repo = Repo;
            var metaTypeCache = repo.GetPuckMeta().Where(x => x.Name == DBNames.CachePolicy).ToList();
            var metaCacheExclude = repo.GetPuckMeta().Where(x => x.Name == DBNames.CacheExclude).ToList();

            var mapTypeCache = new Dictionary<string, int>();
            metaTypeCache.ForEach(x =>
            {
                int cacheMinutes;
                if (int.TryParse(x.Value, out cacheMinutes))
                {
                    //mapTypeCache.Add(x.Key, cacheMinutes);
                    mapTypeCache[x.Key] = cacheMinutes;
                }
            });

            var mapCacheExclude = new HashSet<string>();
            metaCacheExclude.Where(x => x.Value.ToLower() == bool.TrueString.ToLower()).ToList().ForEach(x =>
            {
                if(!mapCacheExclude.Contains(x.Key.ToLower()))
                    mapCacheExclude.Add(x.Key.ToLower());
            });
            PuckCache.TypeOutputCache = mapTypeCache;
            PuckCache.OutputCacheExclusion = mapCacheExclude;
        }
        public static void UpdateDomainMappings()
        {
            var repo = Repo;
            var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.DomainMapping).ToList();
            var map = new Dictionary<string, string>();
            meta.ForEach(x => {
                //map.Add(x.Value.ToLower(), x.Key.ToLower());
                map[x.Value.ToLower()] = x.Key.ToLower();
            });
            PuckCache.DomainRoots = map;
        }
        public static void UpdatePathLocaleMappings()
        {
            var repo = Repo;
            var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.PathToLocale).OrderByDescending(x => x.Key.Length).ToList();
            var map = new Dictionary<string, string>();
            meta.ForEach(x =>
            {
                //map.Add(x.Key.ToLower(), x.Value.ToLower());
                map[x.Key.ToLower()] = x.Value.ToLower();
            });
            PuckCache.PathToLocale = map;
        }
        public static void UpdateAQNMappings()
        {
            foreach (var t in apiHelper.AllModels(true))
            {
                PuckCache.ModelFullNameToAQN[t.Name] = t.AssemblyQualifiedName;
            }
        }
        public static void UpdateAnalyzerMappings()
        {
            var panalyzers = new List<Analyzer>();
            var analyzerForModel = new Dictionary<Type, Analyzer>();
            foreach (var t in apiHelper.AllModels(true))
            {
                var instance = ApiHelper.CreateInstance(t);
                try
                {
                    ObjectDumper.SetPropertyValues(instance);
                }
                catch (Exception ex) {
                    PuckCache.PuckLog.Log(ex);
                };
                
                var dmp = ObjectDumper.Write(instance, int.MaxValue);
                var analyzers = new Dictionary<string, Analyzer>();
                PuckCache.TypeFields[t.AssemblyQualifiedName] = new Dictionary<string, string>();
                foreach (var p in dmp)
                {
                    if (!PuckCache.TypeFields[t.AssemblyQualifiedName].ContainsKey(p.Key))
                        PuckCache.TypeFields[t.AssemblyQualifiedName].Add(p.Key, p.Type.AssemblyQualifiedName);
                    if (p.Analyzer == null)
                        continue;
                    if (!panalyzers.Any(x => x.GetType() == p.Analyzer.GetType()))
                    {
                        panalyzers.Add(p.Analyzer);
                    }
                    analyzers.Add(p.Key, panalyzers.Where(x => x.GetType() == p.Analyzer.GetType()).FirstOrDefault());
                }
                var pfAnalyzer = new PerFieldAnalyzerWrapper(new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48), analyzers);
                analyzerForModel.Add(t, pfAnalyzer);
            }
            PuckCache.Analyzers = panalyzers;
            PuckCache.AnalyzerForModel = analyzerForModel;
        }
        public static void UpdateDefaultLanguage()
        {
            var repo = Repo;
            var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.Settings && x.Key == DBKeys.DefaultLanguage).FirstOrDefault();
            if (meta != null && !string.IsNullOrEmpty(meta.Value))
                PuckCache.SystemVariant = meta.Value;
        }


    }
}
