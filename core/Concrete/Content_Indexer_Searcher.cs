using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using System.Web;
using Lucene.Net.Documents;
using System.IO;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;
using puck.core.Abstract;
using Lucene.Net.Store;
using puck.core.Constants;
using puck.core.Helpers;
using Newtonsoft.Json;
using Lucene.Net.Analysis;
using puck.core.Base;
using puck.core.Events;

namespace puck.core.Concrete
{
    public class Content_Indexer_Searcher : I_Content_Indexer,I_Content_Searcher
    {
        private Lucene.Net.Analysis.Standard.StandardAnalyzer StandardAnalyzer = new Lucene.Net.Analysis.Standard.StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
        private Lucene.Net.Analysis.KeywordAnalyzer KeywordAnalyzer = new KeywordAnalyzer();
        private string INDEXPATH { get { return HttpContext.Current.Server.MapPath("~/App_Data/Lucene"); } }
        private string[] NoToken = new string[] { FieldKeys.ID.ToString(), FieldKeys.Path.ToString() };
        private IndexSearcher Searcher = null;
        private IndexWriter Writer = null;
        private Object write_lock = new Object();
        private I_Log logger;
        
        public Content_Indexer_Searcher(I_Log Logger/*,I_BQ_Youtube_Repository repo*/) {
            this.logger = Logger;
            Ini();

            BeforeIndex+=new EventHandler<BeforeIndexingEventArgs>(DelegateBeforeIndexing);
            AfterIndex += new EventHandler<IndexingEventArgs>(DelegateAfterIndexing);
            BeforeDelete += new EventHandler<BeforeIndexingEventArgs>(DelegateBeforeDelete);
            AfterDelete += new EventHandler<IndexingEventArgs>(DelegateAfterDelete);
        }
        private static void DelegateBeforeEvent(Dictionary<string,Tuple<Type, Action<object, BeforeIndexingEventArgs>, bool>> list,object n,BeforeIndexingEventArgs e){
            var type = n.GetType();
            var types = ApiHelper.BaseTypes(type);
            types.Add(type);
            list.Where(x => x.Value.Item1 == type || (x.Value.Item3 && types.Contains(x.Value.Item1)))
                .ToList().ForEach(x =>
                {
                    x.Value.Item2(n, e);
                });
        }
        private static void DelegateAfterEvent(Dictionary<string, Tuple<Type, Action<object, IndexingEventArgs>, bool>> list, object n, IndexingEventArgs e)
        {
            var type = n.GetType();
            var types = ApiHelper.BaseTypes(type);
            types.Add(type);
            list.Where(x => x.Value.Item1 == type || (x.Value.Item3 && types.Contains(x.Value.Item1)))
                .ToList().ForEach(x =>
                {
                    x.Value.Item2(n, e);
                });
        }
        private static void DelegateBeforeIndexing(object n , BeforeIndexingEventArgs e){
            DelegateBeforeEvent(BeforeIndexActionList, n, e);
        }
        private static void DelegateAfterIndexing(object n, IndexingEventArgs e)
        {
            DelegateAfterEvent(AfterIndexActionList, n, e);
        }
        private static void DelegateBeforeDelete(object n, BeforeIndexingEventArgs e)
        {
            DelegateBeforeEvent(BeforeDeleteActionList, n, e);
        }
        private static void DelegateAfterDelete(object n, IndexingEventArgs e)
        {
            DelegateAfterEvent(AfterIndexActionList, n, e);
        }

        public static Dictionary<string,Tuple<Type, Action<object, BeforeIndexingEventArgs>, bool>> BeforeIndexActionList = 
            new Dictionary<string,Tuple<Type, Action<object, BeforeIndexingEventArgs>, bool>>();
        
        public static Dictionary<string,Tuple<Type, Action<object, IndexingEventArgs>, bool>> AfterIndexActionList =
            new Dictionary<string,Tuple<Type, Action<object, IndexingEventArgs>, bool>>();
        
        public static Dictionary<string,Tuple<Type, Action<object, BeforeIndexingEventArgs>, bool>> BeforeDeleteActionList =
            new Dictionary<string,Tuple<Type, Action<object, BeforeIndexingEventArgs>, bool>>();
        
