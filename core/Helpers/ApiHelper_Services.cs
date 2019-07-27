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
using puck.core.Exceptions;
using puck.core.Events;
using System.Web.Security;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using puck.core.Identity;
using Microsoft.AspNet.Identity;
using StackExchange.Profiling;
using System.Data.SqlClient;
using puck.core.Tasks;

namespace puck.core.Helpers
{
    public partial class ApiHelper
    {
        private static readonly object _savelck = new object();
        public static PuckRoleManager roleManager{get{
                return PuckCache.NinjectKernel.Get<PuckRoleManager>();
        }}
        public static PuckUserManager userManager {get{
                return PuckCache.NinjectKernel.Get<PuckUserManager>();
        }}
        public static I_Puck_Repository Repo { get {
            return PuckCache.NinjectKernel.Get<I_Puck_Repository>();
        }}
        public static I_Task_Dispatcher tdispatcher{get{
            return PuckCache.PuckDispatcher;
        }}
        public static I_Content_Indexer indexer{get{
            return PuckCache.PuckIndexer;
         }}
        public static I_Log logger { get { return PuckCache.PuckLog; } }
        public static string UserVariant() {
            string variant;
            if (HttpContext.Current.Session["language"] != null)
            {
                variant = HttpContext.Current.Session["language"] as string;
            }
            else
            {
                var repo = PuckCache.PuckRepo;
                var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.UserVariant && x.Key == HttpContext.Current.User.Identity.Name).FirstOrDefault();
                if (meta != null && !string.IsNullOrEmpty(meta.Value))
                {
                    variant = meta.Value;
                    HttpContext.Current.Session["language"] = meta.Value;
                }
                else
                {
                    variant = PuckCache.SystemVariant;
                }
            }
            return variant;
        }
        
