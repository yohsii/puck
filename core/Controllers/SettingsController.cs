using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using puck.core.Abstract;
using puck.core.Models;
using puck.core.Constants;
using System.Web.Script.Serialization;
using puck.core.Entities;
using puck.core.Helpers;
using puck.core.Filters;
using Newtonsoft.Json;
using puck.core.CodeGenerator;

namespace puck.core.Controllers
{

    [SetPuckCulture]
    [Auth(Roles=PuckRoles.Settings)]
    public class SettingsController : BaseController
    {
        I_Content_Indexer indexer;
        I_Content_Searcher searcher;
        I_Log log;
        I_Puck_Repository repo;
        public SettingsController(I_Content_Indexer i, I_Content_Searcher s, I_Log l, I_Puck_Repository r) {
            this.indexer = i;
            this.searcher = s;
            this.log = l;
            this.repo = r;
        }

        public JsonResult EditViewModels() {
            var source = "";
            source = System.IO.File.ReadAllText(Server.MapPath("~/App_Data/source.txt"));
            PuckCompiler.CompileCode(source);
            return Json("",JsonRequestBehavior.AllowGet);
        }

        public JsonResult DeleteParameters(string key) {
            bool success = false;
            string message = "";
            try
            {
                repo.GetPuckMeta().Where(x => x.Name == DBNames.EditorSettings && x.Key == key).ToList().ForEach(x=>repo.DeleteMeta(x));
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

        public ActionResult EditParameters(string settingsType,string modelType,string propertyName) {
            string key = string.Concat(settingsType, ":", modelType, ":", propertyName);
            var typeSettings = Type.GetType(settingsType);
            var meta = repo.GetPuckMeta().Where(x=>x.Name==DBNames.EditorSettings && x.Key == key).FirstOrDefault();
            object model = null;
            if (meta != null) {
                try {
                    model = JsonConvert.DeserializeObject(meta.Value, typeSettings);
                }
                catch (Exception ex) {
                    log.Log(ex);
                }
            }
            if (model == null) {
                model = Activator.CreateInstance(typeSettings);
            }
            return View(model);
        }

        [HttpPost]
        public JsonResult EditParameters(string puck_settingsType,string puck_modelType,string puck_propertyName,FormCollection fc) {
            string key = string.Concat(puck_settingsType, ":", puck_modelType, ":", puck_propertyName);
            var targetType = Type.GetType(puck_settingsType);
            var model = Activator.CreateInstance(targetType);
            bool success = false;
            string message = "";
            try
            {
                UpdateModelDynamic(model, fc.ToValueProvider());
                PuckMeta settingsMeta = null;
                settingsMeta = repo.GetPuckMeta().Where(x =>x.Name==DBNames.EditorSettings && x.Key == key).FirstOrDefault();
                if(settingsMeta != null){
                    settingsMeta.Value = JsonConvert.SerializeObject(model);
                }
                else
                {
                    settingsMeta = new PuckMeta();
                    settingsMeta.Name = DBNames.EditorSettings;
                    settingsMeta.Key = key;
                    settingsMeta.Value = JsonConvert.SerializeObject(model);
                    repo.AddMeta(settingsMeta);
                }
                repo.SaveChanges();
                ApiHelper.OnAfterSettingsSave(this,new puck.core.Events.AfterEditorSettingsSaveEventArgs {Setting=(I_Puck_Editor_Settings)model});
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

        public ActionResult Edit()
        {
            var model = new Settings();
            var meta = repo.GetPuckMeta();

            var defaultLanguage = meta.Where(x => x.Name == DBNames.Settings && x.Key == DBKeys.DefaultLanguage).FirstOrDefault();
            var enableLocalePrefix = meta.Where(x => x.Name == DBNames.Settings && x.Key == DBKeys.EnableLocalePrefix).FirstOrDefault();
            var redirects = meta.Where(x => x.Name == DBNames.Redirect301 || x.Name==DBNames.Redirect302).ToList()
                .Select(x=>new KeyValuePair<string,string>(x.Name+x.Key,x.Value)).ToDictionary(x=>x.Key,x=>x.Value);
            var pathToLocale = meta.Where(x => x.Name == DBNames.PathToLocale).ToList().Select(x => new KeyValuePair<string, string>(x.Key, x.Value)).ToDictionary(x=>x.Key,x=>x.Value);
            var languages = meta.Where(x => x.Name == DBNames.Settings && x.Key == DBKeys.Languages).ToList().Select(x=>x.Value).ToList();
            var fieldGroups = meta.Where(x => x.Name.StartsWith(DBNames.FieldGroups)).ToList();
            var typeAllowedTypes = meta.Where(x => x.Name == DBNames.TypeAllowedTypes).Select(x=>x.Key+":"+x.Value).ToList();
            var editorParameters = meta.Where(x => x.Name == DBNames.EditorSettings).Select(x=>x.Key).ToList();
            var cachePolicy = meta.Where(x => x.Name == DBNames.CachePolicy).Select(x=>x.Key+":"+x.Value).ToList();
            var typeAllowedTemplates = meta.Where(x => x.Name == DBNames.TypeAllowedTemplates).Select(x => x.Key + ":" + x.Value).ToList();
            model.TypeGroupField = new List<string>();
            
            fieldGroups.ForEach(x => {
                string typeName = x.Name.Replace(DBNames.FieldGroups,"");
                string groupName = x.Key;
                string FieldName = x.Value;
                model.TypeGroupField.Add(string.Concat(typeName,":",groupName,":",FieldName));                
            });
            model.TypeAllowedTemplates = typeAllowedTemplates;
            model.TypeAllowedTypes = typeAllowedTypes;
            model.DefaultLanguage = defaultLanguage == null ? "" : defaultLanguage.Value;
            model.EnableLocalePrefix = enableLocalePrefix == null ? false : bool.Parse(enableLocalePrefix.Value);
            model.Languages = languages;
            model.PathToLocale = pathToLocale;
            model.Redirect = redirects;
            model.EditorParameters = editorParameters;
            model.CachePolicy = cachePolicy;
            return View(model);
        }

        //
        // POST: /admin/Settings/Edit/5
        [ValidateInput(enableValidation:false)]
        [HttpPost]
        public JsonResult Edit(Settings model)
        {
            string msg = "";
            bool success = false;
            try
            {
                // TODO: Add update logic here
                //default language
                if (!string.IsNullOrEmpty(model.DefaultLanguage)) {
                    var metaDL = repo.GetPuckMeta().Where(x => x.Name == DBNames.Settings && x.Key == DBKeys.DefaultLanguage).FirstOrDefault();
                    if (metaDL != null)
                    {
                        metaDL.Value = model.DefaultLanguage;
                    }
                    else {
                        var newMeta = new PuckMeta();
                        newMeta.Name = DBNames.Settings;
                        newMeta.Key = DBKeys.DefaultLanguage;
                        newMeta.Value = model.DefaultLanguage;
                        repo.AddMeta(newMeta);
                    }
                }
                //enable local prefix
                var metaELP = repo.GetPuckMeta().Where(x => x.Name == DBNames.Settings && x.Key == DBKeys.EnableLocalePrefix).FirstOrDefault();
                if (metaELP != null)
                {
                    metaELP.Value = model.EnableLocalePrefix.ToString();
                }
                else {
                    var newMeta = new PuckMeta();
                    newMeta.Name = DBNames.Settings;
                    newMeta.Key = DBKeys.EnableLocalePrefix;
                    newMeta.Value = model.EnableLocalePrefix.ToString();
                    repo.AddMeta(newMeta);
                }
                //languages
                if (model.Languages!=null && model.Languages.Count > 0)
                {
                    var metaLanguages = repo.GetPuckMeta().Where(x => x.Name == DBNames.Settings && x.Key == DBKeys.Languages).ToList();
                    if (metaLanguages.Count > 0)
                    {
                        metaLanguages.ForEach(x =>
                        {
                            repo.DeleteMeta(x);
                        });
                    }
                    model.Languages.ForEach(x => {
                        var newMeta = new PuckMeta();
                        newMeta.Name = DBNames.Settings;
                        newMeta.Key = DBKeys.Languages;
                        newMeta.Value = x;
                        repo.AddMeta(newMeta);
                    });
                }
                //redirects
                if (model.Redirect!=null&&model.Redirect.Count > 0) {
                    var redirectMeta = repo.GetPuckMeta().Where(x => x.Name == DBNames.Redirect301 || x.Name==DBNames.Redirect302).ToList();
                    redirectMeta.ForEach(x => {
                        repo.DeleteMeta(x);
                    });
                    //count of 1 and key/value of null indicates delete only so inserts are skipped
                    if (!(model.Redirect.Count == 1 && string.IsNullOrEmpty(model.Redirect.First().Key)))
                    {
                        model.Redirect.ToList().ForEach(x =>
                        {
                            var newMeta = new PuckMeta();
                            newMeta.Name = x.Key.StartsWith(DBNames.Redirect301) ? DBNames.Redirect301 : DBNames.Redirect302;
                            newMeta.Key = x.Key.Substring(newMeta.Name.Length);
                            newMeta.Value = x.Value;
                            repo.AddMeta(newMeta);
                        });
                    }
                }
                //fieldgroup
                if (model.TypeGroupField!=null&&model.TypeGroupField.Count > 0) {
                    foreach (var mod in ApiHelper.AllModels(true))
                    {
                        var fieldGroupMeta = repo.GetPuckMeta().Where(x => x.Name.StartsWith(DBNames.FieldGroups+mod.AssemblyQualifiedName)).ToList();
                        fieldGroupMeta.ForEach(x =>
                        {
                            repo.DeleteMeta(x);
                        });
                    }
                    model.TypeGroupField.ForEach(x => {
                        var values = x.Split(new char[]{':'},StringSplitOptions.RemoveEmptyEntries);
                        var newMeta = new PuckMeta();
                        newMeta.Name = DBNames.FieldGroups+values[0];
                        newMeta.Key = values[1];
                        newMeta.Value=values[2];
                        repo.AddMeta(newMeta);
                    });
                }
                //typeallowedtypes
                if (model.TypeAllowedTypes != null && model.TypeAllowedTypes.Count > 0){
                    var typeAllowedTypesMeta = repo.GetPuckMeta().Where(x => x.Name == DBNames.TypeAllowedTypes).ToList();
                    typeAllowedTypesMeta.ForEach(x => {
                        repo.DeleteMeta(x);
                    });
                    model.TypeAllowedTypes.ForEach(x => {
                        var values = x.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                        var newMeta = new PuckMeta();
                        newMeta.Name = DBNames.TypeAllowedTypes;
                        newMeta.Key = values[0];
                        newMeta.Value = values[1];
                        repo.AddMeta(newMeta);
                    });
                }
                //typeallowedtemplates
                if (model.TypeAllowedTemplates != null && model.TypeAllowedTemplates.Count > 0)
                {
                    var typeAllowedTemplatesMeta = repo.GetPuckMeta().Where(x => x.Name == DBNames.TypeAllowedTemplates).ToList();
                    typeAllowedTemplatesMeta.ForEach(x =>
                    {
                        repo.DeleteMeta(x);
                    });
                    model.TypeAllowedTemplates.ForEach(x =>
                    {
                        var values = x.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                        var newMeta = new PuckMeta();
                        newMeta.Name = DBNames.TypeAllowedTemplates;
                        newMeta.Key = values[0];
                        newMeta.Value = values[1];
                        repo.AddMeta(newMeta);
                    });
                }
                //cachepolicy
                if (model.CachePolicy == null)
                    model.CachePolicy = new List<string>();
                var cacheTypes = new List<string>();
                if (model.CachePolicy.Count > 0) {
                    foreach (var entry in model.CachePolicy) {
                        var type = entry.Split(new char[]{':'},StringSplitOptions.RemoveEmptyEntries)[0];
                        cacheTypes.Add(type);
                        var minutes = entry.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[1];
                        int min;
                        if(!int.TryParse(minutes,out min))
                            throw new Exception("cache policy minutes not int for type:"+type);
                        var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.CachePolicy && x.Key.ToLower().Equals(type.ToLower())).FirstOrDefault();
                        if (meta != null)
                        {
                            meta.Value = minutes;
                        }
                        else {
                            meta = new PuckMeta() { Name=DBNames.CachePolicy,Key=type,Value=minutes};
                            repo.AddMeta(meta);
                        }
                    }
                }
                //delete unset
                repo.GetPuckMeta().Where(x => x.Name == DBNames.CachePolicy && !cacheTypes.Contains(x.Key)).ToList().ForEach(x => repo.DeleteMeta(x));
                
                //orphan types
                if (model.Orphans != null && model.Orphans.Count > 0)
                {
                    foreach (var entry in model.Orphans) {
                        var t1 = entry.Key;
                        var t2 = entry.Value;
                        ApiHelper.RenameOrphaned(t1, t2);
                    }
                }
                repo.SaveChanges();
                StateHelper.UpdateDefaultLanguage();
                StateHelper.UpdateCacheMappings();
                StateHelper.UpdateRedirectMappings();
                
                success = true;                
            }
            catch(Exception ex)
            {
                msg = ex.Message;
                success = false;
            }
            return Json(new { success=success,message = msg}, JsonRequestBehavior.AllowGet);
        }
                
    }
}
