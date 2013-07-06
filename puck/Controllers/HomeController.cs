using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using puck.core.Constants;
using puck.core.Base;
using Newtonsoft.Json;
using puck.core.Abstract;
using puck.core.Controllers;
using System.Web.Caching;
using System.Threading;
using System.Globalization;
using System.Reflection;
using System.Diagnostics;
using puck.core.Filters;

namespace puck.Controllers
{
    public class HomeController : BaseController
    {
        I_Log log;
        public HomeController(I_Log log) {
            this.log = log;
        }

        [OutputCache(Duration=10,VaryByParam="*")]
        [CacheValidate]
        public ActionResult Index(string path)
        {
            try
            {
                //inspect cache attribute, if set in current method or stack below
                if (PuckCache.DefaultOutputCacheMinutes == -1){
                    var st = new StackTrace(false);
                    if (st.FrameCount > 1)
                    {
                        var frame = st.GetFrame(1);
                        var outputCacheAttribute = frame.GetMethod().GetCustomAttribute(typeof(OutputCacheAttribute)) as OutputCacheAttribute;
                        if (outputCacheAttribute == null)
                        {
                            frame = st.GetFrame(0);
                            outputCacheAttribute = frame.GetMethod().GetCustomAttribute(typeof(OutputCacheAttribute)) as OutputCacheAttribute;
                        }
                        if (outputCacheAttribute != null)
                        {
                            PuckCache.DefaultOutputCacheMinutes = outputCacheAttribute.Duration;
                        }
                        else
                        {
                            PuckCache.DefaultOutputCacheMinutes = 0;
                        }
                    }
                    else {
                        PuckCache.DefaultOutputCacheMinutes = 0;
                    }
                }
                //do redirects
                string redirectUrl;
                if (PuckCache.Redirect301.TryGetValue(Request.Url.AbsolutePath, out redirectUrl)) {
                    Response.Cache.SetCacheability(HttpCacheability.Public);
                    Response.Cache.SetExpires(DateTime.Now.AddMinutes(PuckCache.RedirectOuputCacheMinutes));
                    Response.RedirectPermanent(redirectUrl,true);
                }
                if (PuckCache.Redirect302.TryGetValue(Request.Url.AbsolutePath, out redirectUrl))
                {
                    Response.Cache.SetCacheability(HttpCacheability.Public);
                    Response.Cache.SetExpires(DateTime.Now.AddMinutes(PuckCache.RedirectOuputCacheMinutes));
                    Response.Redirect(redirectUrl, true);
                }

                var dmode = this.GetDisplayModeId();
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
                //set thread culture for future api calls on this thread
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(variant);

                var results = puck.core.Helpers.QueryHelper<BaseModel>.Query(
                    string.Concat("+", FieldKeys.Path, ":", searchPath, " +", FieldKeys.Variant, ":", variant)
                    );

                var result = results.FirstOrDefault();
                object model = null;
                if (result != null)
                {
                    model = JsonConvert.DeserializeObject(result[FieldKeys.PuckValue], Type.GetType(result[FieldKeys.PuckType]));
                    if (!PuckCache.OutputCacheExclusion.Contains(Request.Url.AbsolutePath))
                    {
                        int cacheMinutes;
                        if (PuckCache.TypeOutputCache.TryGetValue(result[FieldKeys.PuckType], out cacheMinutes))
                        {
                            Response.Cache.SetCacheability(HttpCacheability.Public);
                            Response.Cache.SetExpires(DateTime.Now.AddMinutes(cacheMinutes));
                        }
                        else {
                            Response.Cache.SetCacheability(HttpCacheability.Public);
                            Response.Cache.SetExpires(DateTime.Now.AddMinutes(PuckCache.DefaultOutputCacheMinutes));
                        }
                    }
                }

                if (model == null)
                {
                    //404
                    return View(PuckCache.Path404);
                }
                string templatePath = result[FieldKeys.TemplatePath];
                if (!string.IsNullOrEmpty(dmode)) {
                    string cacheKey = CacheKeys.PrefixTemplateExist + dmode + templatePath;
                    if (HttpContext.Cache[cacheKey] != null)
                    {
                        templatePath = HttpContext.Cache[cacheKey] as string;
                    }
                    else
                    {
                        string dpath = templatePath.Insert(templatePath.LastIndexOf('.') + 1, dmode + ".");
                        if (System.IO.File.Exists(Server.MapPath(dpath)))
                        {
                            templatePath = dpath;
                        }
                        HttpContext.Cache.Insert(cacheKey,templatePath,null,Cache.NoAbsoluteExpiration,TimeSpan.FromMinutes(10));
                    }
                }
                return View(templatePath, model);
            }
            catch (Exception ex) {
                log.Log(ex);
                ViewBag.error = ex.Message;
                return View(PuckCache.Path500);
            }
        }
                
    }
}
