using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using puck.core.Helpers;
using System.Reflection;
using puck.areas.admin.ViewModels;
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
namespace puck.core.Controllers
{
    [SetPuckCulture]
    public class ApiController : BaseController
    {
        I_Content_Indexer indexer;
        I_Content_Searcher searcher;
        I_Log log;
        I_Puck_Repository repo;
        public ApiController(I_Content_Indexer i, I_Content_Searcher s, I_Log l, I_Puck_Repository r) {
            this.indexer = i;
            this.searcher = s;
            this.log = l;
            this.repo = r;
        }
        public ActionResult Index() {
            return View();
        }
        public JsonResult FieldGroups(string type) {
            var model = ApiHelper.FieldGroups(type);
            return Json(model,JsonRequestBehavior.AllowGet);
        }
        public ActionResult CreateDialog(string type) {
            return View();
        }
        public JsonResult Models(string type)
        {
            if(string.IsNullOrEmpty(type))
                return Json(ApiHelper.Models().Select(x=>x.AssemblyQualifiedName),JsonRequestBehavior.AllowGet);
            else
                return Json(ApiHelper.AllowedTypes(type).Select(x => x.AssemblyQualifiedName), JsonRequestBehavior.AllowGet);
        }
        public JsonResult Variants() {
            var model = ApiHelper.Variants();
            return Json(model,JsonRequestBehavior.AllowGet);
        }
        public ActionResult DomainMappingDialog(string path)
        {
            var model = ApiHelper.DomainMapping(path);
            return View((object)model);
        }
        public JsonResult DomainMapping(string path)
        {
            var model = ApiHelper.DomainMapping(path);
            return Json(model,JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult DomainMapping(string path,string domains) {
            string message = "";
            bool success = false;
            try
            {
                ApiHelper.SetDomain(path,domains);
                success = true;
            }
            catch (Exception ex)
            {
                log.Log(ex);
                message = ex.Message;
            }
            return Json(new { message = message, success = success }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult LocalisationDialog(string path)
        {
            var model = ApiHelper.PathLocalisation(path);
            return View((object)model);
        }
        public JsonResult Localisation(string path) {
            var model = ApiHelper.PathLocalisation(path);
            return Json(model,JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult Localisation(string path,string variant)
        {
            string message = "";
            bool success = false;
            try
            {
                ApiHelper.SetLocalisation(path,variant);
                success = true;
            }
            catch (Exception ex) {
                log.Log(ex);
                message = ex.Message;
            }
            return Json(new { message=message,success=success}, JsonRequestBehavior.AllowGet);
        }
        public JsonResult Content(string path = "/") {
            var results = repo.CurrentRevisionsByPath(path)
                .ToList()
                .Select(x =>ApiHelper.RevisionToBaseModel(x)).ToList().GroupByPath().OrderBy(x=>x.Value.First().Value.SortOrder).ToDictionary(x=>x.Key,x=>x.Value);
            List<string> haveChildren = new List<string>();
            foreach (var k in results) {
                if (repo.CurrentRevisionChildren(k.Key).Count() > 0)
                    haveChildren.Add(k.Key);                
            }
            var qh = new QueryHelper<BaseModel>();
            var publishedContent = qh.Directory(path).GetAll().GroupByPath();
            return Json(new { current=results,published=publishedContent,children=haveChildren }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult Sort(string path,List<string> items) {
            string message = "";
            bool success = false;
            try{
                ApiHelper.Sort(path,items);
                success = true;
            }
            catch (Exception ex) {
                log.Log(ex);
                success = false;
                message = ex.Message;
            }
            return Json(new {success=success,message=message },JsonRequestBehavior.AllowGet);
        }
        public JsonResult Publish(string id,bool descendants=false)
        {
            var message = string.Empty;
            var success = false;
            try
            {
                ApiHelper.Publish(id,descendants);
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
        public JsonResult UnPublish(string id)
        {
            var message = string.Empty;
            var success = false;
            try
            {
                ApiHelper.UnPublish(id);
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
        public JsonResult Delete(string id,string variant = null){
            var message = string.Empty;
            var success = false;
            try {
                ApiHelper.Delete(id,variant);
                success = true;
            }
            catch (Exception ex) {
                log.Log(ex);
                success = false;
                message = ex.Message;
            }
            return Json(new { success = success, message = message }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Edit(string type,string path="/",string variant="") {
            //empty model of type
            var modelType= Type.GetType(type);
            object model = Activator.CreateInstance(modelType);
            //if creating new, return early
            if (path.EndsWith("/"))
            {
                var basemodel = (BaseModel)model;
                basemodel.Path = path;
                basemodel.Variant = variant;
                basemodel.TypeChain =ApiHelper.TypeChain(modelType);
                basemodel.Type = modelType.AssemblyQualifiedName;
                return View(model);
            }
            //else we'll need to get current data to edit for node
           
            //get current culture
            if(string.IsNullOrEmpty(variant))
                variant = Thread.CurrentThread.CurrentCulture.Name;

            List<PuckRevision> results = null;
            //try get node by path with particular variant
            results = repo.GetPuckRevision().Where(x => x.Path.ToLower().Equals(path) && x.Variant.ToLower().Equals(variant) && x.Current).ToList();
            
            //just get node by path
            if (results.Count == 0)
                results = repo.GetPuckRevision().Where(x => x.Path.ToLower().Equals(path) && x.Current).ToList();
            
            if (results.Count > 0) {
                var result = results.FirstOrDefault();
                model = ApiHelper.RevisionToModel(result);
            }
            return View(model);
        }

        [HttpPost]
        public JsonResult Edit(FormCollection fc,string p_type,string p_path) {
            var targetType = Type.GetType(p_type);
            var model = Activator.CreateInstance(targetType);
            bool success = false;
            string message = "";
            try { 
                UpdateModelDynamic(model,fc.ToValueProvider());
                ObjectDumper.BindImages(model, int.MaxValue);
                ObjectDumper.Transform(model, int.MaxValue);
                var npi = ObjectDumper.Write(model, int.MaxValue);
                var mod = model as BaseModel;
                //append nodename to path, which indicates first save
                mod.Path =mod.Path.EndsWith("/")? p_path+mod.NodeName:mod.Path;
                //ApiHelper.SaveContent(mod);
                success = true;
            }
            catch (Exception ex) {
                success = false;
                message = ex.Message;
                log.Log(ex);
            }
            return Json(new {success=success,message=message },JsonRequestBehavior.AllowGet);
        }
        public JsonResult CacheInfo(string path) {
            bool success = false;
            string message = "";
            var model = false;
            try
            {
                var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.CacheExclude && x.Key.ToLower().Equals(path.ToLower())).FirstOrDefault();
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
        [HttpPost]
        public JsonResult CacheInfo(string path,bool value)
        {
            bool success = false;
            string message = "";
            try
            {
                var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.CacheExclude && x.Key.ToLower().Equals(path.ToLower())).FirstOrDefault();
                if (meta != null)
                    meta.Value = value.ToString();
                else {
                    meta = new PuckMeta() { Name=DBNames.CacheExclude,Key=path,Value=value.ToString()};
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
        public ActionResult Revisions(Guid id,string variant) {
            var model = repo.GetPuckRevision().Where(x => x.Id == id && variant.ToLower().Equals(variant.ToLower())).OrderByDescending(x=>x.Revision).ToList();
            return View(model);
        }

        public ActionResult Compare(int id)
        {
            var compareTo = repo.GetPuckRevision().Where(x => x.RevisionID == id).FirstOrDefault();
            var current = repo.GetPuckRevision().Where(x => x.Id == compareTo.Id && x.Variant.ToLower().Equals(compareTo.Variant.ToLower()) && x.Current).FirstOrDefault();
            var model = new RevisionCompare{Current=null,Revision=null,RevisionID=-1};
            if (compareTo != null && current != null)
            {
                var mCompareTo = JsonConvert.DeserializeObject(compareTo.Value, Type.GetType(compareTo.Type)) as BaseModel;
                var mCurrent = JsonConvert.DeserializeObject(current.Value, Type.GetType(current.Type)) as BaseModel;
                model = new RevisionCompare { Current = mCurrent, Revision = mCompareTo,RevisionID=compareTo.RevisionID};
            }
            return View(model);
        }

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
                repo.SaveChanges();
                if (current.Any()) {
                    //don't want to revert change node/path because it has consequences for children/descendants
                    rnode.NodeName = current.FirstOrDefault().NodeName;
                    rnode.Path = current.FirstOrDefault().Path;
                }
                if (current.Any(x => x.Published))
                {
                    var model = JsonConvert.DeserializeObject(rnode.Value, Type.GetType(rnode.Type)) as BaseModel;
                    indexer.Index(new List<BaseModel>(){model});
                }
                path = rnode.Path;
                type = rnode.Type;
                variant = rnode.Variant;
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
