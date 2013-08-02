using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;
using System.Web.WebPages;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Newtonsoft.Json;
using puck.core.Base;
using puck.core.Constants;
using puck.core.Helpers;
using StackExchange.Profiling;

namespace puck.core.Controllers
{
    public class BaseController : Controller
    {

        public ActionResult Puck()
        {
            try
            {
                string path = Request.Url.AbsolutePath.ToLower();
                
                var dmode = this.GetDisplayModeId();
                                
                if (path=="/")
                    path = string.Empty;                
                
                string domain = Request.Url.Host.ToLower();
                string searchPathPrefix;
                if (!PuckCache.DomainRoots.TryGetValue(domain, out searchPathPrefix))
                {
                    if (!PuckCache.DomainRoots.TryGetValue("*", out searchPathPrefix))
                        throw new Exception("domain roots not set. DOMAIN:" + domain);
                }
                string searchPath = searchPathPrefix.ToLower() + path;

                //do redirects
                string redirectUrl;
                if (PuckCache.Redirect301.TryGetValue(searchPath, out redirectUrl) || PuckCache.Redirect302.TryGetValue(searchPath, out redirectUrl))
                {
                    Response.Cache.SetCacheability(HttpCacheability.Public);
                    Response.Cache.SetExpires(DateTime.Now.AddMinutes(PuckCache.RedirectOuputCacheMinutes));
                    Response.Cache.SetValidUntilExpires(true);
                    Response.RedirectPermanent(redirectUrl, true);
                }
                
                string variant;
                if (!PuckCache.PathToLocale.TryGetValue(searchPath, out variant))
                {
                    foreach (var entry in PuckCache.PathToLocale) {
                        if (searchPath.StartsWith(entry.Key)){
                            variant = entry.Value;
                            break;
                        }
                    }
                    if (string.IsNullOrEmpty(variant))
                        variant = PuckCache.SystemVariant;
                }
                //set thread culture for future api calls on this thread
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(variant);
                IList<Dictionary<string, string>> results;
#if DEBUG
                using (MiniProfiler.Current.Step("lucene"))
                {
                    results = puck.core.Helpers.QueryHelper<BaseModel>.Query(
                        string.Concat("+", FieldKeys.Path, ":", searchPath, " +", FieldKeys.Variant, ":", variant)
                        );                    
                }
#else                
                results = puck.core.Helpers.QueryHelper<BaseModel>.Query(
                        string.Concat("+", FieldKeys.Path, ":", searchPath, " +", FieldKeys.Variant, ":", variant)
                        );           
#endif
                var result = results == null ? null : results.FirstOrDefault();
                BaseModel model = null;
                if (result != null)
                {
#if DEBUG
                    using (MiniProfiler.Current.Step("deserialize"))
                    {
                        model = JsonConvert.DeserializeObject(result[FieldKeys.PuckValue], Type.GetType(result[FieldKeys.PuckType])) as BaseModel;
                    }
#else
                    model = JsonConvert.DeserializeObject(result[FieldKeys.PuckValue], Type.GetType(result[FieldKeys.PuckType])) as BaseModel;
#endif
                    if (!PuckCache.OutputCacheExclusion.Contains(searchPath))
                    {
                        int cacheMinutes;
                        if (!PuckCache.TypeOutputCache.TryGetValue(result[FieldKeys.PuckType], out cacheMinutes))
                        {
                            if (!PuckCache.TypeOutputCache.TryGetValue(typeof(BaseModel).AssemblyQualifiedName, out cacheMinutes))
                            {
                                cacheMinutes = PuckCache.DefaultOutputCacheMinutes;
                            }
                        }
                        Response.Cache.SetCacheability(HttpCacheability.Public);
                        Response.Cache.SetExpires(DateTime.Now.AddMinutes(cacheMinutes));
                        Response.Cache.SetValidUntilExpires(true);
                    }
                }

                if (model == null)
                {
                    //404
                    return View(PuckCache.Path404);
                }
                string templatePath = result[FieldKeys.TemplatePath];
                if (!string.IsNullOrEmpty(dmode))
                {
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
                        HttpContext.Cache.Insert(cacheKey, templatePath, null, Cache.NoAbsoluteExpiration, TimeSpan.FromMinutes(10));
                    }
                }
                return View(templatePath, model);
            }
            catch (Exception ex)
            {
                PuckCache.PuckLog.Log(ex);
                ViewBag.error = ex.Message;
                return View(PuckCache.Path500);
            }
        }

        public string GetDisplayModeId()
        {
            System.Web.HttpContextBase context = this.HttpContext;
            IList<IDisplayMode> modes = DisplayModeProvider.Instance.Modes;
            int length = modes.Count;

            for (var i = 0; i < length; i++)
            {
                if (modes[i].CanHandleContext(context))
                {
                    return modes[i].DisplayModeId;
                }
            }
            return null;            
        }
        internal static bool IsPropertyAllowed(string propertyName, string[] includeProperties, string[] excludeProperties)
        {
            // We allow a property to be bound if its both in the include list AND not in the exclude list.
            // An empty include list implies all properties are allowed.
            // An empty exclude list implies no properties are disallowed.
            bool includeProperty = (includeProperties == null) || (includeProperties.Length == 0) || includeProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
            bool excludeProperty = (excludeProperties != null) && excludeProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
            return includeProperty && !excludeProperty;
        }

