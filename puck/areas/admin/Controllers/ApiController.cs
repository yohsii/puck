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

        public JsonResult Models()
        {
            return Json(ApiHelper.Models.Select(x=>x.AssemblyQualifiedName),JsonRequestBehavior.AllowGet);
        }

        public ActionResult EditMarkup(string type) {
            var model = Activator.CreateInstance(Type.GetType(type));
            return View(model);
        }

        [HttpPost]
        public JsonResult NewContent(FormCollection fc,string p_type) {
            var targetType = Type.GetType(p_type);
            var model = Activator.CreateInstance(targetType);
            
            try { 
                UpdateModelDynamic(model,fc.ToValueProvider());
                //indexer.Index(model);
            }
            catch (Exception ex) {
                log.Log(ex);
            }
            var q = new QueryHelper<Home>();

            q
                .Field(x => x.Person.Name, "Amanuel")
                .And()
                .Field(x => x.Description, "News home running jumping gardening".Wrap())
                .And(
                    q.New()
                    .Not()
                    .Field(x=>x.PageTitle,"blog")
                    .Or()
                    .Field(x=>x.Person.Age,10)
                );

            var schr = indexer as puck.core.Concrete.Content_Indexer_Searcher;
            var props = ObjectDumper.Write(model,int.MaxValue);
            var analyzers = new List<KeyValuePair<string,Analyzer>>();
            schr.GetFieldSettings(props, null, analyzers);
            var analyzer = new PerFieldAnalyzerWrapper(new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30),analyzers);
            var qp = new Lucene.Net.QueryParsers.QueryParser(Lucene.Net.Util.Version.LUCENE_30, "", analyzer);
            var parsed = qp.Parse(q.ToString());
            
            return Json("");
        }

        
    }
}
