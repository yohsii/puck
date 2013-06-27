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
namespace puck.core
{
    public static class Bootstrap
    {
        public static void Ini() {
            ApiHelper.UpdateDomainMappings();
            ApiHelper.UpdatePathLocaleMappings();
            ApiHelper.UpdateTaskMappings();
            PuckCache.Analyzers = new List<Lucene.Net.Analysis.Analyzer>();
            PuckCache.AnalyzerForModel = new Dictionary<Type,Lucene.Net.Analysis.Analyzer>();
            foreach(var t in ApiHelper.Models(true)){
                var instance = Activator.CreateInstance(t);
                var dmp = ObjectDumper.Write(instance,int.MaxValue);
                var analyzers = new List<KeyValuePair<string, Analyzer>>();
                foreach (var p in dmp) {
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
                var dispatcher = PuckCache.NinjectKernel.Get<I_Task_Dispatcher>();
                if (dispatcher != null) { 
                    dispatcher.TaskEnd+= (object s,DispatchEventArgs e)=>{
                        if ((PuckCache.UpdateTaskLastRun && !e.Task.Recurring) || (PuckCache.UpdateRecurringTaskLastRun && e.Task.Recurring))
                        {
                            var repo = PuckCache.NinjectKernel.Get<I_Puck_Repository>("T");
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
        }
    }
}
