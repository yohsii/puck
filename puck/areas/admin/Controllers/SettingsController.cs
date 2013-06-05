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
            var redirects = meta.Where(x => x.Name == DBNames.Redirect).ToList().Select(x=>new KeyValuePair<string,string>(x.Key,x.Value)).ToDictionary(x=>x.Key,x=>x.Value);
            var pathToLocale = meta.Where(x => x.Name == DBNames.PathToLocale).ToList().Select(x => new KeyValuePair<string, string>(x.Key, x.Value)).ToDictionary(x=>x.Key,x=>x.Value);
            var languages = meta.Where(x => x.Name == DBNames.Settings && x.Key == DBKeys.Languages).ToList().Select(x=>x.Key).ToList();

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
                var metaELP = repo.GetPuckMeta().Where(x => x.Name == DBNames.Settings && x.Key == DBKeys.EnableLocalePrefix).FirstOrDefault();
                if (metaELP != null)
                {
                    metaELP.Value = model.EnableLocalePrefix.ToString();
                }
                else {
                    var newMeta = new PuckMeta();
                    newMeta.Name = DBNames.Settings;
                    newMeta.Key = DBKeys.DefaultLanguage;
                    newMeta.Value = model.EnableLocalePrefix.ToString();
                    repo.AddMeta(newMeta);
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
