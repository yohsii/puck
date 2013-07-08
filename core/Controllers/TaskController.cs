using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using puck.core.Base;
using puck.core.Helpers;
using puck.core.Controllers;
using puck.core.Abstract;
using puck.core.Constants;
using Newtonsoft.Json;
using puck.core.Entities;
using puck.core.Filters;

namespace puck.core.Controllers
{
    [SetPuckCulture]
    [Auth]
    public class TaskController : BaseController
    {
        I_Puck_Repository repo;
        I_Log log;
        public TaskController(I_Puck_Repository repo,I_Log log) {
            this.repo = repo;
            this.log = log;
        }
        //
        // GET: /admin/Task/

        public ActionResult Index()
        {
            List<BaseTask> tasks= ApiHelper.Tasks();

            return View(tasks);
        }

        //
        // GET: /admin/Task/Details/5

        public ActionResult Details(int id)
        {
            return View();
        }

        //
        // GET: /admin/Task/Create
        public JsonResult TaskTypes() {
            var ttypes = ApiHelper.TaskTypes();
            return Json(ttypes.Select(x => new {Name=x.FullName,Key=x.AssemblyQualifiedName}), JsonRequestBehavior.AllowGet);
        }
        public ActionResult CreateTaskDialog() {
            return View();
        }
        public ActionResult Edit(string type,int id = -1)
        {
            if (id != -1) {
                var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.Tasks && x.ID == id).FirstOrDefault();
                if (meta != null) { 
                    Type t = Type.GetType(meta.Key);
                    var editModel = JsonConvert.DeserializeObject(meta.Value, t);
                    return View(editModel);
                }
            }
            Type modelType = Type.GetType(type);
            var model = Activator.CreateInstance(modelType);
            ((BaseTask)model).ID = -1;
            return View(model);
        }

        //
        // POST: /admin/Task/Edit/5

        [HttpPost]
        public ActionResult Edit(string p_type,FormCollection fc)
        {
            var targetType = Type.GetType(p_type);
            var model = Activator.CreateInstance(targetType);
            bool success = false;
            string message = "";
            try
            {
                UpdateModelDynamic(model, fc.ToValueProvider());
                var mod = model as BaseTask;
                PuckMeta taskMeta=null;
                if (mod.ID != -1){
                    taskMeta = repo.GetPuckMeta().Where(x => x.ID == mod.ID).FirstOrDefault();
                    taskMeta.Value = JsonConvert.SerializeObject(mod);
                }else{
                    taskMeta = new PuckMeta();
                    taskMeta.Name = DBNames.Tasks;
                    taskMeta.Key = mod.GetType().AssemblyQualifiedName;
                    taskMeta.Value = taskMeta.Value = JsonConvert.SerializeObject(mod);
                    repo.AddMeta(taskMeta);
                }
                repo.SaveChanges();
                ApiHelper.UpdateTaskMappings();
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
                log.Log(ex);
            }
            return Json(new {success=success,message=message });
        }

        //
        // GET: /admin/Task/Delete/5

        public JsonResult Delete(int id)
        {
            bool success = false;
            string message = "";
            try
            {
                repo.GetPuckMeta().Where(x => x.Name == DBNames.Tasks && x.ID == id).ToList().ForEach(x=>repo.DeleteMeta(x));
                repo.SaveChanges();
                ApiHelper.UpdateTaskMappings();
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
                log.Log(ex);
            }
            return Json(new { success = success, message = message },JsonRequestBehavior.AllowGet);
        }

    }
}
