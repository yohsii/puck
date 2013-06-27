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

namespace puck.core.Controllers
{
    public class SettingsController : Controller
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

            model.TypeGroupField = new List<string>();
            
            fieldGroups.ForEach(x => {
                string typeName = x.Name.Replace(DBNames.FieldGroups,"");
                string groupName = x.Key;
                string FieldName = x.Value;
                model.TypeGroupField.Add(string.Concat(typeName,":",groupName,":",FieldName));                
            });

            model.TypeAllowedTypes = typeAllowedTypes;
            model.DefaultLanguage = defaultLanguage == null ? "" : defaultLanguage.Value;
            model.EnableLocalePrefix = enableLocalePrefix == null ? false : bool.Parse(enableLocalePrefix.Value);
            model.Languages = languages;
            model.PathToLocale = pathToLocale;
            model.Redirect = redirects;

            return View(model);
        }

        //
        // POST: /admin/Settings/Edit/5

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
                    model.Redirect.ToList().ForEach(x => {
                        var newMeta = new PuckMeta();
                        newMeta.Name = x.Key.StartsWith(DBNames.Redirect301)?DBNames.Redirect301:DBNames.Redirect302;
                        newMeta.Key = x.Key.Substring(newMeta.Name.Length);
                        newMeta.Value = x.Value;
                        repo.AddMeta(newMeta);
                    });
                }
                //fieldgroup
                if (model.TypeGroupField!=null&&model.TypeGroupField.Count > 0) {
                    var fieldGroupMeta = repo.GetPuckMeta().Where(x => x.Name.StartsWith(DBNames.FieldGroups)).ToList();
                    fieldGroupMeta.ForEach(x => {
                        repo.DeleteMeta(x);
                    });
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
                repo.SaveChanges();
                success = true;
                //return RedirectToAction("Index");
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