        protected internal bool TryUpdateModelDynamic<TModel>(TModel model) where TModel : class
        {
            return TryUpdateModelDynamic(model, null, null, null, ValueProvider);
        }

        protected internal bool TryUpdateModelDynamic<TModel>(TModel model, string prefix) where TModel : class
        {
            return TryUpdateModelDynamic(model, prefix, null, null, ValueProvider);
        }

        protected internal bool TryUpdateModelDynamic<TModel>(TModel model, string[] includeProperties) where TModel : class
        {
            return TryUpdateModelDynamic(model, null, includeProperties, null, ValueProvider);
        }

        protected internal bool TryUpdateModelDynamic<TModel>(TModel model, string prefix, string[] includeProperties) where TModel : class
        {
            return TryUpdateModelDynamic(model, prefix, includeProperties, null, ValueProvider);
        }

        protected internal bool TryUpdateModelDynamic<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties) where TModel : class
        {
            return TryUpdateModelDynamic(model, prefix, includeProperties, excludeProperties, ValueProvider);
        }

        protected internal bool TryUpdateModelDynamic<TModel>(TModel model, IValueProvider valueProvider) where TModel : class
        {
            return TryUpdateModelDynamic(model, null, null, null, valueProvider);
        }

        protected internal bool TryUpdateModelDynamic<TModel>(TModel model, string prefix, IValueProvider valueProvider) where TModel : class
        {
            return TryUpdateModelDynamic(model, prefix, null, null, valueProvider);
        }

        protected internal bool TryUpdateModelDynamic<TModel>(TModel model, string[] includeProperties, IValueProvider valueProvider) where TModel : class
        {
            return TryUpdateModelDynamic(model, null, includeProperties, null, valueProvider);
        }

        protected internal bool TryUpdateModelDynamic<TModel>(TModel model, string prefix, string[] includeProperties, IValueProvider valueProvider) where TModel : class
        {
            return TryUpdateModelDynamic(model, prefix, includeProperties, null, valueProvider);
        }

        protected internal bool TryUpdateModelDynamic<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties, IValueProvider valueProvider) where TModel : class
        {
            if (model == null)
            {
                throw new ArgumentNullException("model");
            }
            if (valueProvider == null)
            {
                throw new ArgumentNullException("valueProvider");
            }

            Predicate<string> propertyFilter = propertyName => IsPropertyAllowed(propertyName, includeProperties, excludeProperties);
            IModelBinder binder = Binders.GetBinder(model.GetType());

            ModelBindingContext bindingContext = new ModelBindingContext()
            {
                ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(() => model, model.GetType()),
                ModelName = prefix,
                ModelState = ModelState,
                PropertyFilter = propertyFilter,
                ValueProvider = valueProvider
            };
            binder.BindModel(ControllerContext, bindingContext);
            return ModelState.IsValid;
        }


        protected internal void UpdateModelDynamic<TModel>(TModel model) where TModel : class
        {
            UpdateModelDynamic(model, null, null, null, ValueProvider);
        }

        protected internal void UpdateModelDynamic<TModel>(TModel model, string prefix) where TModel : class
        {
            UpdateModelDynamic(model, prefix, null, null, ValueProvider);
        }

        protected internal void UpdateModelDynamic<TModel>(TModel model, string[] includeProperties) where TModel : class
        {
            UpdateModelDynamic(model, null, includeProperties, null, ValueProvider);
        }

        protected internal void UpdateModelDynamic<TModel>(TModel model, string prefix, string[] includeProperties) where TModel : class
        {
            UpdateModelDynamic(model, prefix, includeProperties, null, ValueProvider);
        }

        protected internal void UpdateModelDynamic<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties) where TModel : class
        {
            UpdateModelDynamic(model, prefix, includeProperties, excludeProperties, ValueProvider);
        }

        protected internal void UpdateModelDynamic<TModel>(TModel model, IValueProvider valueProvider) where TModel : class
        {
            UpdateModelDynamic(model, null, null, null, valueProvider);
        }

        protected internal void UpdateModelDynamic<TModel>(TModel model, string prefix, IValueProvider valueProvider) where TModel : class
        {
            UpdateModelDynamic(model, prefix, null, null, valueProvider);
        }

        protected internal void UpdateModelDynamic<TModel>(TModel model, string[] includeProperties, IValueProvider valueProvider) where TModel : class
        {
            UpdateModelDynamic(model, null, includeProperties, null, valueProvider);
        }

        protected internal void UpdateModelDynamic<TModel>(TModel model, string prefix, string[] includeProperties, IValueProvider valueProvider) where TModel : class
        {
            UpdateModelDynamic(model, prefix, includeProperties, null, valueProvider);
        }

        protected internal void UpdateModelDynamic<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties, IValueProvider valueProvider) where TModel : class
        {
            bool success = TryUpdateModelDynamic(model, prefix, includeProperties, excludeProperties, valueProvider);
            if (!success)
            {
                string message = String.Format("The model of type '{0}' could not be updated.", model.GetType().FullName);
                throw new InvalidOperationException(message);
            }
        }
    }
}
