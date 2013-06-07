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
namespace puck.core.Controllers
{
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
        public ActionResult CreateDialog() {
            return View();
        }
        public JsonResult Models(string type)
        {
            return Json(ApiHelper.Models.Select(x=>x.AssemblyQualifiedName),JsonRequestBehavior.AllowGet);
        }
        public JsonResult Variants() {
            var model = ApiHelper.Variants();
            return Json(model,JsonRequestBehavior.AllowGet);
        }
        public JsonResult Content(string path = "/") {
            var qh = new QueryHelper<BaseModel>();
            var results = qh.Directory(path).GetAll().GroupByPath();
            return Json(results,JsonRequestBehavior.AllowGet);
        }
        public JsonResult Publish(string id)
        {
            var message = string.Empty;
            var success = false;
            try
            {
                var qh = new QueryHelper<BaseModel>();
                var toIndex = qh.ID(id).GetAll();
                if (toIndex.Count == 0)
                    throw new Exception("no results with ID " + id + " to publish");
                toIndex.AddRange(toIndex.First().Descendants<BaseModel>());
                toIndex.ForEach(x=>x.Published=true);
                indexer.Index(toIndex);
                success = true;
            }
            catch (Exception ex)
            {
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
                var qh = new QueryHelper<BaseModel>();
                var toIndex = qh.ID(id).GetAll();
                if (toIndex.Count == 0)
                    throw new Exception("no results with ID " + id + " to unpublish");
                toIndex.AddRange(toIndex.First().Descendants<BaseModel>());
                toIndex.ForEach(x => x.Published = false);
                indexer.Index(toIndex);
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }
            return Json(new { success = success, message = message }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult Delete(string id){
            var message = string.Empty;
            var success = false;
            try {
                var qh = new QueryHelper<BaseModel>();
                var toDelete=qh.And().ID(id).GetAll();
                if (toDelete.Count == 0)
                    throw new Exception("no results with ID "+id+" to delete");
                toDelete.AddRange(toDelete.First().Descendants<BaseModel>());
                toDelete.Delete();
                success = true;
            }
            catch (Exception ex) {
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
                basemodel.Created = DateTime.Now;
                basemodel.Updated = DateTime.Now;
                basemodel.Id = Guid.NewGuid();
                basemodel.Revision = 0;
                basemodel.Path = path;
                basemodel.Variant = variant;
                basemodel.SortOrder = -1;
                basemodel.TypeChain =ApiHelper.TypeChain(modelType);
                basemodel.Type = modelType.AssemblyQualifiedName;
                return View(model);
            }
            //else we'll need to get current data to edit for node
           
            //get current culture
            if(string.IsNullOrEmpty(variant))
                variant = Thread.CurrentThread.CurrentCulture.Name;

            List<Dictionary<string, string>> results=null;
            //try get node by path with particular variant
            results = searcher.Query(string.Concat(
                "+",FieldKeys.Path, ":", path," +",FieldKeys.Variant,":",variant
            ),type).ToList();
            //just get node by path
            if (results.Count == 0)
                results = searcher.Query(string.Concat(
                     "+", FieldKeys.Path, ":", path
                ),type).ToList();
            
            if (results.Count > 0) {
                var result = results.FirstOrDefault();
                model = JsonConvert.DeserializeObject(result[FieldKeys.PuckValue],Type.GetType(result[FieldKeys.PuckType]));
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
                //check name doesn't already exist and set sort number
                var mod = model as BaseModel;
                //get sibling nodes
                mod.Path = p_path+mod.NodeName;
                var nodesAtPath = mod.Siblings<BaseModel>().GroupByID();
                //set sort order for new content
                if(mod.SortOrder==-1)
                    mod.SortOrder = nodesAtPath.Count;
                //check node name is unique at path
                if (nodesAtPath.Any(x => x.Value.Any(y => y.Value.NodeName.ToLower().Equals(mod.NodeName))))
                    throw new Exception("Nodename exists at this path, choose another.");
                //check this is an update or create
                var qh = new QueryHelper<BaseModel>();
                qh.Field(x=>x.Id,mod.Id.ToString())
                    .And()
                    .Field(x=>x.Variant,mod.Variant);
                var original = qh.Get();
                
                var toIndex = new List<BaseModel>();
                toIndex.Add(mod);
                bool nameChanged = false;
                string originalPath = string.Empty;                
                if (original != null)
                {//this must be an edit
                    if (!original.NodeName.ToLower().Equals(mod.NodeName.ToLower())) {
                        nameChanged = true;
                        originalPath =p_path+original.Path;
                    }
                }
                var variants = mod.Variants<BaseModel>();
                if (variants.Any(x => !x.NodeName.ToLower().Equals(mod.NodeName.ToLower())))
                {//update path of variants
                    nameChanged = true;
                    if(string.IsNullOrEmpty(originalPath))
                        originalPath =p_path+variants.First().NodeName;
                    variants.ForEach(x => { x.NodeName = mod.NodeName; toIndex.Add(x); });
                }
                if (nameChanged) { 
                    //update path of decendants
                    var descendants = mod.Descendants<BaseModel>();
                    var regex = new Regex(originalPath,RegexOptions.Compiled);
                    descendants.ForEach(x =>
                    {
                        x.Path = regex.Replace(x.Path, mod.Path,1);
                        toIndex.Add(x);
                    });
                }
                indexer.Index(toIndex);
                success = true;
            }
            catch (Exception ex) {
                success = false;
                message = ex.Message;
                log.Log(ex);
            }
            return Json(new {success=success,message=message },JsonRequestBehavior.AllowGet);
        }

        
    }
}
