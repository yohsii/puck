﻿using System;
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
using puck.core.Models;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using puck.core.Attributes;
using puck.core.State;
namespace puck.core.Controllers
{
    [SetPuckCulture]
    [Auth(Roles=PuckRoles.Tasks)]
    public class TaskController : BaseController
    {
        I_Puck_Repository repo;
        I_Log log;
        ApiHelper apiHelper;
        public TaskController(ApiHelper ah,I_Puck_Repository repo,I_Log log) {
            this.apiHelper = ah;
            this.repo = repo;
            this.log = log;
        }

        public ActionResult Templates(string path)
        {
            var basePath = PuckCache.TemplateDirectory;
            path = basePath + path;
            var ppath = Server.MapPath(path);

            var directory = new DirectoryInfo(ppath);

            var directoriesAtPath = directory.GetDirectories();
            var filesAtPath = directory.GetFiles();

            var result = new List<dynamic>();

            Func<string,bool,string> pathFromBase =(p,isFolder) =>
            {
                p = p.Replace(Server.MapPath(basePath), "").Replace("\\", "/") + (isFolder?"/":"");
                return p;
            };

            directoriesAtPath.ToList().ForEach(x => result.Add(new
            {
                Name = x.Name,
                Path = pathFromBase(x.FullName,true),
                Type = "folder",
                HasChildren = x.GetFiles().Any() || x.GetDirectories().Any()
            }));
            filesAtPath.ToList().ForEach(x => result.Add(new { Name = x.Name, Path = pathFromBase(x.FullName,false), Type = "file", HasChildren = false }));

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CreateFolder(string path="") {
            var model = new CreateFolder {Path=path};
            return View(model);
        }
        [HttpPost]
        public ActionResult CreateFolder(CreateFolder model)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                model.Path = model.Path == "/" ? "" : model.Path;
                if (!ModelState.IsValid){
                    var errors = string.Join("<br/>", ModelState.SelectMany(x => x.Value.Errors).Select(x => x.ErrorMessage));
                    throw new Exception(errors);
                }
                var destPath = PuckCache.TemplateDirectory + model.Path + model.Name;
                var absDestPath = Server.MapPath(destPath);
                if (Directory.Exists(absDestPath))
                    throw new Exception("folder with that name already exists");
                Directory.CreateDirectory(absDestPath);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return Json(new { success = success, message = message }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult MoveTemplateFolder(string from, string to)
        {
            bool success = false;
            string message = "";
            try
            {
                var currentPath = PuckCache.TemplateDirectory + from;
                var absCurrentPath = Server.MapPath(currentPath);

                var destPath = PuckCache.TemplateDirectory + to;
                var absDestPath = Server.MapPath(destPath);

                if (absCurrentPath.ToLower().Equals(absDestPath.ToLower()))
                    throw new Exception("you are trying to move a folder to itself. wake up.");

                if (to.ToLower().StartsWith(from.ToLower()))
                    throw new Exception("you are trying to put a parent folder inside a child folder. this is incest and forbidden.");
                
                if (!System.IO.Directory.Exists(absCurrentPath))
                    throw new Exception("no folder to move at path - " + absCurrentPath);

                if (!System.IO.Directory.Exists(absDestPath))
                    throw new Exception("no folder to move to at path - " + absDestPath);

                var fromDirectory = new DirectoryInfo(absCurrentPath);
                fromDirectory.MoveTo(absDestPath+fromDirectory.Name);
                
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return Json(new { message = message, success = success }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult MoveTemplate(string from,string to)
        {
            bool success = false;
            string message = "";
            try
            {
                var currentPath = PuckCache.TemplateDirectory + from;
                var absCurrentPath = Server.MapPath(currentPath);

                var destPath = PuckCache.TemplateDirectory + to;
                var absDestPath = Server.MapPath(destPath);
                
                if (!System.IO.File.Exists(absCurrentPath))
                    throw new Exception("no file to move at path - " + absCurrentPath);

                if (!System.IO.Directory.Exists(absDestPath))
                    throw new Exception("no folder to move to at path - " + absDestPath);

                var f = new FileInfo(absCurrentPath);
                var destFileName = absDestPath + Path.GetFileName(absCurrentPath);
                f.MoveTo(destFileName);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return Json(new { message = message, success = success }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult DeleteTemplate(string path)
        {
            bool success = false;
            string message = "";
            try
            {
                var destPath = PuckCache.TemplateDirectory + path;
                var absDestPath = Server.MapPath(destPath);
                if (!System.IO.File.Exists(absDestPath))
                    throw new Exception("no file to delete at path - " + absDestPath);
                System.IO.File.Delete(absDestPath);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return Json(new { message = message, success = success }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult DeleteTemplateFolder(string path)
        {
            bool success = false;
            string message = "";
            try
            {
                var destPath = PuckCache.TemplateDirectory + path;
                var absDestPath = Server.MapPath(destPath);
                if (!System.IO.Directory.Exists(absDestPath))
                    throw new Exception("no folder to delete at path - " + absDestPath);
                System.IO.Directory.Delete(absDestPath,true);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return Json(new { message = message, success = success }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CreateTemplate(string path="")
        {
            var model = new CreateTemplate {Path=path};
            return View(model);
        }
        [HttpPost]
        public ActionResult CreateTemplate(CreateTemplate model)
        {
            bool success = false;
            string message = "";
            try
            {
                model.Path = model.Path == "/" ? "" : model.Path;
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("<br/>", ModelState.SelectMany(x => x.Value.Errors).Select(x => x.ErrorMessage));
                    throw new Exception(errors);
                }
                string modelname = "";
                var isGenerated = false;
                
                if (!string.IsNullOrEmpty(model.TemplateModel)) {
                    var type = ApiHelper.GetType(model.TemplateModel);
                    modelname = type.FullName;
                    isGenerated = typeof(I_Generated).IsAssignableFrom(type);
                }
                var destPath = PuckCache.TemplateDirectory + model.Path + model.Name + ".cshtml";
                var absDestPath = Server.MapPath(destPath);
                if (System.IO.File.Exists(absDestPath))
                    throw new Exception("file with that name already exists");

                var contents = "";
                if (!string.IsNullOrEmpty(modelname)) {
                    if (isGenerated) {
                        contents += string.Concat("@model dynamic\n@{/*",modelname,"*/}\n\n");
                    } else {
                        contents += string.Concat("@model ", modelname,"\n\n");
                    }
                }
                
                System.IO.File.WriteAllText(absDestPath, 
                    contents
                    );
                
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return Json(new {name=model.Path+model.Name+".cshtml", message = message, success = success }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetViewFileContent(string fp) {
            var absPath = Server.MapPath(PuckCache.TemplateDirectory + fp);
            var content = System.IO.File.ReadAllText(absPath);
            return Content(content);
        }

        [ValidateInput(false)]
        public ActionResult SaveFileContent(string fp,string content)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                var absPath = Server.MapPath(PuckCache.TemplateDirectory + fp);
                System.IO.File.WriteAllText(absPath, content);
                success = true;
            }catch(Exception ex){
                message = ex.Message;
            }
            return Json(new {success=success,message=message },JsonRequestBehavior.AllowGet);
        }

        //model stuff

        public ActionResult Models(string parent) {
            var success = false;
            var message = string.Empty;
            List<GeneratedModel> models=null;                
            try{
                if (string.IsNullOrEmpty(parent))
                    models = repo.GetGeneratedModel().ToList();
                else
                    models = repo.GetGeneratedModel().Where(x => x.Inherits.Equals(parent)).ToList();
                success = true;
            }
            catch (Exception ex) {
                message = ex.Message;
            }
            return Json(new {models=models,success=success,message=message});
        }
                
        public ActionResult EditAttribute(int id,int pid,string optionType)
        {
            var attribute = new GeneratedAttribute();
            if (id > -1)
            {
                attribute = repo.GetGeneratedAttribute().FirstOrDefault(x => x.ID == id);
            }
            ViewBag.pid = pid;
            ViewBag.aid = id;
            ViewBag.type = optionType;
            var modelType = Type.GetType(optionType);
            I_GeneratedOption model;
            if (string.IsNullOrEmpty(attribute.Value))
                model = ApiHelper.CreateInstance(modelType) as I_GeneratedOption;
            else
                model = JsonConvert.DeserializeObject(attribute.Value, modelType) as I_GeneratedOption;
            return View(model);
        }
        [HttpPost]
        public ActionResult EditAttribute(int pid,int aid,string optionType,FormCollection fc)
        {
            var success = false;
            var message = string.Empty;
            var _id = -1;
            try
            {
                if (aid < 1)
                {
                    var prop = repo.GetGeneratedProperty().Where(x => x.ID == pid).FirstOrDefault();
                    var att = new GeneratedAttribute {Type=optionType};
                    var model = ApiHelper.CreateInstance(ApiHelper.GetType(optionType));
                    UpdateModelDynamic(model,fc.ToValueProvider());
                    var value = JsonConvert.SerializeObject(model);
                    att.Value = value;
                    prop.Attributes.Add(att);
                    repo.SaveChanges();
                    _id = att.ID;
                }
                else
                {
                    var att = repo.GetGeneratedAttribute().Where(x => x.ID == aid).FirstOrDefault();
                    var model = ApiHelper.CreateInstance(ApiHelper.GetType(optionType));
                    UpdateModelDynamic(model,fc.ToValueProvider());
                    var value = JsonConvert.SerializeObject(model);
                    att.Value = value;
                    repo.SaveChanges();
                    _id = att.ID;
                }
            }
            catch (Exception ex){
                message = ex.Message;
            }

            return Json(new { success = true, message = message, id = _id }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult EditProperty(int id,int mid)
        {
            var model = new GeneratedProperty{};
            if (id > -1)
            {
                model = repo.GetGeneratedProperty().FirstOrDefault(x => x.ID == id);
            }
            ViewBag.mid = mid;
            return View(model);
        }
        [HttpPost]
        public ActionResult EditProperty(int mid,GeneratedProperty gp)
        {
            var success = false;
            var message = string.Empty;
            var _id = -1;
            try
            {
                if (ModelState.Any(x => x.Value.Errors.Any()))
                {
                    var errs =
                        string.Join("<br/>", ModelState.SelectMany(x => x.Value.Errors.Select(xx => "<br/>" + xx.ErrorMessage)));
                    throw new Exception(errs);
                }                
                if (gp.ID < 1)
                {
                    var mod = repo.GetGeneratedModel().Where(x => x.ID == mid).FirstOrDefault();
                    if (DuplicateProperty(mod.Name, mod)) {
                        throw new Exception("duplicate property: " + mod.Name);
                    }
                    mod.Properties.Add(gp);
                }
                else {
                    var mod = repo.GetGeneratedProperty().Where(x => x.ID == gp.ID).FirstOrDefault();
                    mod.Name = gp.Name;
                    mod.Type = gp.Type;
                }
                repo.SaveChanges();
                _id = gp.ID;
            }
            catch (Exception ex){
                message = ex.Message;
            }

            return Json(new { success = true, message = message, id = _id }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult EditModel(int id)
        {
            var model = new GeneratedModel();
            if (id > -1)
                model = repo.GetGeneratedModel().FirstOrDefault(x=>x.ID==id);
            return View(model);
        }
        [HttpPost]
        public ActionResult EditModel(GeneratedModel gm) {
            var success = false;
            var message = string.Empty;
            var _id = -1;
            try {
                if (gm.ID < 1)
                {
                    repo.AddGeneratedModel(gm);
                }
                else {
                    var mod = repo.GetGeneratedModel().Where(x => x.ID == gm.ID).FirstOrDefault();
                    mod.Name = gm.Name;
                    mod.Inherits = gm.Inherits;
                    if (!string.IsNullOrEmpty(mod.Inherits)) {
                        var parentType = repo.GetGeneratedModel().Where(x => x.IFullName.Equals(mod.Inherits)).FirstOrDefault();
                        if (parentType != null){
                            var dupes = new List<string>();
                            if (DuplicateProperties(mod,parentType,dupes))
                                throw new Exception("following properties already exist: "+string.Join(", ",dupes));
                        }
                    }
                }
                repo.SaveChanges();
                _id = gm.ID;
            }catch(Exception ex){
                message = ex.Message;
            }

            return Json(new{success=true,message=message,id=_id},JsonRequestBehavior.AllowGet);
        }

        public bool DuplicateProperty(string propName, GeneratedModel model)
        {
            if (model.Properties.Any(x => x.Name.ToLower().Equals(propName.ToLower())))
                return true;
            return false;
        }
        public bool DuplicateProperties(GeneratedModel model, GeneratedModel parent, List<string> dupes){
            var mod = model;
            var isDuplicate = false;
            var modelProps = model.Properties.ToList();
            var parentProps = apiHelper.AllProperties(parent);
            dupes = modelProps.Select(x => x.Name.ToLower()).Intersect(parentProps.Select(x => x.Name.ToLower())).ToList();
            if (dupes.Any())
                isDuplicate = true;
            return isDuplicate;
        }

        [HttpPost]
        public ActionResult DeleteModel(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                var model = repo.GetGeneratedModel().FirstOrDefault(x => x.ID == id);
                repo.DeleteGeneratedModel(model);
                repo.SaveChanges();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return Json(new { success = true, message = message, id = id }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult DeleteProperty(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                var model = repo.GetGeneratedProperty().FirstOrDefault(x => x.ID == id);
                repo.DeleteGeneratedProperty(model);
                repo.SaveChanges();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return Json(new { success = true, message = message, id = id }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult DeleteAttribute(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                var model = repo.GetGeneratedAttribute().FirstOrDefault(x => x.ID == id);
                repo.DeleteGeneratedAttribute(model);
                repo.SaveChanges();                
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return Json(new { success = true, message = message, id = id }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GenerateModel(int id,bool compile=true)
        {
            string output = "";
            var message = string.Empty;
            var success = false;
            try
            {                
                DoGenerate(id,out output,compile);
                if (compile)
                {
                    StateHelper.SetGeneratedMappings();
                    StateHelper.UpdateAnalyzerMappings();
                }
                success = true;
            }
            catch (Exception ex) {
                message = ex.Message;
            }
            return Json(new{message=message,success=success,Output=output},JsonRequestBehavior.AllowGet);
        }

        public void DoGenerate(int id,out string cs_source,bool compile=true) {
            var gm = this.repo.GetGeneratedModel().Where(x => x.ID == id).FirstOrDefault();
            var currentClassName = gm.CName;

            var itemplate = System.IO.File.ReadAllText(HttpContext.Server.MapPath("~/app_data/generated/i_template.txt"));
            var cstemplate = System.IO.File.ReadAllText(HttpContext.Server.MapPath("~/app_data/generated/cs_template.txt"));
            var stamp = DateTime.Now;
            var stampstr = stamp.ToString("yyyyMMddHHmmss");

            var iname = string.Concat("I_", ApiHelper.SanitizeClassName(gm.Name), "_", stampstr);
            var cname = string.Concat(ApiHelper.SanitizeClassName(gm.Name), "_", stampstr);

            var sourceBasePath = string.Concat("~/app_data/generated/");

            var sourceVIBasePath = string.Concat(sourceBasePath, "abstract/");
            var sourceVBasePath = string.Concat(sourceBasePath, "concrete/");

            var sourceVIPath = string.Concat(sourceVIBasePath, iname, ".dll");
            var sourceVPath = string.Concat(sourceVBasePath, cname, ".dll");

            var sourcePIPath = HttpContext.Server.MapPath(sourceVIPath);
            var sourcePPath = HttpContext.Server.MapPath(sourceVPath);

            new FileInfo(sourcePPath).Directory.Create();
            /*
            var gen_interfaces = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(x => x.GetTypes()).Where(x => x.IsInterface && typeof(I_Generated).IsAssignableFrom(x) && x!=typeof(I_Generated) );
            */
            /*
            var interfaceg = gen_interfaces
                .Where(x => x.FullName.Equals(string.Concat("puck.Generated.Abstract.I_",gm.Name))).FirstOrDefault();
            */
            Type interfaceg;
            if (compile)
            {
                if (gm.IFullName == null)
                {
                    new FileInfo(sourcePIPath).Directory.Create();
                    var isource = itemplate
                        .Replace("//{interfacename}", string.Concat(iname))
                        .Replace("//{baseinterface}", string.Concat("I_Generated"))
                        .Replace("//{name}", gm.Name);
                    var iassembly = CodeGenerator.PuckCompiler.CompileCode(isource);

                    System.IO.File.Copy(iassembly.Location, sourcePIPath, true);
                    interfaceg = iassembly.GetTypes().First();
                    gm.IFullPath = sourceVIPath;
                    gm.IFullName = interfaceg.AssemblyQualifiedName;
                }
                else
                {
                    interfaceg = ApiHelper.GetType(gm.IFullName);
                    iname = interfaceg.Name;
                }
            }
            var properties = new StringBuilder();

            var propList = gm.Properties.ToList();

            foreach (var prop in propList)
            {
                var propertyEntry = GeneratorValues.PropertyType[prop.Type];
                prop.Attributes.ToList().ForEach(x => {
                    try
                    {
                        I_GeneratedOption opt = JsonConvert.DeserializeObject(x.Value, ApiHelper.GetType(x.Type)) as I_GeneratedOption; 
                        properties.AppendLine(
                            opt.OutputString()
                        );
                    }
                    catch (Exception ex) {
                        var exx = new Exception("error rendering attribute - "+x.ID,ex);
                        log.Log(ex);
                    }  
                });
                var uiHint = propertyEntry.Type.GetCustomAttributes(typeof(PuckHint), false).FirstOrDefault() as PuckHint;
                if (uiHint != null && !string.IsNullOrEmpty(uiHint.Name))
                {
                    properties.AppendLine(string.Format("[UIHint(\"{0}\")]", uiHint.Name));
                }
                properties.AppendLine(string.Format("[DisplayName(\"{0}\")]",prop.Name));
                properties.AppendLine(propertyEntry.AttributeString);
                properties.AppendLine(
                    string.Concat("public ", propertyEntry.Type.FullName, " ", ApiHelper.SanitizePropertyName(prop.Name), "{get;set;}")
                );
            };
            var inherits = "BaseModel";
            if (!string.IsNullOrEmpty(gm.Inherits))
            {
                var inheritedType = ApiHelper.GetType(gm.Inherits);
                inherits = inheritedType.Name;
                if (!string.IsNullOrEmpty(gm.CName))
                {
                    var modelType = ApiHelper.GetType(gm.CName);
                    var baseTypesForInheritedType = ApiHelper.BaseTypes(inheritedType);
                    if (baseTypesForInheritedType.Contains(modelType))
                        throw new Exception("circular inheritance chain detected");
                }
            }
            var source = cstemplate
                .Replace("//{classname}", string.Concat(cname))
                .Replace("//{baseclass}", string.Concat(inherits))
                .Replace("//{interface}", string.Concat(iname))
                .Replace("//{properties}", properties.ToString())
                .Replace("//{name}", gm.Name);
            cs_source = source;                

            if (compile)
            {
                var assembly = CodeGenerator.PuckCompiler.CompileCode(source);
                System.IO.File.Copy(assembly.Location, sourcePPath, true);
                
                var ctype = assembly.GetTypes().First();

                gm.CFullPath = sourceVPath;
                gm.CName = ctype.AssemblyQualifiedName;

                repo.SaveChanges();
                //get all models which inherit from current edited model, they need to be updated
                var models = repo.GetGeneratedModel().Where(x => x.Inherits.Equals(currentClassName)).ToList();
                //order by least amount of dependencies
                models = models
                    .OrderBy(x =>
                        repo.GetGeneratedModel().Where(xx => xx.Inherits.Equals(x.CName)).Count())
                    .ToList();
                foreach (var m in models)
                {
                    string output = "";
                    m.Inherits = gm.CName;
                    repo.SaveChanges();
                    DoGenerate(m.ID, out output);
                }
            }
        }

        public ActionResult PreviewEditor(string type) {
            var cstemplate = System.IO.File.ReadAllText(HttpContext.Server.MapPath("~/app_data/generated/cs_template.txt"));
            var propertyEntry = GeneratorValues.PropertyType[type];
            var reg = new Regex("using\\s[a-zA-Z0-9\\.]+;");
            var matches = reg.Matches(cstemplate);
            var uses = "";
            for(var i=0;i < matches.Count;i++) { 
                uses += matches[i].Value;
            }
            var source = string.Format("{0} \n public class test_{1} {{ {2} public {3} Preview {{ get;set; }} }}",
                uses,DateTime.Now.ToString("yyyyMMddHHmmss"),propertyEntry.AttributeString,propertyEntry.Type);
            var assembly = CodeGenerator.PuckCompiler.CompileCode(source);
            var t = assembly.GetTypes().First();
            var instance = Activator.CreateInstance(t);
            return View(instance);
        }

        //
        // GET: /admin/Task/

        public ActionResult Index()
        {
            var model = new TasksModel();
            model.Tasks = apiHelper.Tasks();
            model.GeneratedModels = repo.GetGeneratedModel().ToList();
            return View(model);
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
            var ttypes = apiHelper.TaskTypes();
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
                StateHelper.UpdateTaskMappings();
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
                StateHelper.UpdateTaskMappings();
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
