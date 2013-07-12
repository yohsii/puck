using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using puck.core.Abstract;
using puck.core.Constants;
using puck.core.Helpers;
using puck.core.Events;
using Ninject;
using Newtonsoft.Json;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using System.Data.Entity;
using puck.core.Entities;
namespace puck.core
{
    public static class Bootstrap
    {
        public static void Ini() {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<PuckContext, puck.core.Migrations.Configuration>());
            ApiHelper.UpdateDomainMappings();
            ApiHelper.UpdatePathLocaleMappings();
            ApiHelper.UpdateTaskMappings();
            ApiHelper.UpdateDefaultLanguage();
            ApiHelper.UpdateCacheMappings();
            ApiHelper.UpdateRedirectMappings();
            PuckCache.Analyzers = new List<Lucene.Net.Analysis.Analyzer>();
            PuckCache.AnalyzerForModel = new Dictionary<Type,Lucene.Net.Analysis.Analyzer>();
            PuckCache.TypeFields = new Dictionary<string, List<string>>();
            foreach(var t in ApiHelper.Models(true)){
                var instance = Activator.CreateInstance(t);
                var dmp = ObjectDumper.Write(instance,int.MaxValue);
                var analyzers = new List<KeyValuePair<string, Analyzer>>();
                PuckCache.TypeFields[t.AssemblyQualifiedName] = new List<string>();
                foreach (var p in dmp) {
                    PuckCache.TypeFields[t.AssemblyQualifiedName].Add(p.Key);
                    if (p.Analyzer == null)
                        continue;
                    if (!PuckCache.Analyzers.Any(x => x.GetType() == p.Analyzer.GetType())) {
                        PuckCache.Analyzers.Add(p.Analyzer);
                    }
                    analyzers.Add(new KeyValuePair<string,Analyzer>(p.Key, PuckCache.Analyzers.Where(x => x.GetType() == p.Analyzer.GetType()).FirstOrDefault()));
                }
                var pfAnalyzer = new PerFieldAnalyzerWrapper(new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30),analyzers);
                PuckCache.AnalyzerForModel.Add(t,pfAnalyzer);
            }
            if (PuckCache.UpdateTaskLastRun || PuckCache.UpdateRecurringTaskLastRun) {
                var dispatcher = PuckCache.PuckDispatcher;
                if (dispatcher != null) { 
                    dispatcher.TaskEnd+= (object s,DispatchEventArgs e)=>{
                        if ((PuckCache.UpdateTaskLastRun && !e.Task.Recurring) || (PuckCache.UpdateRecurringTaskLastRun && e.Task.Recurring))
                        {
                            var repo = PuckCache.PuckRepo;
                            var taskMeta = repo.GetPuckMeta().Where(x => x.Name == DBNames.Tasks && x.ID == e.Task.ID).FirstOrDefault();
                            if (taskMeta != null)
                            {
                                taskMeta.Value = JsonConvert.SerializeObject(e.Task);
                                repo.SaveChanges();
                                repo = null;
                            }
                        }
                    };
                }
            }

            //DataAnnotationsModelValidatorProvider.AddImplicitRequiredAttributeForValueTypes = false;
            /*
            Content_Indexer_Searcher.RegisterBeforeIndexHandler<puck.areas.admin.ViewModels.Home>("doshit"
                ,(object o,puck.core.Events.BeforeIndexingEventArgs args) => { 
                    
                }
                ,true
                );
            */

        }
    }
}
