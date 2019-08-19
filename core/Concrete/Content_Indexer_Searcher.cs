﻿using System;
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
using Spatial4n.Core.Context;
using Lucene.Net.Spatial.Vector;
using Lucene.Net.Spatial.Queries;
using puck.core.PuckLucene;
using StackExchange.Profiling;
using System.Threading.Tasks;
using System.Web.Hosting;
using puck.core.State;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Util;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Spatial4n.Core.Shapes;
using System.Globalization;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Prefix;

namespace puck.core.Concrete
{
    public class Content_Indexer_Searcher : I_Content_Indexer,I_Content_Searcher
    {
        private StandardAnalyzer StandardAnalyzer = new Lucene.Net.Analysis.Standard.StandardAnalyzer(LuceneVersion.LUCENE_48);
        private KeywordAnalyzer KeywordAnalyzer = new KeywordAnalyzer();
        public readonly SpatialContext ctx = SpatialContext.GEO;
        private string INDEXPATH { get { return HostingEnvironment.MapPath("~/App_Data/Lucene"); } }
        private string[] NoToken = new string[] { FieldKeys.ID.ToString(), FieldKeys.Path.ToString() };
        private IndexSearcher Searcher = null;
        private IndexWriter Writer = null;
        private Object write_lock = new Object();
        private I_Log logger;
        