        public static void Sort(Guid parentId, List<Guid> ids) {
            var repo = Repo;
            var qh = new QueryHelper<BaseModel>();
            var indexItems = qh.And().Field(x=>x.ParentId,parentId.ToString()).GetAllNoCast();
            var dbItems = repo.CurrentRevisionsByParentId(parentId).ToList();
            indexItems.ForEach(n =>
            {
                for (var i = 0; i < ids.Count; i++)
                {
                    if (ids[i].Equals(n.Id))
                    {
                        n.SortOrder = i;
                    }
                }
            });
            var c = 0;
            var indexItemsNotListed = indexItems.Where(x => !ids.Contains(x.Id)).ToList();
            indexItemsNotListed.ForEach(x=> { x.SortOrder = ids.Count + c; c++; });
            dbItems.ForEach(n =>
            {
                for (var i = 0; i < ids.Count; i++)
                {
                    if (ids[i].Equals(n.Id))
                    {
                        n.SortOrder = i;
                    }
                }
            });
            var dbItemsNotListed = dbItems.Where(x => !ids.Contains(x.Id)).ToList();
            dbItemsNotListed.ForEach(x=>{ x.SortOrder = ids.Count + c;c++; });
            repo.SaveChanges();
            indexer.Index(indexItems);            
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
            StateHelper.UpdateDomainMappings();                
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
            StateHelper.UpdatePathLocaleMappings();                
        }
        public static int UpdateDescendantHasNoPublishedRevision(string path, string value,List<string> descendantVariants)
        {
            int rowsAffected = 0;
            using (var con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["PuckContext"].ConnectionString))
            {
                var sql = "update PuckRevisions set [HasNoPublishedRevision] = @value where [IdPath] LIKE @likeStr";
                if (descendantVariants.Any())
                {
                    sql += " and (";
                    for (var i = 0; i < descendantVariants.Count; i++)
                    {
                        var variants = descendantVariants[i];
                        if (i > 0)
                            sql += " or ";
                        sql += "[Variant] = @variant" + i;
                    }
                    sql += ")";
                }
                var com = new SqlCommand(sql, con);
                com.Parameters.AddWithValue("@likeStr", path + "%");
                com.Parameters.AddWithValue("@value", value);
                for (var i = 0; i < descendantVariants.Count; i++) {
                    com.Parameters.AddWithValue("@variant"+i, descendantVariants[i]);
                }
                con.Open();
                rowsAffected = com.ExecuteNonQuery();
            }
            return rowsAffected;
        }
        public static int UpdateDescendantIsPublishedRevision(string path, string value,bool addWhereIsCurrentClause,List<string> descendantVariants)
        {
            int rowsAffected = 0;
            using (var con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["PuckContext"].ConnectionString))
            {
                var sql = "update PuckRevisions set [Published] = @value , [IsPublishedRevision] = @value where [IdPath] LIKE @likeStr";
                if (addWhereIsCurrentClause)
                    sql += " and [Current] = 1";
                if (descendantVariants.Any())
                {
                    sql += " and (";
                    for (var i = 0; i < descendantVariants.Count; i++)
                    {
                        var variants = descendantVariants[i];
                        if (i > 0)
                            sql += " or ";
                        sql += "[Variant] = @variant" + i;
                    }
                    sql += ")";
                }

                var com = new SqlCommand(sql, con);
                com.Parameters.AddWithValue("@likeStr", path + "%");
                com.Parameters.AddWithValue("@value", value);
                for (var i = 0; i < descendantVariants.Count; i++)
                {
                    com.Parameters.AddWithValue("@variant" + i, descendantVariants[i]);
                }
                con.Open();
                rowsAffected = com.ExecuteNonQuery();
            }
            return rowsAffected;
        }
        public static void Publish(Guid id, string variant,List<string> descendantVariants) {
            lock (_savelck)
            {
                var repo = Repo;
                var currentRevision = repo.CurrentRevision(id, variant);
                if (currentRevision.ParentId != Guid.Empty) {
                    var publishedParentRevisions = repo.PublishedRevisions(currentRevision.ParentId);
                    if (publishedParentRevisions.Count() == 0) {
                        throw new Exception("You cannot publish this node because its parent is not published.");
                    }
                }
                var mod = ApiHelper.RevisionToBaseModel(currentRevision);
                mod.Published = true;
                SaveContent(mod, makeRevision: false);
                var affected = 0;
                if (descendantVariants.Any())
                {
                    //set descendants to have HasNoPublishedRevision set to false
                    affected = UpdateDescendantHasNoPublishedRevision(currentRevision.IdPath + ",", "0", descendantVariants);
                    //set descendants to have IsPublishedRevision set to false
                    affected = UpdateDescendantIsPublishedRevision(currentRevision.IdPath + ",", "0", false, descendantVariants);
                    //set current descendants to have IsPublishedRevision set to true, since we're publishing Current descendants
                    affected = UpdateDescendantIsPublishedRevision(currentRevision.IdPath + ",", "1", true, descendantVariants);
                    var descendantVariantsLowerCase = descendantVariants.Select(x => x.ToLower()).ToList();
                    var descendantRevisions = repo.CurrentRevisionDescendants(currentRevision.IdPath).Where(x => descendantVariantsLowerCase.Contains(x.Variant.ToLower())).ToList();
                    var descendantModels = descendantRevisions.Select(x => ApiHelper.RevisionToBaseModel(x)).ToList();
                    indexer.Index(descendantModels);
                }
            }
        }
        public static void UnPublish(Guid id, string variant, List<string> descendantVariants)
        {
            lock (_savelck)
            {
                var repo = Repo;
                var toIndex = new List<BaseModel>();
                var currentRevision = repo.CurrentRevision(id, variant);
                var publishedRevision = repo.PublishedRevision(id, variant);
                var mod = ApiHelper.RevisionToBaseModel(currentRevision);
                mod.Published = false;
                SaveContent(mod, makeRevision: false);
                toIndex.Add(mod);
                var publishedVariants = repo.PublishedRevisionVariants(id, variant).ToList();
                var affected = 0;
                if (publishedVariants.Count() == 0)
                {
                    if (!currentRevision.IsPublishedRevision && publishedRevision != null && !currentRevision.Path.ToLower().Equals(publishedRevision.Path.ToLower()))
                    {
                        //since we're unpublishing the published revision (which descendant paths are based on), we should set descendant paths to be based off of the current revision
                        affected = UpdateDescendantPaths(publishedRevision.Path + "/", currentRevision.Path + "/");
                        UpdatePathRelatedMeta(publishedRevision.Path, currentRevision.Path);
                    }
                }
                if (descendantVariants.Any())
                {
                    //set descendants to have HasNoPublishedRevision set to true
                    affected=UpdateDescendantHasNoPublishedRevision(currentRevision.IdPath + ",", "1", descendantVariants);
                    //set descendants to have IsPublishedRevision set to false
                    affected=UpdateDescendantIsPublishedRevision(currentRevision.IdPath + ",", "0", false, descendantVariants);
                    var descendantVariantsLowerCase = descendantVariants.Select(x => x.ToLower()).ToList();
                    var descendantRevisions = repo.CurrentRevisionDescendants(currentRevision.IdPath).Where(x => descendantVariantsLowerCase.Contains(x.Variant.ToLower())).ToList();
                    var descendantModels = descendantRevisions.Select(x => ApiHelper.RevisionToBaseModel(x)).ToList();
                    toIndex.AddRange(descendantModels);
                }
                indexer.Index(toIndex);
            }
        }
        public static void Publish(Guid id,string variant,List<string> descendants,bool publish) {
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
                    throw new NoParentExistsException("you cannot publish an item if it has no parent, unless it's a root node");
                
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
        public static void Delete(Guid id, string variant = null) {
            //remove from index
            var repo = Repo;
            var qh = new QueryHelper<BaseModel>();
            qh.ID(id);
            if (!string.IsNullOrEmpty(variant))
                qh.And().Field(x => x.Variant, variant);
            var toDelete = qh.GetAll();

            var variants = new List<BaseModel>();
            if (toDelete.Count > 0)
            {
                variants = toDelete.First().Variants<BaseModel>();
                if (variants.Count == 0 || string.IsNullOrEmpty(variant))
                {
                    var descendants = toDelete.First().Descendants<BaseModel>();
                    toDelete.AddRange(descendants);
                }
            }
            indexer.Delete(toDelete);
            var cancelled = new List<BaseModel>();
            //remove from repo
            var repoItemsQ = repo.GetPuckRevision().Where(x => x.Id == id && x.Current);
            if (!string.IsNullOrEmpty(variant))
                repoItemsQ = repoItemsQ.Where(x => x.Variant.ToLower().Equals(variant.ToLower()));
            var repoItems = repoItemsQ.ToList();
            var repoVariants = new List<PuckRevision>();
            if (repoItems.Count > 0)
            {
                repoVariants = repo.CurrentRevisionVariants(repoItems.First().Id, repoItems.First().Variant).ToList();
                if (variants.Count == 0 || string.IsNullOrEmpty(variant))
                {
                    var descendants = repo.CurrentRevisionDescendants(repoItems.First().Path).ToList();
                    repoItems.AddRange(descendants);
                }
            }
            repoItems.ForEach(x => {
                var args = new BeforeIndexingEventArgs() { Node = x, Cancel = false };
                OnBeforeDelete(null, args);
                if (args.Cancel) {
                    cancelled.Add(x);
                    return;
                }    
                repo.DeleteRevision(x);
            });
            repoItems
                    .Where(x => !cancelled.Contains(x))
                    .ToList()
                    .ForEach(x => { OnAfterDelete(null, new IndexingEventArgs() { Node = x }); });
                
            //remove localisation setting
            string lookUpPath=string.Empty;
            if (repoItems.Any())
                lookUpPath = repoItems.First().Path;
            else if (toDelete.Any())
                lookUpPath = toDelete.First().Path;

            if (!string.IsNullOrEmpty(lookUpPath))
            {
                var lmeta = new List<PuckMeta>();
                var dmeta = new List<PuckMeta>();
                var cmeta = new List<PuckMeta>();
                var nmeta = new List<PuckMeta>();
                //if descendants are being deleted - descendants are included if there are no variants for the deleted node (therefore orphaning descendants) or if variant argument is not present (which means you wan't all variants deleted)
                if (repoVariants.Any() && !string.IsNullOrEmpty(variant))
                {
                    //lmeta = repo.GetPuckMeta().Where(x => x.Name == DBNames.PathToLocale && x.Key.ToLower().Equals(lookUpPath.ToLower())).ToList();
                    //dmeta = repo.GetPuckMeta().Where(x => x.Name == DBNames.DomainMapping && x.Key.ToLower().Equals(lookUpPath.ToLower())).ToList();
                    //cmeta = repo.GetPuckMeta().Where(x => x.Name == DBNames.CacheExclude && x.Key.ToLower().Equals(lookUpPath.ToLower())).ToList();
                }
                else
                {
                    lmeta = repo.GetPuckMeta().Where(x => x.Name == DBNames.PathToLocale && x.Key.ToLower().StartsWith(lookUpPath.ToLower())).ToList();
                    dmeta = repo.GetPuckMeta().Where(x => x.Name == DBNames.DomainMapping && x.Key.ToLower().StartsWith(lookUpPath.ToLower())).ToList();
                    cmeta = repo.GetPuckMeta().Where(x => x.Name == DBNames.CacheExclude && x.Key.ToLower().StartsWith(lookUpPath.ToLower())).ToList();
                    nmeta = repo.GetPuckMeta().Where(x => x.Name.StartsWith(DBNames.Notify) && (
                         x.Key.ToLower().Equals(lookUpPath.ToLower())
                        || (lookUpPath.ToLower().StartsWith(x.Key.ToLower()) && x.Name.Contains(":*:"))
                        )).ToList();
                }
                lmeta.ForEach(x => { repo.DeleteMeta(x); });
                dmeta.ForEach(x => { repo.DeleteMeta(x); });
                cmeta.ForEach(x => { repo.DeleteMeta(x); });
                nmeta.ForEach(x => { repo.DeleteMeta(x); });
            }
            StateHelper.UpdateDomainMappings();
            StateHelper.UpdatePathLocaleMappings();
            repo.SaveChanges();                         
        }
        public static string GetLiveOrCurrentPath(Guid id) {
            var repo = Repo;
            var node=repo.GetPuckRevision()
                .Where(x => x.Id == id && ((x.HasNoPublishedRevision && x.Current) || x.IsPublishedRevision)).OrderByDescending(x=>x.Updated).FirstOrDefault();
            return node?.Path;
        }
        public static T Create<T>(Guid parentId,string variant,string name,string template=null,bool published=false,string userName=null) where T : BaseModel {
            var repo = Repo;
            var instance = (T)ApiHelper.CreateInstance(typeof(T));
            if (parentId != Guid.Empty)
            {
                var parent = repo.GetPuckRevision().FirstOrDefault(x => x.Id == parentId);
                if (parent == null)
                    throw new Exception("could not find parent node");
                var slug = Slugify(name);
                instance.Path = $"{parent.Path}/";
            }
            else
                instance.Path = $"/";
            if (!string.IsNullOrEmpty(template))
                instance.TemplatePath = template;
            else {
                var meta = repo.GetPuckMeta()
                    .Where(x => x.Name == DBNames.TypeAllowedTemplates && x.Key == typeof(T).AssemblyQualifiedName)
                    .ToList();
                if (meta == null||meta.Count==0) {
                    throw new Exception($"you've not specified a template parameter. tried to pick one from allowable templates (set in settings section) but none have been set for type:{FriendlyClassName(typeof(T))}");
                }
                instance.TemplatePath = meta.FirstOrDefault().Value;
            }
            instance.NodeName = name;
            instance.Variant = variant;
            instance.TypeChain = ApiHelper.TypeChain(typeof(T));
            instance.Type = typeof(T).AssemblyQualifiedName;
            if (string.IsNullOrEmpty(userName))
            {
                instance.CreatedBy = HttpContext.Current.User.Identity.Name;
            }
            else {
                var user = userManager.FindByName(userName);
                if (user == null) throw new Exception("there is no user for provided username");
                instance.CreatedBy = userName;
            }
            
            instance.LastEditedBy = instance.CreatedBy;
            instance.Published = published;
            return instance;
        }
        public static string GetIdPath(BaseModel mod) {
            var repo = Repo;
            if (mod.ParentId == Guid.Empty) {
                return mod.Id.ToString();
            }
            var chain = new List<string>();
            chain.Add(mod.Id.ToString());
            var currentRevision = repo.GetPuckRevision().FirstOrDefault(x=>x.Id==mod.ParentId&&x.Current);
            chain.Add(currentRevision.Id.ToString());
            while (currentRevision.ParentId != Guid.Empty) {
                currentRevision = repo.GetPuckRevision().FirstOrDefault(x => x.Id == currentRevision.ParentId && x.Current);
                chain.Add(currentRevision.Id.ToString());
            }
            chain.Reverse();
            var result = string.Join(",", chain);
            return result;
        }
        public static int UpdateDescendantPaths(string oldPath,string newPath) {
            int rowsAffected=0;
            using (var con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["PuckContext"].ConnectionString))
            {
                var sql = "update PuckRevisions set [Path] = @newPath + SUBSTRING([Path], LEN(@oldPath)+1,8000) where [Path] LIKE @likeStr";
                var com = new SqlCommand(sql, con);
                com.Parameters.AddWithValue("@likeStr",oldPath+"%");
                com.Parameters.AddWithValue("@oldPath", oldPath);
                com.Parameters.AddWithValue("@newPath",newPath);
                con.Open();
                rowsAffected = com.ExecuteNonQuery();
            }
            /*
            var context = new PuckContext();
            var rowsAffected = context.Database.ExecuteSqlCommand(
                "update PuckRevisions set [Path] = @newPath + SUBSTRING([Path], LEN(@oldPath)+1,8000) where [Path] LIKE @oldPath%"
      
            ,new SqlParameter("@oldPath",oldPath)
                ,new SqlParameter("@newPath",newPath)
            );*/
            return rowsAffected;
        }
        public static int UpdateDescendantIdPaths(string oldPath, string newPath)
        {
            int rowsAffected = 0;
            using (var con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["PuckContext"].ConnectionString))
            {
                var sql = "update PuckRevisions set [IdPath] = @newPath + SUBSTRING([IdPath], LEN(@oldPath)+1,8000) where [IdPath] LIKE @likeStr";
                var com = new SqlCommand(sql, con);
                com.Parameters.AddWithValue("@likeStr", oldPath + "%");
                com.Parameters.AddWithValue("@oldPath", oldPath);
                com.Parameters.AddWithValue("@newPath", newPath);
                con.Open();
                rowsAffected = com.ExecuteNonQuery();
            }
            return rowsAffected;
        }
        public static void UpdatePathRelatedMeta(string oldPath,string newPath) {
            var repo = Repo;

            var regex = new Regex(Regex.Escape(oldPath), RegexOptions.Compiled);

            var lmeta = repo.GetPuckMeta().Where(x => x.Name == DBNames.PathToLocale && x.Key.ToLower().Equals(oldPath.ToLower())).ToList();
            lmeta.ForEach(x => x.Key = newPath);
            var lmetaD = repo.GetPuckMeta().Where(x => x.Name == DBNames.PathToLocale && x.Key.ToLower().StartsWith(oldPath.ToLower() + "/")).ToList();
            lmetaD.ForEach(x => x.Key = regex.Replace(x.Key, newPath, 1));

            var dmeta = repo.GetPuckMeta().Where(x => x.Name == DBNames.DomainMapping && x.Key.ToLower().Equals(oldPath.ToLower())).ToList();
            dmeta.ForEach(x => x.Key = newPath);

            var cmeta = repo.GetPuckMeta().Where(x => x.Name == DBNames.CacheExclude && x.Key.ToLower().Equals(oldPath.ToLower())).ToList();
            cmeta.ForEach(x => x.Key = newPath);
            var cmetaD = repo.GetPuckMeta().Where(x => x.Name == DBNames.CacheExclude && x.Key.ToLower().StartsWith(oldPath.ToLower() + "/")).ToList();
            cmetaD.ForEach(x => x.Key = regex.Replace(x.Key, newPath, 1));

            var nmeta = repo.GetPuckMeta().Where(x => x.Name.StartsWith(DBNames.Notify) && x.Key.ToLower().Equals(oldPath.ToLower())).ToList();
            nmeta.ForEach(x => x.Key = newPath);
            var nmetaD = repo.GetPuckMeta().Where(x => x.Name.StartsWith(DBNames.Notify) && x.Key.ToLower().StartsWith(oldPath.ToLower() + "/")).ToList();
            nmetaD.ForEach(x => x.Key = regex.Replace(x.Key, newPath, 1));

            repo.SaveChanges();

        }
        public static void SaveContent<T>(T mod,bool makeRevision=true,string userName = null) where T : BaseModel {
            lock (_savelck)
            {
                PuckUser user = null;
                if (!string.IsNullOrEmpty(userName)) {
                    user = userManager.FindByName(userName);
                    if (user == null)
                        throw new Exception("there is no user for provided username");
                }
                ObjectDumper.Transform(mod, int.MaxValue);
                var beforeArgs = new BeforeIndexingEventArgs { Node = mod };
                OnBeforeIndex(null, beforeArgs);
                if (beforeArgs.Cancel)
                    throw new SaveCancelledException("Saving was cancelled by a custom event handler");

                var repo = Repo;
                var revisions = repo.GetPuckRevision().Where(x => x.Id.Equals(mod.Id) && x.Variant.ToLower().Equals(mod.Variant.ToLower())).ToList();
                if (makeRevision)
                {
                    if (revisions.Count() == 0)
                        mod.Revision = 1;
                    else
                        mod.Revision = revisions.Max(x => x.Revision) + 1;
                }
                mod.Updated = DateTime.Now;
                //get parent check published
                var parentVariants = repo.GetPuckRevision().Where(x=>x.Id==mod.ParentId&&x.Current).ToList();
                if (mod.ParentId!=Guid.Empty && parentVariants.Count() == 0)
                    throw new NoParentExistsException("this is not a root node yet doesn't have a parent");
                //can't publish if parent not published
                var publishedParentVariants = repo.PublishedRevisions(mod.ParentId).ToList();
                if (mod.ParentId!=Guid.Empty && !publishedParentVariants.Any())//!parentVariants.Any(x => x.Published /*&& x.Variant.ToLower().Equals(mod.Variant.ToLower())*/))
                    mod.Published = false;
                //get sibling nodes
                if (mod.ParentId == Guid.Empty)
                {
                    mod.Path = "/" + Slugify(mod.NodeName);
                }
                else {
                    var parentPath = GetLiveOrCurrentPath(mod.ParentId);
                    mod.Path = $"{parentPath}/{Slugify(mod.NodeName)}";
                }
                var nodeDirectory = mod.Path.Substring(0, mod.Path.LastIndexOf('/') + 1);
                
                var nodesAtPath = repo.CurrentRevisionsByParentId(mod.ParentId).Where(x => x.Id != mod.Id)
                    .ToList()
                    .Select(x =>
                        RevisionToBaseModel(x)
                        ).Where(x => x != null).ToList().GroupByID();
                //set sort order for new content
                if (mod.SortOrder == -1)
                    mod.SortOrder = nodesAtPath.Count;
                //check node name is unique at path
                if (nodesAtPath.Any(x => x.Value.Any(y => y.Value.NodeName.ToLower().Equals(mod.NodeName))))
                    throw new NodeNameExistsException($"Nodename:{mod.NodeName} exists at this path:{nodeDirectory}, choose another.");
                //check this is an update or create
                var original = repo.CurrentRevision(mod.Id, mod.Variant);
                var publishedRevision = repo.PublishedRevision(mod.Id, mod.Variant);
                //could be published revision, or even a published variant
                var publishedRevisionOrVariant = repo.PublishedRevisions(mod.Id).OrderByDescending(x => x.Updated).FirstOrDefault();
                var toIndex = new List<BaseModel>();
                //toIndex.Add(mod);
                bool nameChanged = false;
                bool nameDifferentThanCurrent = false;
                bool parentChanged = false;
                string currentRevisionPath = string.Empty;
                bool nameDifferentThanPublished = false;
                string publishedRevisionPath = string.Empty;
                string originalPath = string.Empty;
                bool publishedRevisionRepublished = false;
                if (original != null)
                {//this must be an edit
                 //if (!original.NodeName.ToLower().Equals(mod.NodeName.ToLower()))
                    if (original.ParentId != mod.ParentId) {
                        parentChanged = true;
                    }
                    if (!original.Path.ToLower().Equals(mod.Path.ToLower()))
                    {
                        nameChanged = true;
                        nameDifferentThanCurrent = true;
                        currentRevisionPath = original.Path;
                        originalPath = original.Path;
                    }
                }
                if (publishedRevisionOrVariant != null)
                {
                 //if (!original.NodeName.ToLower().Equals(mod.NodeName.ToLower()))
                    if (!publishedRevisionOrVariant.Path.ToLower().Equals(mod.Path.ToLower()))
                    {
                        nameChanged = true;
                        nameDifferentThanPublished = true;
                        publishedRevisionPath = publishedRevisionOrVariant.Path;
                        originalPath = original.Path;
                    }
                }
                var idPath= GetIdPath(mod);
                var currentVariantsDb = repo.CurrentRevisionVariants(mod.Id, mod.Variant).ToList();
                var publishedVariantsDb = repo.PublishedRevisionVariants(mod.Id,mod.Variant).ToList();
                var hasNoPublishedVariants = publishedVariantsDb.Count == 0;
                //if (variantsDb.Any(x => !x.NodeName.ToLower().Equals(mod.NodeName.ToLower())))
                if (currentVariantsDb.Any(x => x.ParentId!=mod.ParentId))
                {//update parentId of variants
                    currentVariantsDb.ForEach(x => { x.ParentId = mod.ParentId;x.IdPath = idPath; });
                }
                if (publishedVariantsDb.Any(x => x.ParentId != mod.ParentId))
                {//update parentId of variants
                    publishedVariantsDb.ForEach(x => { x.ParentId = mod.ParentId; x.IdPath = idPath; });
                }
                if (!mod.Published)
                {
                    if (currentVariantsDb.Where(x => !x.Published).Any(x => !x.Path.ToLower().Equals(mod.Path.ToLower())))
                    {//update path of variants
                        nameChanged = true;
                        if (string.IsNullOrEmpty(originalPath))
                            originalPath = currentVariantsDb.First().Path;
                        currentVariantsDb.Where(x => !x.Published).ToList().ForEach(x => { x.NodeName = mod.NodeName; x.Path = mod.Path; });
                    }
                }
                else {
                    if (publishedVariantsDb.Any(x => !x.Path.ToLower().Equals(mod.Path.ToLower())))
                    {//update path of published variants
                        nameChanged = true;
                        if (string.IsNullOrEmpty(originalPath))
                            originalPath = publishedVariantsDb.First().Path;
                        publishedVariantsDb.ToList().ForEach(x => { x.NodeName = mod.NodeName; x.Path = mod.Path; });
                    }
                }
                
                var pAffected = 0;
                var affected = 0;
                if (parentChanged) {
                    pAffected = UpdateDescendantIdPaths(original.IdPath,idPath);
                    if (!string.IsNullOrEmpty(publishedRevisionPath))
                    {
                        //update descendant paths(publishedRevisionPath)
                        affected = UpdateDescendantPaths(publishedRevisionPath + "/", mod.Path + "/");
                        UpdatePathRelatedMeta(publishedRevisionPath,mod.Path);
                    }
                    else
                    {
                        //update descendant paths(currentRevisionPath)
                        affected = UpdateDescendantPaths(currentRevisionPath + "/", mod.Path + "/");
                        UpdatePathRelatedMeta(currentRevisionPath, mod.Path);
                    }
                    var descendants = repo.CurrentOrPublishedDescendants(idPath).ToList().Select(x=>ApiHelper.RevisionToBaseModel(x)).ToList();
                    var variantsToIndex = new List<BaseModel>();
                    variantsToIndex.AddRange(currentVariantsDb.Select(x => ApiHelper.RevisionToBaseModel(x)).ToList());
                    variantsToIndex.AddRange(publishedVariantsDb.Select(x => ApiHelper.RevisionToBaseModel(x)).ToList());
                    toIndex.AddRange(descendants);
                    toIndex.AddRange(variantsToIndex);
                    //if current model is not set to publish, update the currently published revision to reflect parent id being changed
                    if (publishedRevision != null && !mod.Published) {
                        publishedRevision.IdPath = idPath;
                        publishedRevision.Path = mod.Path;
                        toIndex.Add(publishedRevision);
                    }
                    else
                        toIndex.Add(mod);
                }
                else
                {
                    if (original != null && original.HasNoPublishedRevision && hasNoPublishedVariants && !mod.Published && nameDifferentThanCurrent)
                    {
                        //update descendant paths
                        affected = UpdateDescendantPaths(original.Path + "/", mod.Path + "/");
                        UpdatePathRelatedMeta(original.Path, mod.Path);
                    }
                    if (mod.Published && (nameDifferentThanCurrent || nameDifferentThanPublished))
                    {
                        if (!string.IsNullOrEmpty(publishedRevisionPath))
                        {
                            //update descendant paths(publishedRevisionPath)
                            affected = UpdateDescendantPaths(publishedRevisionPath + "/", mod.Path + "/");
                            UpdatePathRelatedMeta(publishedRevisionPath, mod.Path);
                        }
                        else
                        {
                            //update descendant paths(currentRevisionPath)
                            affected = UpdateDescendantPaths(currentRevisionPath + "/", mod.Path + "/");
                            UpdatePathRelatedMeta(currentRevisionPath, mod.Path);
                        }
                    }
                }
                
                /*if (nameChanged)
                {
                    var regex = new Regex(Regex.Escape(originalPath), RegexOptions.Compiled);
                    //update path of decendants
                    var descendantsDb = repo.CurrentRevisionDescendants(originalPath).ToList();
                    descendantsDb.ForEach(x => { x.Path = regex.Replace(x.Path, mod.Path, 1); });
                    repo.GetPuckMeta().Where(x => x.Name.StartsWith(DBNames.Notify))
                            .Where(x => x.Key.ToLower().Equals(originalPath.ToLower())
                            || originalPath.ToLower().StartsWith(x.Key.ToLower()) && x.Name.Contains(":*:"))
                            .ToList()
                            .ForEach(x => x.Key = mod.Path);
                }
                */
                //add revision
                PuckRevision revision;
                if (makeRevision)
                {
                    revision = new PuckRevision();
                    repo.GetPuckRevision()
                        .Where(x => x.Id.Equals(mod.Id) && x.Variant.ToLower().Equals(mod.Variant.ToLower()) && x.Current)
                        .ToList()
                        .ForEach(x => x.Current = false);
                    repo.AddRevision(revision);
                }
                else
                {
                    revision = repo.GetPuckRevision()
                        .Where(x => x.Id.Equals(mod.Id) && x.Variant.ToLower().Equals(mod.Variant.ToLower()) && x.Current).FirstOrDefault();
                    if (revision == null)
                    {
                        revision = new PuckRevision();
                        repo.AddRevision(revision);
                    }
                }
                revision.IdPath = idPath;
                if(user==null)
                    revision.LastEditedBy = HttpContext.Current.User.Identity.Name;
                else
                    revision.LastEditedBy = user.UserName;
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
                revision.ParentId = mod.ParentId;
                revision.Value = JsonConvert.SerializeObject(mod);
                //if published, set the currently published revision. this requires unsetting any previously set publishedrevision flag
                if (mod.Published) {
                    revisions.ForEach(x=>x.IsPublishedRevision=false);
                    revision.IsPublishedRevision = true;
                }
                //if this revision or any previous revisions have a published revision, HasNoPublishedRevision must be false
                if (revision.IsPublishedRevision || revisions.Any(x => x.IsPublishedRevision))
                {
                    revision.HasNoPublishedRevision = false;
                    revisions.ForEach(x => x.HasNoPublishedRevision = false);
                }
                else {//current revision or previous revisions don't have have IsPublishedRevision set so this must mean there is no published revision
                    revision.HasNoPublishedRevision = true;
                    revisions.ForEach(x => x.HasNoPublishedRevision = true);
                }

                //if first time node saved and is root node - set locale for path
                if (currentVariantsDb.Count == 0 && (original == null) && mod.ParentId==Guid.Empty)
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
                        repo.AddMeta(dMeta);
                    }
                }
                repo.SaveChanges();
                StateHelper.UpdateDomainMappings();
                StateHelper.UpdatePathLocaleMappings();

                //index related operations
                var qh = new QueryHelper<BaseModel>();
                //get current indexed node with same ID and VARIANT
                var currentMod = qh.And().Field(x => x.Variant, mod.Variant)
                    .ID(mod.Id)
                    .Get();
                //if parent changed, we index regardless of if the model being saved is set to publish or not. moves are always published immediately
                if (parentChanged) {
                    indexer.Index(toIndex);
                }
                else if (mod.Published || currentMod == null)//add to lucene index if published or no such node exists in index
                                                        /*note that you can only have one node with particular id/variant in index at any one time
                                                         * the reason that you want to add node to index when it's not published but there is no such node currently in index
                                                         * is to make sure there is always at least one version of the node in the index for back office search operations
                                                         */
                {
                    toIndex.Add(mod);
                    var changed = false;
                    var indexOriginalPath = string.Empty;
                    //if node exists in index
                    if (currentMod != null)
                    {
                        //and that node currently has a different path than the node we're indexing to replace it
                        if (!mod.Path.ToLower().Equals(currentMod.Path.ToLower()))
                        {
                            //means we have changed the path - by changing the nodename
                            changed = true;
                            //set the original path so we can use it for regex replace operation for changing descendants who will otherwise have incorrect paths
                            indexOriginalPath = currentMod.Path;
                        }
                    }
                    //get nodes currently indexed which have the same ID but different VARIANT
                    var variants = mod.Variants<BaseModel>(noCast: true);
                    if (variants.Any(x => x.ParentId != mod.ParentId)) {
                        variants.ForEach(x=> { x.ParentId = mod.ParentId;toIndex.Add(x); });
                    }
                    //if any of the variants have different path to the current node
                    if (variants.Any(x => !x.Path.ToLower().Equals(mod.Path.ToLower())))
                    {
                        //means we have changed the path - by changing the nodename
                        changed = true;
                        //if the original path hasn't been set already, set it for use in a regex replace operation
                        if (string.IsNullOrEmpty(indexOriginalPath))
                            indexOriginalPath = variants.First().Path;
                    }
                    //if there was a change in the path
                    if (changed &&mod.Published)
                    {
                        //sync up all the variants so they have the same nodename and path
                        variants.ForEach(x => { x.NodeName = mod.NodeName; x.Path = mod.Path;
                            if(!toIndex.Contains(x))
                                toIndex.Add(x);
                        });
                        //new regex which searches for the current indexed path so it can be replaced with the new one
                        var regex = new Regex(Regex.Escape(indexOriginalPath), RegexOptions.Compiled);
                        var descendants = new List<BaseModel>();
                        //get descendants - either from currently indexed version of the node we're currently saving (which may be new variant and so not currently indexed) or from its variants.
                        if (currentMod != null)
                            descendants = currentMod.Descendants<BaseModel>(currentLanguage: false, noCast: true);
                        else if (variants.Any())
                            descendants = variants.First().Descendants<BaseModel>(currentLanguage: false, noCast: true);
                        //replace portion of path that has changed
                        descendants.ForEach(x => { x.Path = regex.Replace(x.Path, mod.Path, 1); toIndex.Add(x); });
                        //delete previous meta binding
                        repo.GetPuckMeta().Where(x => x.Name == DBNames.PathToLocale && x.Key.ToLower().Equals(originalPath.ToLower())).ToList()
                            .ForEach(x => x.Key = mod.Path);
                        repo.GetPuckMeta().Where(x => x.Name == DBNames.DomainMapping && x.Key.ToLower().Equals(originalPath.ToLower())).ToList()
                            .ForEach(x => x.Key = mod.Path);
                        repo.SaveChanges();
                        StateHelper.UpdateDomainMappings();
                        StateHelper.UpdatePathLocaleMappings();
                    }
                    indexer.Index(toIndex);
                }

                var afterArgs = new IndexingEventArgs { Node = mod };
                OnAfterIndex(null, afterArgs);
                if (toIndex.Count > 0) {
                    var instruction = new PuckInstruction() {InstructionKey=InstructionKeys.Publish,Count=toIndex.Count,ServerName=ServerName() };
                    string instructionDetail = "";
                    toIndex.ForEach(x=>instructionDetail+=$"{x.Id.ToString()}:{x.Variant},");
                    instructionDetail = instructionDetail.TrimEnd(',');
                    instruction.InstructionDetail = instructionDetail;
                    repo.AddPuckInstruction(instruction);
                    repo.SaveChanges();
                }                
            }
        }
        public static void RePublishEntireSite() {
            var repo = Repo;
            lock (_savelck)
            {
                try
                {
                    ((Puck_Repository)repo).repo.Configuration.AutoDetectChangesEnabled = false;
                    var toIndex = new List<BaseModel>();
                    using (MiniProfiler.Current.Step("get all models"))
                    {
                        toIndex = repo.GetPuckRevision().Where(x => x.Current).ToList().Select(x => RevisionToBaseModel(x)).ToList();
                    }
                    var qh = new QueryHelper<BaseModel>(prependTypeTerm: false);
                    qh.And().Field(x => x.TypeChain, typeof(BaseModel).FullName.Wrap());
                    var query = qh.ToString();
                    using (MiniProfiler.Current.Step("delete models"))
                    {
                        indexer.Delete(query, reloadSearcher: false);
                    }
                    using (MiniProfiler.Current.Step("index models"))
                    {
                        indexer.Index(toIndex);
                    }
                }
                catch (Exception ex)
                {
                    throw;
                }
                finally {
                    ((Puck_Repository)repo).repo.Configuration.AutoDetectChangesEnabled = true;
                }
            }
        }
        public static void RePublishEntireSite2()
        {
            PuckCache.IsRepublishingEntireSite = true;
            var repo = Repo;
            lock (_savelck)
            {
                try
                {
                    var values = new List<string>();
                    var models = new List<BaseModel>();
                    var typeAndValues = new List<KeyValuePair<string, string>>();
                    using (var con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["PuckContext"].ConnectionString)) {
                        PuckCache.IndexingStatus = $"retrieving records to republish";
                        var sql = "SELECT Path,Type,Value FROM PuckRevisions where ([IsPublishedRevision] = 1 OR ([HasNoPublishedRevision]=1 AND [Current] = 1))";
                        var com = new SqlCommand(sql,con);
                        //com.Parameters.AddWithValue("@pricePoint", paramValue);
                        con.Open();
                        SqlDataReader reader = com.ExecuteReader();
                        using (MiniProfiler.Current.Step("get all models"))
                        {
                            while (reader.Read())
                            {
                                var aqn = reader.GetString(1);
                                var value = reader.GetString(2);
                                //var model = JsonConvert.DeserializeObject(reader.GetString(2), ApiHelper.GetType(aqn)) as BaseModel;
                                typeAndValues.Add(new KeyValuePair<string, string>(aqn,value));
                                //values.Add(reader.GetString(2));
                            }
                        }
                        reader.Close();
                    }
                    using (MiniProfiler.Current.Step("deserialize"))
                    {
                        PuckCache.IndexingStatus = $"deserializing records";
                        foreach (var item in typeAndValues)
                        {
                            try
                            {
                                var model = JsonConvert.DeserializeObject(item.Value,ApiHelper.GetType(item.Key)) as BaseModel;
                                models.Add(model);
                                //values.Add(reader.GetString(2));
                            }
                            catch (Exception ex) { }

                        }
                    }
                    var qh = new QueryHelper<BaseModel>(prependTypeTerm: false);
                    qh.And().Field(x => x.TypeChain, typeof(BaseModel).FullName.Wrap());
                    var query = qh.ToString();
                    PuckCache.IndexingStatus = $"deleting all indexed items";
                    using (MiniProfiler.Current.Step("delete models"))
                    {
                        indexer.Delete(query, reloadSearcher: false);
                    }
                    using (MiniProfiler.Current.Step("index models"))
                    {
                        indexer.Index(models,triggerEvents:false);
                    }
                }
                catch (Exception ex)
                {
                    //PuckCache.IsRepublishingEntireSite = false;
                    //PuckCache.IndexingStatus = "";
                    logger.Log(ex);
                }
                finally
                {
                    PuckCache.IsRepublishingEntireSite = false;
                    PuckCache.IndexingStatus = "";

                }
            }
        }
        public static List<BaseTask> Tasks()
        {
            var repo = Repo;
            var result = new List<BaseTask>();
            var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.Tasks).ToList();
            var toRemove = new List<PuckMeta>();
            meta.ForEach(x => {
                var type = Type.GetType(x.Key);
                if (type == null)
                {
                    toRemove.Add(x);
                    return;
                }
                var instance = JsonConvert.DeserializeObject(x.Value, type) as BaseTask;
                instance.ID = x.ID;
                if (!tdispatcher.CanRun(instance)) {
                    toRemove.Add(x);
                    return;
                }
                result.Add(instance);
            });
            toRemove.ForEach(x => repo.DeleteMeta(x));
            repo.SaveChanges();
            return result;
        }
        public static List<BaseTask> SystemTasks() {
            var result = new List<BaseTask>();
            result.Add(new SyncCheckTask());
            return result;
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
        public static void Copy(Guid id,Guid parentId,bool includeDescendants) {
            var repo = Repo;

            var itemsToCopy = repo.GetPuckRevision().Where(x => x.Id == id && x.Current).ToList();
            if (itemsToCopy.Count == 0) return;
            var descendants = new List<PuckRevision>();
            if(includeDescendants)
                descendants = repo.CurrentRevisionDescendants(itemsToCopy.First().IdPath).ToList();

            var allItemsToCopy = new List<PuckRevision>();
            allItemsToCopy.AddRange(itemsToCopy);
            allItemsToCopy.AddRange(descendants);

            var ids = allItemsToCopy.Select(x => x.Id).Distinct();

            var idMap = new Dictionary<Guid, Guid>();

            foreach (var guid in ids) {
                idMap[guid] = Guid.NewGuid();
            }

            foreach (var item in allItemsToCopy) {
                if (idMap.ContainsKey(item.Id))
                    item.Id = idMap[item.Id];
                if (idMap.ContainsKey(item.ParentId))
                    item.ParentId = idMap[item.ParentId];
            }

            itemsToCopy.ForEach(x => x.ParentId = parentId);

            void SaveCopies(Guid pId,List<PuckRevision> items){
                var children = items.Where(x => x.ParentId == pId).ToList();
                var childrenGroupedById = children.GroupBy(x=>x.Id);
                foreach (var group in childrenGroupedById) {
                    foreach (var revision in group) {
                        var model = ApiHelper.RevisionToBaseModel(revision);
                        SaveContent(model);
                    }
                    SaveCopies(group.Key,items);
                }
            }

            SaveCopies(parentId,allItemsToCopy);

        }
        public static void Move(Guid nodeId, Guid destinationId)
        {
            var repo = Repo;
            var startRevisions = repo.GetPuckRevision().Where(x => x.Id == nodeId && x.Current).ToList();
            var destinationRevisions = repo.GetPuckRevision().Where(x=>x.Id==destinationId && x.Current).ToList();
            if (startRevisions.Count==0) throw new Exception("cannot find start node");
            if (destinationRevisions.Count == 0) throw new Exception("cannot find destination node");
            if (destinationRevisions.FirstOrDefault().IdPath.ToLower().StartsWith(startRevisions.FirstOrDefault().IdPath.ToLower()))
                throw new Exception("cannot move parent node to child");
            if (startRevisions.FirstOrDefault().ParentId==Guid.Empty)
                throw new Exception("cannot move root node");
            var startNodes = startRevisions.Select(x => RevisionToBaseModel(x)).ToList();
            var destinationNodes = destinationRevisions.Select(x => RevisionToBaseModel(x)).ToList();
            var beforeArgs = new BeforeMoveEventArgs {
                Nodes = startNodes
                , DestinationNodes = destinationNodes
            };
            OnBeforeMove(null, beforeArgs);
            if (!beforeArgs.Cancel)
            {
                startNodes.ForEach(x=>x.ParentId=destinationId);
                var startNode = startNodes.FirstOrDefault();
                SaveContent(startNode, makeRevision: false);
                var afterArgs = new MoveEventArgs { Nodes = startNodes, DestinationNodes = startNodes};
                OnAfterMove(null, afterArgs);
            }
            else {
                throw new Exception("Move cancelled by custom event handler.");
            }
        }
        public static void Move(string start, string destination) {
            var repo = Repo;
            if (destination.ToLower().StartsWith(start.ToLower()))
                throw new Exception("cannot move parent node to child");
            if (start.Count(x => x == '/') == 1)
                throw new Exception("cannot move root node");
            var toMove = repo.CurrentRevisionsByPath(start).FirstOrDefault();
            if (!destination.EndsWith("/"))
                destination += "/";
            toMove.Path = destination + toMove.NodeName;
            
            var startRevisions = repo.CurrentRevisionsByPath(start).ToList().Cast<BaseModel>().ToList();
            var destinationRevisions = repo.CurrentRevisionsByPath(destination.TrimEnd('/')).ToList().Cast<BaseModel>().ToList();
            var beforeArgs = new BeforeMoveEventArgs { Nodes=startRevisions,DestinationNodes=destinationRevisions};
            OnBeforeMove(null, beforeArgs);
            if (!beforeArgs.Cancel)
            {
                SaveContent(toMove, makeRevision: false);
                startRevisions = repo.CurrentRevisionsByPath(toMove.Path).ToList().Cast<BaseModel>().ToList();
                var afterArgs = new MoveEventArgs {Nodes=startRevisions, DestinationNodes=destinationRevisions };
                OnAfterMove(null,afterArgs);
            }
        }
        public static Notify NotifyModel(string path){
            //:actions
            //save
            //publish
            //delete
            //move

            //:target
            //recursive
            //path

            //:filter
            //users            

            //DBNAME
            //notify:admin:save(|publish|delete|move|)
            //DBKEY
            //|user|user1|etc|
            //VALUE
            //content/home/*
            var repo = Repo;
            var username = HttpContext.Current.User.Identity.Name;
            var model = new Notify { Path = path, Actions = new List<string>(), Users = new List<string>() };
            var notify = repo.GetPuckMeta()
                .Where(x => x.Name.StartsWith(DBNames.Notify))
                .Where(x => x.Key.Equals(path))
                .Where(x=>x.Value==username)
                .FirstOrDefault();
            if (notify != null)
            {
                var actions = notify.Name.Substring((DBNames.Notify+":*").Length);
                var actionList = actions.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                //var usersList = notify.Value.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                model.Actions = actionList;
                //model.Users = usersList;
                model.Recursive = notify.Name.Contains(":*");
            }
            model.AllActions = Enum.GetNames(typeof(PuckCache.NotifyActions)).Select(x => new SelectListItem() { Text = x, Value = x, Selected = model.Actions.Contains(x) });
            //model.AllUsers = Roles.GetUsersInRole(PuckRoles.Puck).ToList().Select(x => new SelectListItem() { Text = x, Value = x, Selected = model.Users.Contains(x) });
            return model;
        }
        public static void SetNotify(Notify model) {
            var repo = Repo;
            model.Actions = model.Actions ?? new List<string>();
            model.Users = model.Users ?? new List<string>();
            var username = HttpContext.Current.User.Identity.Name;
            var dbname = string.Concat(DBNames.Notify, ":", model.Recursive ? "*" : ".", string.Join("", model.Actions.Select(x => ":" + x)));
            var dbkey = model.Path;
            var dbvalue = username;
            repo.GetPuckMeta()
                .Where(x => x.Name.StartsWith(DBNames.Notify))
                .Where(x=>x.Key.Equals(model.Path))
                .Where(x=>x.Value.Equals(username))
                .ToList()
                .ForEach(x => repo.DeleteMeta(x));
            var newMeta = new PuckMeta { 
                Key=dbkey,
                Name=dbname,
                Value=dbvalue
            };
            repo.AddMeta(newMeta);
            repo.SaveChanges();
        }
        public static List<PuckUser> UsersToNotify(string path, PuckCache.NotifyActions action)
        {
            var repo = Repo;
            //var user = HttpContext.Current.User.Identity.Name;
            var strAction = action.ToString();
            var metas = repo.GetPuckMeta()
                .Where(x => x.Name.Contains(":" + strAction + ":"))
                .Where(
                    x => x.Key.Equals(path) && x.Name.Contains(":.:")
                    ||
                    x.Key.StartsWith(path) && x.Name.Contains(":*:")
                )
                .ToList();
            var usernames = metas.Select(x => x.Value).ToList();
            var users = usernames.Select(x => userManager.FindByName(x)).Where(x=>x!=null).ToList();
            return users;
        }
        public static List<GeneratedProperty> AllProperties(GeneratedModel model) {
            var repo = Repo;
            var result = new List<GeneratedProperty>();
            var mod = model;
            do{
                result.AddRange(mod.Properties.ToList());
                if (!string.IsNullOrEmpty(mod.Inherits))
                    mod = repo.GetGeneratedModel().Where(x => x.IFullName == mod.Inherits).SingleOrDefault();
                else
                    mod = null;
            }while(mod!=null);
            return result;
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
                var targetType = ApiHelper.GetType(type);
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
        
        public static List<FileInfo> AllowedViews(string type,string[] excludePaths = null) {
            var repo = Repo;
            var paths = repo.GetPuckMeta().Where(x => x.Name == DBNames.TypeAllowedTemplates && x.Key.Equals(type))
                .Select(x=>x.Value)
                .ToList();
            return Views(excludePaths).Where(x => paths.Contains(ToVirtualPath(x.FullName))).ToList();
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
        public static List<Type> AllowedTypes(string typeName) {
            var repo = Repo;
            var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.TypeAllowedTypes && x.Key.Equals(typeName)).ToList();
            var result = meta.Select(x=>ApiHelper.GetType(x.Value)).ToList();
            return result;
        }
        
        public static List<Type> AllModels(bool inclusive = false) {
            var models = Models(inclusive);
            var gmodels = GeneratedModelTypes();
            models.AddRange(gmodels);
            return models;
        }
        /*
        public static List<Type> GeneratedModels() {
            var models = new List<Type>();
            Repo.GetGeneratedModel().ToList()
                .ForEach(x=>models.Add(Type.GetType(x.IFullName)));
            return models;
        } 
        */
        public static List<GeneratedModel> GeneratedModels() {
            var models = Repo.GetGeneratedModel().ToList();
            return models;
        }
        public static List<Type> GeneratedModelTypes (List<Type> excluded = null){
            var repo = Repo;
            var models = repo.GetGeneratedModel()
                .Where(x=>!string.IsNullOrEmpty(x.IFullName))
                .ToList()
                .Select(x => GetType(x.IFullName))
                .Where(x=>x!=null)
                .ToList();
            if (excluded != null) {
                models = models.Except(excluded).ToList();
            }
            return models;
        }
        public static List<Type> Models(bool inclusive=false) {
            var excluded = new List<Type>() { typeof(PuckRevision)};
            var igenerated = FindDerivedClasses(typeof(I_Generated)).Where(x=>x.IsInterface);
            var generated = new List<Type>();
            igenerated.ToList().ForEach(x => {
                var concrete = FindDerivedClasses(x);
                generated.AddRange(concrete);
            });
            excluded.AddRange(generated);
            var result = FindDerivedClasses(typeof(BaseModel),excluded,inclusive).ToList();
            //result.AddRange(igenerated);
            return result;
        }
        public static List<string> OrphanedTypeNames() {
            var repo = Repo;
            var loadedTypes = Models().Select(x => x.AssemblyQualifiedName).ToList();
            var names = repo.GetPuckRevision().Where(x => !loadedTypes.Contains(x.Type)).Select(x => x.Type).Distinct().ToList();
            return names;
        }
        public static void RenameOrphaned(string orphanTypeName,string newTypeName)
        {
            var repo = Repo;
            var newType = ApiHelper.GetType(newTypeName);
            var newTypeChain = TypeChain(newType);
            var indexChecked = new HashSet<string>();
            //determines how many db revisions to get at once and also the reindex threshhold - useful for handling large amount of data without raping server resources.
            var step = 1000;
            var toIndex = new List<BaseModel>();
            //we're doing this in chunks - while there are still chunks to process
            while (repo.GetPuckRevision().Where(x => x.Type.Equals(orphanTypeName)).Count()>0)
            {
                //get next chunk from database
                var records = repo.GetPuckRevision().Where(x => x.Type.Equals(orphanTypeName)).Take(step).ToList();
                var recordCounter = 0;
                records.ForEach(x => {
                    try
                    {
                        //update json string
                        var valueobj = JsonConvert.DeserializeObject(x.Value, ConcreteType(newType)) as BaseModel;
                        //set database revision type to new type
                        x.Type = newTypeName;
                        //update typechain
                        x.TypeChain = newTypeChain;
                        valueobj.Type = x.Type;
                        valueobj.TypeChain = x.TypeChain;
                        x.Value = JsonConvert.SerializeObject(valueobj);
                        //update indexed values, check this hasn't been indexed before
                        if (!indexChecked.Contains(string.Concat(x.Id.ToString(), x.Variant)))
                        {
                            var results = puck.core.Helpers.QueryHelper<BaseModel>.Query(
                                string.Concat("+", FieldKeys.ID,":", x.Id.ToString(), " +", FieldKeys.Variant, ":", x.Variant)
                                );
                            var result = results.FirstOrDefault();
                            if (result != null)
                            {
                                var indexNode = JsonConvert.DeserializeObject(result[FieldKeys.PuckValue], ConcreteType(newType)) as BaseModel;
                                //basically grab currently indexed node, change type information and add to reindex list
                                indexNode.TypeChain = x.TypeChain;
                                indexNode.Type = x.Type;
                                toIndex.Add(indexNode);
                                indexChecked.Add(string.Concat(x.Id.ToString(), x.Variant));
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        var exc = new Exception(ex.Message + string.Format(" -- errored on record {0} with id {1} and variant {2}", recordCounter, records[recordCounter].Id, records[recordCounter].Variant), ex);
                        logger.Log(exc);
                    }
                    finally {
                        recordCounter++;
                    }
                });
                //commit current chunk to db
                repo.SaveChanges();
                //since committing index is slow, only commit once reindex list grows to certain size to avoid frequent expensive operations on index
                if (toIndex.Count >= step)
                {
                    indexer.Index(toIndex);
                    toIndex.Clear();
                }
            }

            //update relevant meta entries
            var metaTypeAllowedTypes = repo.GetPuckMeta().Where(x => x.Name == DBNames.TypeAllowedTypes && (x.Key.Equals(orphanTypeName) || x.Value.Equals(orphanTypeName))).ToList();
            metaTypeAllowedTypes.ForEach(x => {
                if (x.Key.Equals(orphanTypeName))
                    x.Key = newTypeName;
                if (x.Value.Equals(orphanTypeName))
                    x.Value = newTypeName;
            });
            
            var metaEditorSettings = repo.GetPuckMeta().Where(x => x.Name == DBNames.EditorSettings && x.Key.Equals(orphanTypeName)).ToList();
            metaEditorSettings.ForEach(x =>
            {
                x.Key = newTypeName;                
            });

            var metaTypeAllowedTemplates = repo.GetPuckMeta().Where(x => x.Name == DBNames.TypeAllowedTemplates && x.Key.Equals(orphanTypeName)).ToList();
            metaTypeAllowedTemplates.ForEach(x =>
            {
                x.Key = newTypeName;
            });

            var metaFieldGroups = repo.GetPuckMeta().Where(x => x.Name.StartsWith(DBNames.FieldGroups+orphanTypeName)).ToList();
            metaFieldGroups.ForEach(x =>
            {
                x.Name = DBNames.FieldGroups+newTypeName;
            });

            repo.SaveChanges();

            //if there's anything left to reindex, reindex.
            if (toIndex.Count > 0)
                indexer.Index(toIndex);
            
        }
    }
}