        public static Dictionary<string,Tuple<Type, Action<object, IndexingEventArgs>, bool>> AfterDeleteActionList =
            new Dictionary<string,Tuple<Type, Action<object, IndexingEventArgs>, bool>>();
        
        public event EventHandler<BeforeIndexingEventArgs> BeforeIndex;
        public event EventHandler<IndexingEventArgs> AfterIndex;
        public event EventHandler<BeforeIndexingEventArgs> BeforeDelete;
        public event EventHandler<IndexingEventArgs> AfterDelete;
        
        public void RegisterBeforeIndexHandler<T>(string Name,Action<object,BeforeIndexingEventArgs> Handler,bool Propagate=false) where T:BaseModel {
            BeforeIndexActionList.Add(Name,new Tuple<Type,Action<object,BeforeIndexingEventArgs>,bool>(typeof(T),Handler,Propagate));
        }
        public void RegisterAfterIndexHandler<T>(string Name, Action<object, IndexingEventArgs> Handler, bool Propagate = false) where T : BaseModel
        {
            AfterIndexActionList.Add(Name, new Tuple<Type, Action<object, IndexingEventArgs>, bool>(typeof(T), Handler, Propagate));
        }
        public void RegisterBeforeDeleteHandler<T>(string Name, Action<object, BeforeIndexingEventArgs> Handler, bool Propagate = false) where T : BaseModel
        {
            BeforeDeleteActionList.Add(Name, new Tuple<Type, Action<object, BeforeIndexingEventArgs>, bool>(typeof(T), Handler, Propagate));
        }
        public void RegisterAfterDeleteHandler<T>(string Name, Action<object, IndexingEventArgs> Handler, bool Propagate = false) where T : BaseModel
        {
            AfterDeleteActionList.Add(Name, new Tuple<Type, Action<object, IndexingEventArgs>, bool>(typeof(T), Handler, Propagate));
        }

        public void UnRegisterBeforeIndexHandler<T>(string Name)
        {
            BeforeIndexActionList.Remove(Name);
        }
        public void UnRegisterAfterIndexHandler<T>(string Name)
        {
            AfterIndexActionList.Remove(Name);
        }
        public void UnRegisterBeforeDeleteHandler<T>(string Name)
        {
            BeforeDeleteActionList.Remove(Name);
        }
        public void UnRegisterAfterDeleteHandler<T>(string Name)
        {
            AfterDeleteActionList.Remove(Name);
        }

        protected void OnBeforeIndex(object s,BeforeIndexingEventArgs args) {
            if (BeforeIndex != null)
                BeforeIndex(s,args);
        }

        protected void OnAfterIndex(object s, IndexingEventArgs args)
        {
            if (AfterIndex != null)
                AfterIndex(s, args);
        }

        protected void OnBeforeDelete(object s, BeforeIndexingEventArgs args)
        {
            if (BeforeDelete != null)
                BeforeDelete(s, args);
        }

        protected void OnAfterDelete(object s, IndexingEventArgs args)
        {
            if (AfterDelete != null)
                AfterDelete(s, args);
        }