        public Content_Indexer_Searcher(I_Log Logger) {
            this.logger = Logger;
            Ini();

            BeforeIndex+=new EventHandler<BeforeIndexingEventArgs>(DelegateBeforeIndexing);
            AfterIndex += new EventHandler<IndexingEventArgs>(DelegateAfterIndexing);
            BeforeDelete += new EventHandler<BeforeIndexingEventArgs>(DelegateBeforeDelete);
            AfterDelete += new EventHandler<IndexingEventArgs>(DelegateAfterDelete);
        }
        private static void DelegateBeforeEvent(Dictionary<string,Tuple<Type, Action<object, BeforeIndexingEventArgs>, bool>> list,object n,BeforeIndexingEventArgs e){
            var type = e.Node.GetType();
            //refactor:can probably use is operator to implement event propagation
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
            var type = e.Node.GetType();
            //refactor:can probably use is operator to implement event propagation
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

        public void UnRegisterBeforeIndexHandler(string Name)
        {
            BeforeIndexActionList.Remove(Name);
        }
        public void UnRegisterAfterIndexHandler(string Name)
        {
            AfterIndexActionList.Remove(Name);
        }
        public void UnRegisterBeforeDeleteHandler(string Name)
        {
            BeforeDeleteActionList.Remove(Name);
        }
        public void UnRegisterAfterDeleteHandler(string Name)
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
                        var nf = new Int32Field(p.Key.ToLower(), int.Parse(p.Value.ToString()), p.FieldStoreSetting);
                        doc.Add(nf);
                    }
                    else if (p.Value is long)
                    {
                        var nf = new Int64Field(p.Key.ToLower(), long.Parse(p.Value.ToString()), p.FieldStoreSetting);
                        doc.Add(nf);
                    }
                    else if (p.Value is float)
                    {
                        var nf = new SingleField(p.Key.ToLower(), float.Parse(p.Value.ToString()), p.FieldStoreSetting);
                        doc.Add(nf);
                    }
                    else if (p.Value is double)
                    {
                        var nf = new DoubleField(p.Key.ToLower(), double.Parse(p.Value.ToString()), p.FieldStoreSetting);
                        doc.Add(nf);
                    }
                    else if (p.Spatial) {
                        if (p.Value == null || string.IsNullOrEmpty(p.Value.ToString()))
                            continue;
                        var name = p.Key.IndexOf('.')>-1?p.Key.Substring(0,p.Key.LastIndexOf('.')):p.Key;
                        int maxLevels = 11;
                        SpatialPrefixTree grid = new GeohashPrefixTree(ctx, maxLevels);
                        var strat = new RecursivePrefixTreeStrategy(grid, name);

                        //var strat = new PointVectorStrategy(ctx,name);
                        var yx = p.Value.ToString().Split(new char[] { ','},StringSplitOptions.RemoveEmptyEntries).Select(x=>double.Parse(x)).ToList();
                        var point = ctx.MakePoint(yx[1],yx[0]);
                        //var point = ctx.ReadShape(p.Value.ToString());
                        var fields = strat.CreateIndexableFields(point);
                        fields.ToList().ForEach(x => doc.Add(x));
                        
                        IPoint pt = (IPoint)point;
                        //doc.Add(new StoredField(strat.FieldName, pt.X.ToString(CultureInfo.InvariantCulture) + " " + pt.Y.ToString(CultureInfo.InvariantCulture)));
                        
                    }
                    else
                    {
                        string value = p.Value == null ? null : (p.KeepValueCasing ? p.Value.ToString() : p.Value.ToString().ToLower());
                        Field f=null;
                        if (p.FieldIndexSetting == Field.Index.ANALYZED || p.FieldIndexSetting == Field.Index.ANALYZED_NO_NORMS)
                            f = new TextField(p.Key, value ?? string.Empty, p.FieldStoreSetting);
                        else
                            f = new StringField(p.Key, value ?? string.Empty, p.FieldStoreSetting);
                        doc.Add(f);
                    }
                }
            }
        }
        //mass index changes in transactional way, like changing paths for related nodes
        public void Index<T>(List<T> models,bool triggerEvents=true) where T:BaseModel {
            if (models.Count == 0) return;
            lock (write_lock)
            {
                var cancelled = new List<BaseModel>();
                var count = 1;
                try
                {
                    SetWriter(false);
                    //Writer.Flush(true, true, true);
                    Parallel.ForEach(models, (m,state,index) => {
                        PuckCache.IndexingStatus = $"indexing item {count} of {models.Count}";
                        var type = ApiHelper.GetType(m.Type);
                        if (type == null)
                            type = typeof(BaseModel);
                        var analyzer = PuckCache.AnalyzerForModel[type];
                        var parser = new PuckQueryParser<T>(Lucene.Net.Util.LuceneVersion.LUCENE_48, FieldKeys.PuckDefaultField, analyzer);
                        if (triggerEvents)
                        {
                            var args = new BeforeIndexingEventArgs() { Node = m, Cancel = false };
                            OnBeforeIndex(this, args);
                            if (args.Cancel)
                            {
                                cancelled.Add(m);
                                return;
                            }
                        }
                        //delete doc
                        string removeQuery = "+" + FieldKeys.ID + ":" + m.Id.ToString() + " +" + FieldKeys.Variant + ":" + m.Variant.ToLower();
                        var q = parser.Parse(removeQuery);
                        Writer.DeleteDocuments(q);

                        Document doc = new Document();
                        //get fields to index
                        List<FlattenedObject> props = null;
                        using (MiniProfiler.Current.CustomTiming("get properties", ""))
                        {
                            props = ObjectDumper.Write(m, int.MaxValue);
                        }
                        using (MiniProfiler.Current.CustomTiming("add fields to doc", ""))
                        {
                            GetFieldSettings(props, doc, null);
                        }//add cms properties
                        string jsonDoc = JsonConvert.SerializeObject(m);
                        //doc in json form for deserialization later
                        doc.Add(new StringField(FieldKeys.PuckValue, jsonDoc, Field.Store.YES));
                        using (MiniProfiler.Current.CustomTiming("add document", ""))
                        {
                            Writer.AddDocument(doc, analyzer);
                        }
                        count++;
                    });
                    
                    //Writer.Flush(true,true,true);
                    using (MiniProfiler.Current.CustomTiming("commit", ""))
                    {
                        Writer.Commit();
                    }
                    //Optimize();
                    
                }
                catch (Exception ex)
                {
                    throw;
                    //logger.Log(ex);
                }
                finally
                {
                    CloseWriter();
                    SetSearcher();
                    if (triggerEvents)
                    {
                        models
                            .Where(x => !cancelled.Contains(x))
                            .ToList()
                            .ForEach(x => { OnAfterIndex(this, new IndexingEventArgs() { Node = x }); });
                    }
                }
            }
        }
        public void Delete<T>(List<T> toDelete) where T:BaseModel
        {
            lock (write_lock)
            {
                try
                {
                    var analyzer = PuckCache.AnalyzerForModel[typeof(T)];
                    var parser = new PuckQueryParser<T>(Lucene.Net.Util.LuceneVersion.LUCENE_48, FieldKeys.PuckDefaultField, analyzer);
                    SetWriter(false);
                    Writer.Flush(true, true);
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
                    Writer.Flush(true, true);
                    Writer.Commit();
                    toDelete
                        .Where(x => !cancelled.Contains(x))
                        .ToList()
                        .ForEach(x => { OnAfterDelete(this, new IndexingEventArgs() { Node = x }); });
                }
                catch (Exception ex)
                {
                    throw;
                    //logger.Log(ex);
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
                            field = new StringField(nv.Key.ToLower(),nv.Value, Field.Store.YES);
                        }
                        else {
                            field = new TextField(nv.Key.ToLower(), nv.Value, Field.Store.YES);
                        }
                        doc.Add(field);
                    }
                    Writer.AddDocument(doc);
                    Writer.Commit();
                    Optimize();
                }
                catch (Exception ex)
                {
                    throw;
                    //logger.Log(ex);
                }
                finally {
                    CloseWriter();
                    SetSearcher();
                }
            }
        }

        public void Delete(string terms,bool reloadSearcher=true)
        {
            lock (write_lock)
            {
                try
                {
                    var parser = new QueryParser(Lucene.Net.Util.LuceneVersion.LUCENE_48, "text", StandardAnalyzer);
                    var contentQuery = parser.Parse(terms);
                    SetWriter(false);
                    Writer.DeleteDocuments(contentQuery);
                    Writer.Commit();
                }
                catch (Exception ex)
                {
                    throw;
                    //logger.Log(ex);
                }
                finally
                {
                    CloseWriter();
                    if(reloadSearcher)
                        SetSearcher();
                }
            }
        }
        //optimize seems to be dropped in lucene 4.8
        public void Optimize() {
            throw new NotImplementedException();
            lock (write_lock)
            {
                try
                {
                    SetWriter(false);
                    //Writer.Optimize();
                }
                catch (OutOfMemoryException ex) {
                    CloseWriter();
                    throw;
                }
                catch (Exception ex)
                {
                    throw;
                    //logger.Log(ex);
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

            bool create = !DirectoryReader.IndexExists(FSDirectory.Open(INDEXPATH));
            
            lock (write_lock)
            {
                try
                {
                    SetWriter(create);
                    //Writer.Optimize();
                }
                catch (Exception ex)
                {
                    throw;
                    //logger.Log(ex);
                }
                finally {
                    CloseWriter();
                }
            }
            SetSearcher();
        }
        public void SetWriter(bool create) {
            if(Writer==null)
                Writer = new IndexWriter(FSDirectory.Open(INDEXPATH),new IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48,StandardAnalyzer));
        }
        public void CloseWriter() {
            Writer.Dispose(false);
            Writer = null;
        }
        public void SetSearcher() {
            var oldSearcher = Searcher;
            var indexReader = DirectoryReader.Open(FSDirectory.Open(INDEXPATH));
            Searcher = new Lucene.Net.Search.IndexSearcher(indexReader);
            //kill old searcher
            if (oldSearcher != null)
            {
                try
                {
                    oldSearcher.IndexReader.Dispose();
                }
                catch (Exception ex)
                {
                    throw;
                    //logger.Log(ex);
                }
            }
            oldSearcher = null;
        }
        public IList<Dictionary<string, string>> Query(Query contentQuery)
        {
            var hits = Searcher.Search(contentQuery, 10).ScoreDocs;

            var result = new List<Dictionary<string, string>>();
            for (var i = 0; i < hits.Count(); i++)
            {
                var doc = Searcher.Doc(hits[i].Doc);
                var d = new Dictionary<string, string>();
                d.Add(FieldKeys.ID, doc.GetValues(FieldKeys.ID).FirstOrDefault() ?? "");
                d.Add(FieldKeys.PuckType, doc.GetValues(FieldKeys.PuckType).FirstOrDefault() ?? "");
                d.Add(FieldKeys.PuckValue, doc.GetValues(FieldKeys.PuckValue).FirstOrDefault() ?? "");
                d.Add(FieldKeys.Path, doc.GetValues(FieldKeys.Path).FirstOrDefault() ?? "");
                d.Add(FieldKeys.Variant, doc.GetValues(FieldKeys.Variant).FirstOrDefault() ?? "");
                d.Add(FieldKeys.TemplatePath, doc.GetValues(FieldKeys.TemplatePath).FirstOrDefault() ?? "");
                d.Add(FieldKeys.Score, hits[i].Score.ToString());
                result.Add(d);
            }
            return result;            
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
                var type = ApiHelper.GetType(typeName);
                var analyzer = PuckCache.AnalyzerForModel[type];
                parser = new QueryParser(Lucene.Net.Util.LuceneVersion.LUCENE_48, FieldKeys.PuckDefaultField, analyzer);
            }
            else {
                parser = new QueryParser(LuceneVersion.LUCENE_48, "text", KeywordAnalyzer);
            }

            var contentQuery = parser.Parse(terms);
            return Query(contentQuery);            
        }
        public IList<T> QueryNoCast<T>(string qstr) where T:BaseModel
        {
            int total;
            return QueryNoCast<T>(qstr,null,null,out total);
        }
        public IList<T> QueryNoCast<T>(string qstr, Filter filter,Sort sort,out int total,int limit=500,int skip=0) where T:BaseModel
        {
            var analyzer = PuckCache.AnalyzerForModel[typeof(T)];
            var parser = new PuckQueryParser<T>(LuceneVersion.LUCENE_48, FieldKeys.PuckDefaultField, analyzer);
            var q = parser.Parse(qstr);
            TopDocs docs;
            if (sort == null)
                docs = Searcher.Search(q, filter, limit);
            else
            {
                sort = sort.Rewrite(Searcher);
                docs = Searcher.Search(q, filter, limit, sort);
            }
            total = docs.TotalHits;
            var results = new List<T>();
            for (var i = 0; i < docs.ScoreDocs.Count(); i++)
            {
                if (!(i >= skip))
                    continue;
                var doc = Searcher.Doc(docs.ScoreDocs[i].Doc);
                var type = ApiHelper.GetType(doc.GetValues(FieldKeys.PuckType).FirstOrDefault());
                T result = (T)JsonConvert.DeserializeObject(doc.GetValues(FieldKeys.PuckValue)[0],type);
                results.Add(result);
            }
            return results;
        }
        public IList<T> Query<T>(string qstr) where T:BaseModel
        {
            int total;
            return Query<T>(qstr,null,null,out total);
        }
        public int Count<T>(string qstr) where T : BaseModel
        {
            int total;
            var result = Query<T>(qstr, null, null, out total, limit: 1);
            return total;
        }
        
        public IList<T> Query<T>(string qstr,Filter filter,Sort sort,out int total,int limit=500,int skip=0) where T:BaseModel {
            var analyzer = PuckCache.AnalyzerForModel[typeof(T)];
            var parser = new PuckQueryParser<T>(LuceneVersion.LUCENE_48,FieldKeys.PuckDefaultField,analyzer);
            var q = parser.Parse(qstr);
            TopDocs docs;
            if (sort == null)
                docs = Searcher.Search(q, filter, limit);
            else
            {
                sort = sort.Rewrite(Searcher);
                docs = Searcher.Search(q, filter, limit, sort);
            }
            total = docs.TotalHits;
            var results = new List<T>();
            for (var i = 0; i < docs.ScoreDocs.Count(); i++) {
                if (!(i >= skip))
                    continue;
                var doc = Searcher.Doc(docs.ScoreDocs[i].Doc);
                T result = JsonConvert.DeserializeObject<T>(doc.GetValues(FieldKeys.PuckValue)[0]);
                results.Add(result);
            }
            return results;
        }
        public IList<T> Get<T>()
        {
            return Get<T>(int.MaxValue);
        }
        public IList<T> Get<T>(int limit)
        {
            var t = new Term(FieldKeys.PuckTypeChain,typeof(T).FullName);
            var q = new TermQuery(t);
            var hits=Searcher.Search(q,limit).ScoreDocs;
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
