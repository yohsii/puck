using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using puck.core.Helpers;
using System.Reflection;
using System.IO;
using Newtonsoft.Json;
using puck.core.Abstract;
using puck.core.Constants;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Snowball;
using System.Threading;
using puck.core.Base;
using System.Text.RegularExpressions;
using puck.core.Entities;
using puck.core.Filters;
using puck.core.Models;
using StackExchange.Profiling;
using System.Web.Security;
using puck.core.Identity;
using Microsoft.AspNet.Identity;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace puck.core.Controllers
{
    [SetPuckCulture]
    public class ApiController : BaseController
    {
        private static readonly object _savelck = new object();
        I_Content_Indexer indexer;
        I_Content_Searcher searcher;
        I_Log log;
        I_Puck_Repository repo;
        PuckRoleManager roleManager;
        PuckUserManager userManager;
        PuckSignInManager signInManager;
        public ApiController(I_Content_Indexer i, I_Content_Searcher s, I_Log l, I_Puck_Repository r, PuckRoleManager rm, PuckUserManager um, PuckSignInManager sm) {
            this.indexer = i;
            this.searcher = s;
            this.log = l;
            this.repo = r;
            this.roleManager = rm;
            this.userManager = um;
            this.signInManager = sm;
        }
        [Auth(Roles=PuckRoles.Puck)]
        public ActionResult Index()
        {
            return View();
        }
        [Auth(Roles = PuckRoles.Puck)]
        public JsonResult UserLanguage()
        {
            string variant = PuckCache.SystemVariant;
            var user = userManager.FindByName(User.Identity.Name);
            if(!string.IsNullOrEmpty(user.UserVariant))
            //var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.UserVariant && x.Key == User.Identity.Name).FirstOrDefault();
            //if (meta != null)
                variant = user.UserVariant;
            return Json(variant, JsonRequestBehavior.AllowGet);
        }
        [Auth(Roles = PuckRoles.Puck)]
        public async Task<JsonResult> UserRoles()
        {
            var roles = await userManager.GetRolesAsync(User.Identity.GetUserId());
            return Json(roles, JsonRequestBehavior.AllowGet);
        }
        [Auth(Roles =PuckRoles.Puck)]
        public JsonResult FieldGroups(string type)
        {
            var model = ApiHelper.FieldGroups(type);
            return Json(model,JsonRequestBehavior.AllowGet);
        }
        [Auth(Roles = PuckRoles.Puck)]
        public ActionResult CreateDialog(string type)
        {
            return View();
        }
        [Auth(Roles = PuckRoles.Puck)]
        public JsonResult Variants()
        {
            var model = ApiHelper.Variants();
            return Json(model,JsonRequestBehavior.AllowGet);
        }
        [Auth(Roles = PuckRoles.Puck)]
        public JsonResult AllVariants()
        {
            var model = ApiHelper.AllVariants();
            return Json(model, JsonRequestBehavior.AllowGet);
        }
        [Auth(Roles = PuckRoles.Puck)]
        public ActionResult Preview(string path, string variant)
        {
            var model = repo.GetPuckRevision().Where(x => x.Current && x.Path.ToLower().Equals(path.ToLower()) && x.Variant.ToLower().Equals(variant.ToLower())).FirstOrDefault();
            return Preview(model);
        }
        [Auth(Roles = PuckRoles.Puck)]
        public ActionResult PreviewGuid(Guid id, string variant)
        {
            var model = repo.GetPuckRevision().Where(x => x.Current && x.Id == id && x.Variant.ToLower().Equals(variant.ToLower())).FirstOrDefault();
            return Preview(model);
        }
        [Auth(Roles = PuckRoles.Puck)]
        private ActionResult Preview(PuckRevision model)
        {
            var dmode = this.GetDisplayModeId();
            string templatePath = model.TemplatePath;
            if (!string.IsNullOrEmpty(dmode))
            {
                string dpath = templatePath.Insert(templatePath.LastIndexOf('.') + 1, dmode + ".");
                if (System.IO.File.Exists(Server.MapPath(dpath)))
                {
                    templatePath = dpath;
                }
            }
            var mod = ApiHelper.RevisionToBaseModel(model);
            return View(templatePath, mod);
        }
        [Auth(Roles= PuckRoles.Notify)]
        public JsonResult Notify(string p_path){
            var model = ApiHelper.NotifyModel(p_path);
            return Json(model,JsonRequestBehavior.AllowGet);
        }
        [Auth(Roles=PuckRoles.Notify)]
        public ActionResult NotifyDialog(string p_path){
            var model = ApiHelper.NotifyModel(p_path);
            return View(model);
        }
        [Auth(Roles=PuckRoles.Notify)]
        [HttpPost]
        public JsonResult Notify(Notify model){
            string message="";
            bool success=false;
            try{
                ApiHelper.SetNotify(model);
                success=true;
            }catch(Exception ex){
                message = ex.Message;
            }
            return Json(new{success=success,message=message});
        }
        [Auth(Roles = PuckRoles.Domain)]
        public ActionResult DomainMappingDialog(string p_path)
        {
            var model = ApiHelper.DomainMapping(p_path);
            return View((object)model);
        }
        [Auth(Roles = PuckRoles.Domain)]
        public JsonResult DomainMapping(string p_path)
        {
            var model = ApiHelper.DomainMapping(p_path);
            return Json(model,JsonRequestBehavior.AllowGet);
        }
        [Auth(Roles = PuckRoles.Domain)]
        [HttpPost]
        public JsonResult DomainMapping(string p_path,string domains) {
            string message = "";
            bool success = false;
            try
            {
                ApiHelper.SetDomain(p_path,domains);
                success = true;
            }
            catch (Exception ex)
            {
                log.Log(ex);
                message = ex.Message;
            }
            return Json(new { message = message, success = success }, JsonRequestBehavior.AllowGet);
        }

        [Auth(Roles = PuckRoles.Move)]
        public JsonResult Move(string start,string destination)
        {
            string message = "";
            bool success = false;
            try
            {
                ApiHelper.Move(start,destination);
                success = true;
            }
            catch (Exception ex)
            {
                log.Log(ex);
                message = ex.Message;
            }
            return Json(new { message = message, success = success }, JsonRequestBehavior.AllowGet);            
        }

        [Auth(Roles = PuckRoles.Localisation)]
        public ActionResult LocalisationDialog(string p_path)
        {
            var model = ApiHelper.PathLocalisation(p_path);
            return View((object)model);
        }
        [Auth(Roles = PuckRoles.Localisation)]
        public JsonResult Localisation(string p_path) {
            var model = ApiHelper.PathLocalisation(p_path);
            return Json(model,JsonRequestBehavior.AllowGet);
        }
        [Auth(Roles = PuckRoles.Localisation)]
        [HttpPost]
        public JsonResult Localisation(string p_path,string variant)
        {
            string message = "";
            bool success = false;
            try
            {
                ApiHelper.SetLocalisation(p_path,variant);
                success = true;
            }
            catch (Exception ex) {
                log.Log(ex);
                message = ex.Message;
            }
            return Json(new { message=message,success=success}, JsonRequestBehavior.AllowGet);
        }

        [Auth(Roles = PuckRoles.Puck)]
        public JsonResult GetPath(Guid id)
        {
            var node = repo.GetPuckRevision().Where(x => x.Id == id&&x.Current).FirstOrDefault();
            string path = node == null ? string.Empty : node.Path;
            return Json(path,JsonRequestBehavior.AllowGet);
        }

        [Auth(Roles = PuckRoles.Puck)]
        public JsonResult StartPath()
        {
            var user = userManager.FindByName(User.Identity.Name);
            if (user.StartNodeId != Guid.Empty) {
                var node = repo.GetPuckRevision().Where(x => x.Id == user.StartNodeId && x.Current).FirstOrDefault();
                if (node != null)
                {
                    return Json(node.Path + "/", JsonRequestBehavior.AllowGet);
                }
            }
            /*var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.UserStartNode && x.Key == HttpContext.User.Identity.Name).FirstOrDefault();
            if (meta != null)
            {
                var pu = JsonConvert.DeserializeObject(meta.Value, typeof(PuckPicker)) as PuckPicker;
                if (pu != null)
                {
                    var node = repo.GetPuckRevision().Where(x => x.Id == pu.Id&&x.Current).FirstOrDefault();
                    if (node != null)
                    {
                        return Json(node.Path+"/",JsonRequestBehavior.AllowGet);
                    }
                }
            }*/
            return Json("/", JsonRequestBehavior.AllowGet);
        }
        [Auth(Roles = PuckRoles.Puck)]
        public ActionResult SearchTypes(string root)
        {
            var typeGroups = repo.GetPuckRevision().Where(x=>x.Path.StartsWith(root+"/") && x.Current).GroupBy(x=>x.Type);
            var typeStrings = typeGroups.Select(x=>x.Key);
            var result = new List<dynamic>();
            foreach (var typeString in typeStrings) {
                var type = ApiHelper.GetType(typeString);
                if (type != null) {
                    result.Add(new {Name=ApiHelper.FriendlyClassName(type),Type=typeString});
                }
            }
            return Json(result,JsonRequestBehavior.AllowGet);
        }
        [Auth(Roles = PuckRoles.Puck)]
        public ActionResult Search(string q,string type,string root)
        {
            var results = new List<Dictionary<string, string>>();

            var typeGroups = repo.GetPuckRevision().Where(x => x.Path.StartsWith(root + "/") && x.Current).GroupBy(x => x.Type);
            var typeStrings = typeGroups.Select(x => x.Key);
            
            if (string.IsNullOrEmpty(type))
            {
                foreach (var typeString in typeStrings) {
                    var tqs = "(";
                    foreach (var t in PuckCache.TypeFields[typeString])
                    {
                        if (tqs.IndexOf(" " + t.Key + ":") > -1)
                            continue;
                            tqs += string.Concat(t.Key, ":", q, " ");
                    }
                    tqs = tqs.Trim();
                    tqs += ")";
                    tqs += string.Concat(" AND ", FieldKeys.PuckType, ":", "\"",typeString,"\"");
                    if (!string.IsNullOrEmpty(root))
                    {
                        tqs = string.Concat(tqs, " AND ", FieldKeys.Path, ":", root, "/*");
                    }
                    results.AddRange(PuckCache.PuckSearcher.Query(tqs,typeString));
                }
                results = results.OrderByDescending(x => float.Parse(x[FieldKeys.Score])).ToList();
            }
            else {
                var tqs = "(";
                foreach (var f in PuckCache.TypeFields[type])
                {
                    tqs += string.Concat(f.Key, ":", q, " ");
                }
                tqs=tqs.Trim();
                tqs+=")";
                tqs +=string.Concat(" AND ",FieldKeys.PuckType,":","\"",type,"\"");
                if (!string.IsNullOrEmpty(root))
                {
                    tqs = string.Concat(tqs, " AND ", FieldKeys.Path, ":", root, "/*");
                }
                results.AddRange(PuckCache.PuckSearcher.Query(tqs,type));
            }
            
            var model = new List<BaseModel>();
            foreach (var res in results) {
                var mod = JsonConvert.DeserializeObject(res[FieldKeys.PuckValue],ApiHelper.ConcreteType(ApiHelper.GetType(res[FieldKeys.PuckType]))) as BaseModel;
                model.Add(mod);
            }
            return View(model);
        }
        [Auth(Roles = PuckRoles.Puck)]
        public JsonResult VariantsForNode(string path){
            var nodes = repo.CurrentRevisionsByPath(path);
            var result = nodes.Select(x => new { Variant=x.Variant,Published=x.Published});
            return Json(result,JsonRequestBehavior.AllowGet);
        }
        [Auth(Roles=PuckRoles.Puck)]
        public JsonResult Content(string path = "/") {
            //using path instead of p_path in the method sig means path won't be checked against user's start node - which we don't want for this method
            string p_path = path;
            List<PuckRevision> resultsRev;
#if DEBUG
            using (MiniProfiler.Current.Step("content by path from DB"))
            {
                resultsRev = repo.CurrentRevisionsByDirectory(p_path).ToList();
            }
#else
            resultsRev = repo.CurrentRevisionsByDirectory(p_path).ToList();
#endif
            var results = resultsRev.Select(x =>ApiHelper.RevisionToBaseModelCast(x)).ToList()
                .GroupByPath()
                .OrderBy(x=>x.Value.First().Value.SortOrder)
                .ToDictionary(x=>x.Key,x=>x.Value);

            List<string> haveChildren = new List<string>();
            foreach (var k in results) {
                if (repo.CurrentRevisionChildren(k.Key).Count() > 0)
                    haveChildren.Add(k.Key);                
            }
            var qh = new QueryHelper<BaseModel>();
            var publishedContent = qh.Directory(p_path).GetAll().GroupByPath();
            return Json(new { current=results,published=publishedContent,children=haveChildren }, JsonRequestBehavior.AllowGet);
        }

        [Auth(Roles = PuckRoles.Sort)]
        public JsonResult Sort(string p_path,List<string> items) {
            string message = "";
            bool success = false;
            try{
                ApiHelper.Sort(p_path,items);
                success = true;
            }
            catch (Exception ex) {
                log.Log(ex);
                success = false;
                message = ex.Message;
            }
            return Json(new {success=success,message=message },JsonRequestBehavior.AllowGet);
        }

        [Auth(Roles = PuckRoles.Publish)]
        public JsonResult Publish(Guid id,string variant,string descendants)
        {
            lock (_savelck)
            {
                var message = string.Empty;
                var success = false;
                try
                {
                    var arrDescendants = descendants.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    ApiHelper.Publish(id, variant, arrDescendants, true);
                    success = true;
                }
                catch (Exception ex)
                {
                    log.Log(ex);
                    success = false;
                    message = ex.Message;
                }
                return Json(new { success = success, message = message }, JsonRequestBehavior.AllowGet);
            }
        }

        [Auth(Roles = PuckRoles.Unpublish)]
        public JsonResult UnPublish(Guid id,string variant,string descendants)
        {
            lock (_savelck)
            {
                var message = string.Empty;
                var success = false;
                try
                {
                    var arrDescendants = descendants.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    ApiHelper.Publish(id, variant, arrDescendants, false);
                    success = true;
                }
                catch (Exception ex)
                {
                    log.Log(ex);
                    success = false;
                    message = ex.Message;
                }
                return Json(new { success = success, message = message }, JsonRequestBehavior.AllowGet);
            }
        }

        [Auth(Roles = PuckRoles.Delete)]
        public JsonResult Delete(Guid id,string variant = null){
            lock (_savelck)
            {
                var message = string.Empty;
                var success = false;
                try
                {
                    ApiHelper.Delete(id, variant);
                    success = true;
                }
                catch (Exception ex)
                {
                    log.Log(ex);
                    success = false;
                    message = ex.Message;
                }
                return Json(new { success = success, message = message }, JsonRequestBehavior.AllowGet);
            }
        }
        [Auth(Roles = PuckRoles.Puck)]
        public JsonResult Models(string type)
        {
            if (string.IsNullOrEmpty(type))
                return Json(ApiHelper.AllModels().Select(x =>
                    new { Name = ApiHelper.FriendlyClassName(x), AssemblyName = x.AssemblyQualifiedName }
                    ), JsonRequestBehavior.AllowGet);
            else
                return Json(ApiHelper.AllowedTypes(type).Select(x =>
                    new { Name = ApiHelper.FriendlyClassName(x), AssemblyName = x.AssemblyQualifiedName }
                    ), JsonRequestBehavior.AllowGet);
        }
        public ActionResult ModelOptions(string type) {
            var models = ApiHelper.AllModels();
            
            var modelMatches = models.Where(x => x.FullName.EndsWith(type)).ToList();
            var result = (modelMatches == null ? models : new List<Type> (modelMatches))
                .Select(x => new {Name=ApiHelper.FriendlyClassName(x),AssemblyName=x.AssemblyQualifiedName})
                .ToList();
            return Json(result,JsonRequestBehavior.AllowGet);
        }
        public ActionResult InspectModel(string type,string opath="") {
            var isGenerated = false;
            var tType = ApiHelper.GetType(type);
            var originalType = tType;
            if (typeof(I_Generated).IsAssignableFrom(tType))
            {
                isGenerated = true;
                tType = ApiHelper.ConcreteType(tType);
            }
            var props = tType.GetProperties();
            var parts = opath.Split(new char[]{'.'},StringSplitOptions.RemoveEmptyEntries);
            var str = opath;
            
            var currentT = tType;

            var result = new List<dynamic>();

            parts.ToList().ForEach(x=>{
                var t = currentT.GetProperties().Where(xx => xx.Name.Equals(x)).FirstOrDefault().PropertyType;
                currentT = t;    
            });

            currentT.GetProperties().ToList().ForEach(x =>
            {
                var isArray = x.PropertyType.GetInterface(typeof(IEnumerable<>).FullName) != null && !(x.PropertyType == typeof(string));
                result.Add(
                new
                {
                    Name = x.Name,
                    IsArray = isArray,
                    IsComplexType = x.PropertyType.IsClass && !isArray && !(x.PropertyType == typeof(string)),
                    Type = x.PropertyType.Name,
                    InsertString = "@Model."+(string.IsNullOrEmpty(opath)?"":opath+".")+x.Name,
                    IterateString = string.Format("@foreach(var el in Model.{0}){{\n\n}}",
                        (string.IsNullOrEmpty(opath)?"":opath+".")+x.Name),
                    InspectString = (string.IsNullOrEmpty(opath)?"":opath+".")+x.Name                    
                });
            });

            return Json(new { Data=result,Path=opath,Type=type,Name=ApiHelper.FriendlyClassName(tType),FullName=originalType.FullName,IsGenerated=isGenerated }, JsonRequestBehavior.AllowGet);
        }
        
        [Auth(Roles = PuckRoles.Edit)]
        public ActionResult Edit(string p_type,string p_path="/",string p_variant="",string p_fromVariant="",bool create=false) {
            if (p_variant == "null")
                p_variant = PuckCache.SystemVariant;
            object model=null;
            if (!string.IsNullOrEmpty(p_type))
            {
                //empty model of type
                var modelType = ApiHelper.GetType(p_type);
                var concreteType = ApiHelper.ConcreteType(modelType);
                model = ApiHelper.CreateInstance(concreteType);
                //if creating new, return early
                if (p_path.EndsWith("/"))
                {
                    var basemodel = (BaseModel)model;
                    basemodel.Path = p_path;
                    basemodel.Variant = p_variant;
                    basemodel.TypeChain = ApiHelper.TypeChain(concreteType);
                    basemodel.Type = modelType.AssemblyQualifiedName;
                    basemodel.CreatedBy = User.Identity.Name;
                    basemodel.LastEditedBy = basemodel.CreatedBy;
                    return View(model);
                }
            }
            //else we'll need to get current data to edit for node or return node to translate
           
            List<PuckRevision> results = null;
            //try get node by path with particular variant
            if(string.IsNullOrEmpty(p_fromVariant))
                results = repo.GetPuckRevision().Where(x => x.Path.ToLower().Equals(p_path) && x.Variant.ToLower().Equals(p_variant.ToLower()) && x.Current).ToList();
            else
                results = repo.GetPuckRevision().Where(x => x.Path.ToLower().Equals(p_path) && x.Variant.ToLower().Equals(p_fromVariant.ToLower()) && x.Current).ToList();

            if (results.Count > 0) {
                var result = results.FirstOrDefault();
                model = ApiHelper.RevisionToModel(result);
                if(!string.IsNullOrEmpty(p_fromVariant)){
                    var mod = model as BaseModel;
                    mod.Variant = p_variant;
                    mod.Created = DateTime.Now;
                    mod.Updated = DateTime.Now;
                    mod.Published = false;
                    mod.Revision = 0;
                    mod.CreatedBy = User.Identity.Name;
                    mod.LastEditedBy = mod.CreatedBy;
                }
            }
            return View(model);
        }

        [Auth(Roles = PuckRoles.Puck)]
        public ActionResult GetRepublishEntireSiteStatus()
        {
            string message = "";
            if (PuckCache.IsRepublishingEntireSite)
                message = PuckCache.IndexingStatus;
            else
                message = "complete";
            return Json(new { Success = true, Message = message }, JsonRequestBehavior.AllowGet);
        }

        [Auth(Roles = PuckRoles.Republish)]
        [HttpPost]
        public ActionResult RepublishEntireSite() {
            var success = true;
            string message = "republish entire site started";
            if (!PuckCache.IsRepublishingEntireSite)
            {
                HostingEnvironment.QueueBackgroundWorkItem(ct => ApiHelper.RePublishEntireSite2());
                PuckCache.IsRepublishingEntireSite = true;
                PuckCache.IndexingStatus = "republish entire site task queued";
            }
            else
            {
                success = false;
                message = "already republishing entire site";
            }
            return Json(new {Success=success,Message=message }, JsonRequestBehavior.AllowGet);
        }

        [Auth(Roles = PuckRoles.Edit)]
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult Edit(FormCollection fc,string p_type,string p_path) {
            lock (_savelck)
            {
                var targetType = ApiHelper.ConcreteType(ApiHelper.GetType(p_type));
                var model = ApiHelper.CreateInstance(targetType);
                string path = "";
                bool success = false;
                string message = "";
                try
                {
                    UpdateModelDynamic(model, fc.ToValueProvider());
                    ObjectDumper.BindImages(model, int.MaxValue);
                    //ObjectDumper.Transform(model, int.MaxValue);
                    var mod = model as BaseModel;
                    ApiHelper.SaveContent(mod);
                    path = mod.Path;
                    success = true;
                }
                catch (Exception ex)
                {
                    success = false;
                    message = ex.Message;
                    log.Log(ex);
                }
                return Json(new { success = success, message = message, path = path }, JsonRequestBehavior.AllowGet);
            }
        }

        [Auth(Roles = PuckRoles.Cache)]
        public JsonResult CacheInfo(string p_path) {
            bool success = false;
            string message = "";
            var model = false;
            try
            {
                var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.CacheExclude && x.Key.ToLower().Equals(p_path.ToLower())).FirstOrDefault();
                if (meta == null || !bool.TryParse(meta.Value, out model))
                    model = false;
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
                log.Log(ex);
            }
            return Json(new {result=model, success = success, message = message }, JsonRequestBehavior.AllowGet);
        }

        [Auth(Roles = PuckRoles.Cache)]
        [HttpPost]
        public JsonResult CacheInfo(string p_path,bool value)
        {
            bool success = false;
            string message = "";
            try
            {
                var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.CacheExclude && x.Key.ToLower().Equals(p_path.ToLower())).FirstOrDefault();
                if (meta != null)
                    meta.Value = value.ToString();
                else {
                    meta = new PuckMeta() { Name=DBNames.CacheExclude,Key=p_path,Value=value.ToString()};
                    repo.AddMeta(meta);
                }
                repo.SaveChanges();
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
                log.Log(ex);
            }
            return Json(new { success = success, message = message }, JsonRequestBehavior.AllowGet);
        }

        [Auth(Roles = PuckRoles.Revert)]
        public ActionResult Revisions(Guid id,string variant) {
            var model = repo.GetPuckRevision().Where(x => x.Id == id && x.Variant.ToLower().Equals(variant.ToLower())).OrderByDescending(x=>x.Revision).ToList();
            return View(model);
        }
        [Auth(Roles = PuckRoles.Revert)]
        public ActionResult Compare(int id)
        {
            var compareTo = repo.GetPuckRevision().Where(x => x.RevisionID == id).FirstOrDefault();
            var current = repo.GetPuckRevision().Where(x => x.Id == compareTo.Id && x.Variant.ToLower().Equals(compareTo.Variant.ToLower()) && x.Current).FirstOrDefault();
            var model = new RevisionCompare{Current=null,Revision=null,RevisionID=-1};
            if (compareTo != null && current != null)
            {
                var mCompareTo = JsonConvert.DeserializeObject(compareTo.Value,ApiHelper.ConcreteType(ApiHelper.GetType(compareTo.Type))) as BaseModel;
                var mCurrent = JsonConvert.DeserializeObject(current.Value,ApiHelper.ConcreteType(ApiHelper.GetType(current.Type))) as BaseModel;
                model = new RevisionCompare { Current = mCurrent, Revision = mCompareTo,RevisionID=compareTo.RevisionID};
            }
            return View(model);
        }
        [Auth(Roles = PuckRoles.Revert)]
        public ActionResult Revert(int id)
        {
            bool success = false;
            string message = "";
            string path = "";
            string type = "";
            string variant = "";
            try
            {
                var rnode = repo.GetPuckRevision().Where(x=>x.RevisionID==id).FirstOrDefault();
                if (rnode == null)
                    throw new Exception(string.Format("revision does not exist: id:{0}", id));
                var current = repo.GetPuckRevision().Where(x => x.Id == rnode.Id && x.Variant.ToLower().Equals(rnode.Variant.ToLower()) && x.Current).ToList();
                current.ForEach(x => x.Current = false);
                rnode.Current = true;
                if (current.Any()) {
                    //don't want to revert change node/path because it has consequences for children/descendants
                    rnode.NodeName = current.FirstOrDefault().NodeName;
                    rnode.Path = current.FirstOrDefault().Path;
                }
                if (current.Any(x => x.Published))
                {
                    var model = JsonConvert.DeserializeObject(rnode.Value,ApiHelper.ConcreteType(ApiHelper.GetType(rnode.Type))) as BaseModel;
                    indexer.Index(new List<BaseModel>(){model});
                }
                path = rnode.Path;
                type = rnode.Type;
                variant = rnode.Variant;
                repo.SaveChanges();                
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
                log.Log(ex);
            }
            return Json(new { success = success, message = message,path=path,type=type,variant=variant }, JsonRequestBehavior.AllowGet);
        }
        [Auth(Roles = PuckRoles.Revert)]
        public JsonResult DeleteRevision(int id)
        {
            var message = string.Empty;
            var success = false;
            try
            {
                repo.GetPuckRevision().Where(x => x.RevisionID == id).ToList().ForEach(x=>repo.DeleteRevision(x));
                repo.SaveChanges();
                success = true;
            }
            catch (Exception ex)
            {
                log.Log(ex);
                success = false;
                message = ex.Message;
            }
            return Json(new { success = success, message = message }, JsonRequestBehavior.AllowGet);
        }

    }
}