        public void GetFieldSettings(List<FlattenedObject> props,Document doc,List<KeyValuePair<string,Analyzer>> analyzers) {
            foreach (var p in props)
            {
                if (analyzers != null)
                {
                    if (p.Analyzer != null)
                    {
                        analyzers.Add(new KeyValuePair<string, Analyzer>(p.Key, p.Analyzer));
                    }
                }
                if (doc != null)
                {
                    if (p.Value is int)
                    {
                        var nf = new NumericField(p.Key.ToLower(), 4, p.FieldStoreSetting, true);
                        nf.SetIntValue(int.Parse(p.Value.ToString()));
                        doc.Add(nf);
                    }
                    else if (p.Value is long)
                    {
                        var nf = new NumericField(p.Key.ToLower(), 4, p.FieldStoreSetting, true);
                        nf.SetLongValue(long.Parse(p.Value.ToString()));
                        doc.Add(nf);
                    }
                    else if (p.Value is float)
                    {
                        var nf = new NumericField(p.Key.ToLower(), 4, p.FieldStoreSetting, true);
                        nf.SetFloatValue(float.Parse(p.Value.ToString()));
                        doc.Add(nf);
                    }
                    else if (p.Value is double)
                    {
                        var nf = new NumericField(p.Key.ToLower(), 4, p.FieldStoreSetting, true);
                        nf.SetDoubleValue(double.Parse(p.Value.ToString()));
                        doc.Add(nf);
                    }
                    else
                    {
                        var f = new Field(p.Key, p.Value == null ? string.Empty : p.Value.ToString(), p.FieldStoreSetting, p.FieldIndexSetting);
                        doc.Add(f);
                    }
                }
            }
        }
        //mass index changes in transactional way, like changing paths for related nodes
        public void Index<T>(List<T> models) where T:BaseModel {
            lock (write_lock)
            {
                try
                {
                    var model = Activator.CreateInstance(typeof(T));
                    var props = ObjectDumper.Write(model, int.MaxValue);
                    var analyzers = new List<KeyValuePair<string, Analyzer>>();
                    GetFieldSettings(props, null, analyzers);
                    var analyzer = new PerFieldAnalyzerWrapper(StandardAnalyzer,analyzers);

                    var parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30,FieldKeys.PuckDefaultField,analyzer);
                    SetWriter(false);
                    //by flushing before and after bulk changes from within write lock, we make the changes transactional - all deletes/adds will be successful. or none.
                    Writer.Flush(true, true, true);
                    var cancelled = new List<BaseModel>();
                    foreach (var m in models)
                    {
                        var args= new BeforeIndexingEventArgs() {Node=m,Cancel=false };
                        OnBeforeIndex(this, args);
                        if (args.Cancel)
                        {
                            cancelled.Add(m);
                            continue;
                        }
                        //delete doc
                        string removeQuery = "+"+FieldKeys.ID+":"+m.Id.ToString()+ " +"+FieldKeys.Variant+":"+m.Variant;
                        var q = parser.Parse(removeQuery);
                        Writer.DeleteDocuments(q);                    
                        
                        Document doc = new Document();
                        //get fields to index
                        props = ObjectDumper.Write(m, int.MaxValue);
                        GetFieldSettings(props, doc, null);
                        //add cms properties
                        string jsonDoc = JsonConvert.SerializeObject(m);
                        //doc in json form for deserialization later
                        doc.Add(new Field(FieldKeys.PuckValue, jsonDoc, Field.Store.YES, Field.Index.NOT_ANALYZED));
                        Writer.AddDocument(doc);
                    }
                    Writer.Flush(true,true,true);
                    Writer.Commit();
                    models
                        .Where(x=>!cancelled.Contains(x))
                        .ToList()
                        .ForEach(x => { OnAfterIndex(this, new IndexingEventArgs() {Node=x }); });
                }
                catch (Exception ex)
                {
                    logger.Log(ex);
                }
                finally
                {
                    CloseWriter();
                    SetSearcher();
                }
            }
        }
        public void Delete<T>(List<T> toDelete) where T:BaseModel
        {
            lock (write_lock)
            {
                try
                {
                    var model = Activator.CreateInstance(typeof(T));
                    var props = ObjectDumper.Write(model, int.MaxValue);
                    var analyzers = new List<KeyValuePair<string, Analyzer>>();
                    GetFieldSettings(props, null, analyzers);
                    var analyzer = new PerFieldAnalyzerWrapper(StandardAnalyzer, analyzers);

                    var parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, FieldKeys.PuckDefaultField, analyzer);
                    
                    SetWriter(false);
                    Writer.Flush(true, true, true);
                    var cancelled = new List<BaseModel>();
                    foreach (var m in toDelete)
                    {
                        var args = new BeforeIndexingEventArgs() { Node = m, Cancel = false };
                        OnBeforeDelete(this, args);
                        if (args.Cancel)
                        {
                            cancelled.Add(m);
                            continue;
                        }
                        string removeQuery = "+" + FieldKeys.ID + ":" + m.Id.ToString() + " +" + FieldKeys.Variant + ":" + m.Variant;
                        var q = parser.Parse(removeQuery);
                        Writer.DeleteDocuments(q);
                    }
                    Writer.Flush(true, true, true);
                    Writer.Commit();
                    toDelete
                        .Where(x => !cancelled.Contains(x))
                        .ToList()
                        .ForEach(x => { OnAfterDelete(this, new IndexingEventArgs() { Node = x }); });
                }
                catch (Exception ex)
                {
                    logger.Log(ex);
                }
                finally
                {
                    CloseWriter();
                    SetSearcher();
                }
            }
        }
        public void Delete<T>(T toDelete) where T : BaseModel
        {
            if (toDelete != null)
                Delete<T>(new List<T> { toDelete });
        }
        public void Index<T>(T model)where T:BaseModel {
            if (model != null)
                Index(new List<T> {model });
        }
        
        public void Index(List<Dictionary<string, string>> values)
        {
            foreach (var dict in values)
            {
                this.Index(dict);
            }
        }
        
        public void Index(Dictionary<string, string> values)
        {
            lock (write_lock)
            {
                try
                {
                    SetWriter(false);
                    var id = values.Where(x => x.Key.Equals(FieldKeys.ID)).FirstOrDefault().Value;
                    Writer.DeleteDocuments(new Term(FieldKeys.ID, id));
                    Document doc = new Document();
                    foreach (var nv in values)
                    {
                        Field field;
                        if (NoToken.Contains(nv.Key.ToLower()))
                        {
                            field = new Field(nv.Key.ToLower(),nv.Value, Field.Store.YES, Field.Index.NOT_ANALYZED);
                        }
                        else {
                            field = new Field(nv.Key.ToLower(), nv.Value, Field.Store.YES, Field.Index.ANALYZED);
                        }
                        doc.Add(field);
                    }
                    Writer.AddDocument(doc);
                    Writer.Commit();
                }
                catch (Exception ex)
                {
                    logger.Log(ex);
                }
                finally {
                    CloseWriter();
                    SetSearcher();
                }
            }
        }

        public void Delete(string terms)
        {
            lock (write_lock)
            {
                try
                {
                    var parser = new Lucene.Net.QueryParsers.QueryParser(Lucene.Net.Util.Version.LUCENE_30, "text", StandardAnalyzer);
                    var contentQuery = parser.Parse(terms);
                    SetWriter(false);
                    Writer.DeleteDocuments(contentQuery);
                    Writer.Commit();
                }
                catch (Exception ex)
                {
                    logger.Log(ex);
                }
                finally
                {
                    CloseWriter();
                    SetSearcher();
                }
            }
        }

        public void Optimize() {
            lock (write_lock)
            {
                try
                {
                    SetWriter(false);
                    Writer.Optimize();                    
                }
                catch (Exception ex)
                {
                    logger.Log(ex);
                }
                finally
                {
                    SetSearcher();
                }
            }
        }

        public void Ini()
        {
            if (!System.IO.Directory.Exists(INDEXPATH)) {
                System.IO.Directory.CreateDirectory(INDEXPATH);
            }

            bool create = !IndexReader.IndexExists(FSDirectory.Open(INDEXPATH));
            
            lock (write_lock)
            {
                try
                {
                    SetWriter(create);
                    Writer.Optimize();
                }
                catch (Exception ex)
                {
                    logger.Log(ex);
                }
                finally {
                    CloseWriter();
                }
            }
            SetSearcher();
        }
        public void SetWriter(bool create) {
            if(Writer==null)
                Writer = new IndexWriter(FSDirectory.Open(INDEXPATH), StandardAnalyzer, create, IndexWriter.MaxFieldLength.UNLIMITED);                    
        }
        public void CloseWriter() {
            Writer.Dispose(false);
            Writer = null;
        }
        public void SetSearcher() {
            var oldSearcher = Searcher;
            Searcher = new Lucene.Net.Search.IndexSearcher(FSDirectory.Open(INDEXPATH));
            //kill old searcher
            if (oldSearcher != null)
            {
                try
                {
                    oldSearcher.Dispose();
                }
                catch (Exception ex)
                {
                    logger.Log(ex);
                }
            }
            oldSearcher = null;
        }
        public IList<Dictionary<string, string>> Query(string terms)
        {
            return Query(terms, null);
        }
        public IList<Dictionary<string, string>> Query(string terms,string typeName)
        {
            QueryParser parser;
            if (!string.IsNullOrEmpty(typeName))
            {
                var type = Type.GetType(typeName);
                /*
                var model = Activator.CreateInstance(type);
                var props = ObjectDumper.Write(model, int.MaxValue);
                var analyzers = new List<KeyValuePair<string, Analyzer>>();
                GetFieldSettings(props, null, analyzers);
                var analyzer = new PerFieldAnalyzerWrapper(StandardAnalyzer, analyzers);
                */
                var analyzer = PuckCache.AnalyzerForModel[type];
                parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, FieldKeys.PuckDefaultField, analyzer);
            }
            else {
                parser = new Lucene.Net.QueryParsers.QueryParser(Lucene.Net.Util.Version.LUCENE_30, "text", KeywordAnalyzer);
            }

            var contentQuery = parser.Parse(terms);
            //var collector = TopScoreDocCollector.Create(int.MaxValue,true);
            var hits=Searcher.Search(contentQuery,int.MaxValue).ScoreDocs;
            //var hits=collector.TopDocs().ScoreDocs;
            
            var result = new List<Dictionary<string, string>>();
            for(var i=0;i<hits.Count();i++){
                var doc = Searcher.Doc(hits[i].Doc);
                var d = new Dictionary<string, string>();
                d.Add(FieldKeys.ID,doc.GetValues(FieldKeys.ID).FirstOrDefault()??"");
                d.Add(FieldKeys.PuckType,doc.GetValues(FieldKeys.PuckType).FirstOrDefault()??"");
                d.Add(FieldKeys.PuckValue,doc.GetValues(FieldKeys.PuckValue).FirstOrDefault()??"");
                d.Add(FieldKeys.Path, doc.GetValues(FieldKeys.Path).FirstOrDefault() ?? "");
                d.Add(FieldKeys.Variant, doc.GetValues(FieldKeys.Variant).FirstOrDefault() ?? "");
                d.Add(FieldKeys.TemplatePath, doc.GetValues(FieldKeys.TemplatePath).FirstOrDefault() ?? "");
                result.Add(d);
            }
            return result;            
        }
        public IList<T> Query<T>(string qstr) {
            /*
            var model = Activator.CreateInstance(typeof(T));
            var props = ObjectDumper.Write(model, int.MaxValue);
            var analyzers = new List<KeyValuePair<string, Analyzer>>();
            GetFieldSettings(props, null, analyzers);
            var analyzer = new PerFieldAnalyzerWrapper(StandardAnalyzer,analyzers);
            */
            var analyzer = PuckCache.AnalyzerForModel[typeof(T)];
            var parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30,FieldKeys.PuckDefaultField,analyzer);
            var q = parser.Parse(qstr);
            //var coll = TopScoreDocCollector.Create(int.MaxValue,true);
            //Searcher.Search(q, coll);
            //var hits = coll.TopDocs().ScoreDocs;
            var hits = Searcher.Search(q, int.MaxValue).ScoreDocs;
            var results = new List<T>();
            for (var i = 0; i < hits.Count(); i++) {
                var doc = Searcher.Doc(hits[i].Doc);
                T result = JsonConvert.DeserializeObject<T>(doc.GetValues(FieldKeys.PuckValue)[0]);
                results.Add(result);
            }
            return results;
        }
        public IList<T> Get<T>()
        {
            var t = new Term(FieldKeys.PuckTypeChain,typeof(T).FullName);
            var q = new TermQuery(t);
            var hits=Searcher.Search(q,int.MaxValue).ScoreDocs;
            var results = new List<T>();
            for (var i = 0; i < hits.Count(); i++)
            {
                var doc = Searcher.Doc(hits[i].Doc);
                T result = JsonConvert.DeserializeObject<T>(doc.GetValues(FieldKeys.PuckValue)[0]);
                results.Add(result);
            }
            return results;
        }
        
    }
}
