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
using System.Web;
namespace puck.core
{
    public static class Bootstrap
    {
        public static void Ini() {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<PuckContext, puck.core.Migrations.Configuration>());
            ApiHelper.SetGeneratedMappings();
            ApiHelper.UpdateDomainMappings();
            ApiHelper.UpdatePathLocaleMappings();
            ApiHelper.UpdateTaskMappings();
            ApiHelper.UpdateDefaultLanguage();
            ApiHelper.UpdateCacheMappings();
            ApiHelper.UpdateRedirectMappings();
            PuckCache.Analyzers = new List<Lucene.Net.Analysis.Analyzer>();
            PuckCache.AnalyzerForModel = new Dictionary<Type,Lucene.Net.Analysis.Analyzer>();
            PuckCache.TypeFields = new Dictionary<string, Dictionary<string,string>>();
            PuckCache.SmtpFrom = "puck@"+PuckCache.SmtpHost;
            ApiHelper.UpdateAnalyzerMappings();

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
            //bind notification handlers
            //publish
            PuckCache.PuckIndexer.RegisterAfterIndexHandler<puck.core.Base.BaseModel>("puck_publish_notification", (object o, puck.core.Events.IndexingEventArgs args) =>
            {
                try
                {
                    var usersToNotify = ApiHelper.UsersToNotify(args.Node.Path, PuckCache.NotifyActions.Publish);
                    if (usersToNotify.Count == 0) return;
                    var subject = string.Concat("content published - ", args.Node.NodeName, " - ", args.Node.Path);
                    var template = System.IO.File.ReadAllText(HttpContext.Current.Server.MapPath(PuckCache.EmailTemplatePublishPath));
                    template = ApiHelper.EmailTransform(template, args.Node,PuckCache.NotifyActions.Publish);
                    var emails = string.Join(";", usersToNotify.Select(x => x.Email)).TrimEnd(';');
                    ApiHelper.Email(emails, subject, template);
                }
                catch (Exception ex) {
                    PuckCache.PuckLog.Log(ex);
                }
            }, true);
            //edit
            ApiHelper.RegisterAfterIndexHandler<puck.core.Base.BaseModel>("puck_edit_notification", (object o, puck.core.Events.IndexingEventArgs args) =>
            {
                try
                {
                    var usersToNotify = ApiHelper.UsersToNotify(args.Node.Path, PuckCache.NotifyActions.Edit);
                    if (usersToNotify.Count == 0) return; 
                    var subject = string.Concat("content edited - ", args.Node.NodeName, " - ", args.Node.Path);
                    var template = System.IO.File.ReadAllText(HttpContext.Current.Server.MapPath(PuckCache.EmailTemplateEditPath));
                    template = ApiHelper.EmailTransform(template, args.Node, PuckCache.NotifyActions.Edit);
                    var emails = string.Join(";", usersToNotify.Select(x => x.Email)).TrimEnd(';');
                    ApiHelper.Email(emails, subject, template);
                }
                catch (Exception ex)
                {
                    PuckCache.PuckLog.Log(ex);
                }
            }, true);
            //delete
            ApiHelper.RegisterAfterDeleteHandler<puck.core.Base.BaseModel>("puck_delete_notification", (object o, puck.core.Events.IndexingEventArgs args) =>
            {
                try
                {
                    var usersToNotify = ApiHelper.UsersToNotify(args.Node.Path, PuckCache.NotifyActions.Delete);
                    if (usersToNotify.Count == 0) return; 
                    var subject = string.Concat("content deleted - ", args.Node.NodeName, " - ", args.Node.Path);
                    var template = System.IO.File.ReadAllText(HttpContext.Current.Server.MapPath(PuckCache.EmailTemplateDeletePath));
                    template = ApiHelper.EmailTransform(template, args.Node, PuckCache.NotifyActions.Delete);
                    var emails = string.Join(";", usersToNotify.Select(x => x.Email)).TrimEnd(';');
                    ApiHelper.Email(emails, subject, template);
                }
                catch (Exception ex)
                {
                    PuckCache.PuckLog.Log(ex);
                }
            }, true);
            //move
            ApiHelper.RegisterAfterMoveHandler<puck.core.Base.BaseModel>("puck_move_notification", (object o, puck.core.Events.MoveEventArgs args) =>
            {
                try
                {
                    var node = args.Nodes.FirstOrDefault();
                    var usersToNotify = ApiHelper.UsersToNotify(node.Path, PuckCache.NotifyActions.Move);
                    if (usersToNotify.Count == 0) return; 
                    var subject = string.Concat("content move - ", node.NodeName, " - ", node.Path);
                    var template = System.IO.File.ReadAllText(HttpContext.Current.Server.MapPath(PuckCache.EmailTemplateMovePath));
                    template = ApiHelper.EmailTransform(template, node, PuckCache.NotifyActions.Move);
                    var emails = string.Join(";", usersToNotify.Select(x => x.Email)).TrimEnd(';');
                    ApiHelper.Email(emails, subject, template);
                }
                catch (Exception ex)
                {
                    PuckCache.PuckLog.Log(ex);
                }
            }, true);
            
            //DataAnnotationsModelValidatorProvider.AddImplicitRequiredAttributeForValueTypes = false;
            
        }
    }
}
