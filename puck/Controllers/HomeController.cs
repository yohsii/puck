using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using puck.core.Constants;
using puck.core.Base;
using Newtonsoft.Json;
using puck.core.Abstract;

namespace puck.Controllers
{
    public class HomeController : Controller
    {
        I_Log log;
        public HomeController(I_Log log) {
            this.log = log;
        }

        public ActionResult Index(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    path = string.Empty;
                else
                    path = "/" + path;
                
                string domain = Request.Url.Host;
                string searchPathPrefix;
                if (!PuckCache.DomainRoots.TryGetValue(domain,out searchPathPrefix))
                {
                    if(!PuckCache.DomainRoots.TryGetValue("*",out searchPathPrefix))
                        throw new Exception("domain roots not set. DOMAIN:"+domain);
                }
                string searchPath = searchPathPrefix + path;
                
                string variant;
                if (!PuckCache.PathToLocale.TryGetValue(searchPath, out variant)){
                    //get closest ancestor variant
                    KeyValuePair<string,string>? entry = PuckCache.PathToLocale.Where(x => searchPath.StartsWith(x.Key)).OrderByDescending(x => x.Key.Length).FirstOrDefault();
                    if (entry.HasValue){
                        variant = entry.Value.Value;
                        PuckCache.PathToLocale[searchPath] = entry.Value.Value;
                    }else
                        variant = PuckCache.SystemVariant;
                }
                var results = puck.core.Helpers.QueryHelper<BaseModel>.Query(
                    string.Concat("+", FieldKeys.Path, ":", searchPath, " +", FieldKeys.Variant, ":", variant)
                    );

                var result = results.FirstOrDefault();
                object model = null;
                if (result != null)
                {
                    model = JsonConvert.DeserializeObject(result[FieldKeys.PuckValue], Type.GetType(result[FieldKeys.PuckType]));
                }

                if (model == null)
                {
                    //404
                    return View(PuckCache.Path404);
                }

                return View(result[FieldKeys.TemplatePath], model);
            }
            catch (Exception ex) {
                log.Log(ex);
                ViewBag.error = ex.Message;
                return View(PuckCache.Path500);
            }
        }
                
    }
}
