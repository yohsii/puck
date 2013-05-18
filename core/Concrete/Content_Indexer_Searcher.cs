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

namespace puck.core.Concrete
{
    public class Content_Indexer_Searcher : I_Content_Indexer,I_Content_Searcher
    {
        private Lucene.Net.Analysis.Standard.StandardAnalyzer StandardAnalyzer = new Lucene.Net.Analysis.Standard.StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
        private Lucene.Net.Analysis.Snowball.SnowballAnalyzer SnowballAnalyzer = new Lucene.Net.Analysis.Snowball.SnowballAnalyzer(Lucene.Net.Util.Version.LUCENE_30,"English");
        private string INDEXPATH { get { return HttpContext.Current.Server.MapPath("~/App_Data/Lucene"); } }
        private string[] NoToken = new string[] { FieldKeys.ID.ToString(), FieldKeys.Path.ToString() };
        private IndexSearcher Searcher = null;
        private Object write_lock = new Object();
        private I_Log logger;
        
        public Content_Indexer_Searcher(I_Log Logger/*,I_BQ_Youtube_Repository repo*/) {
            this.logger = Logger;
            
        }

        public void Index<T>(T model) {
            lock (write_lock)
            {
                IndexWriter writer = null;
                try
                {
                    //get model properties
                    var props = ObjectDumper.Write(model, int.MaxValue);
                    //delete current doc
                    var id = props.Where(x => x.Key.Equals(FieldKeys.ID)).FirstOrDefault().Value;

                    var analyzers = new List<KeyValuePair<string, Analyzer>>();
                    Document doc = new Document();
                    //get fields to index
                    foreach (var p in props)
                    {
                        if (p.Analyzer != null) {
                            analyzers.Add(new KeyValuePair<string,Analyzer>(p.Key,p.Analyzer));
                        }
                        if (p.Value is int)
                        {
                            var nf = new NumericField(p.Key.ToLower(), 4, p.FieldStoreSetting, true);
                            nf.SetIntValue(int.Parse(p.Value.ToString()));
                            doc.Add(nf);
                        }
                        else if (p.Value is long)
                        {
                            var nf = new NumericField(p.Key.ToLower(), 4,p.FieldStoreSetting, true);
                            nf.SetLongValue(long.Parse(p.Value.ToString()));
                            doc.Add(nf);
                        }
                        else if (p.Value is float)
                        {
                            var nf = new NumericField(p.Key.ToLower(), 4,p.FieldStoreSetting, true);
                            nf.SetFloatValue(float.Parse(p.Value.ToString()));
                            doc.Add(nf);
                        }
                        else if (p.Value is double)
                        {
                            var nf = new NumericField(p.Key.ToLower(), 4,p.FieldStoreSetting, true);
                            nf.SetDoubleValue(double.Parse(p.Value.ToString()));
                            doc.Add(nf);
                        }
                        else
                        {
                            var f = new Field(p.Key,p.Value==null?string.Empty:p.Value.ToString(),p.FieldStoreSetting,p.FieldIndexSetting);
                            doc.Add(f);
                        }
                    }
                    //add cms properties
                    string jsonDoc = JsonConvert.SerializeObject(model);
                    //doc in json form for deserialization later
                    doc.Add(new Field(FieldKeys.PuckValue,jsonDoc,Field.Store.YES,Field.Index.NOT_ANALYZED));
                    //full typename for deserialization later
                    doc.Add(new Field(FieldKeys.PuckType,model.GetType().FullName,Field.Store.YES,Field.Index.NOT_ANALYZED));
                    
                    var analyzer = new PerFieldAnalyzerWrapper(StandardAnalyzer,analyzers);
                    writer = new IndexWriter(FSDirectory.Open(INDEXPATH), analyzer, false, IndexWriter.MaxFieldLength.UNLIMITED);
                    writer.DeleteDocuments(new Term(FieldKeys.ID, id.ToString()));
                    writer.AddDocument(doc);
                    writer.Flush(true, true, true);
                }
                catch (Exception ex)
                {
                    logger.Log(ex);
                }
                finally
                {
                    writer.Dispose(true);
                    writer = null;
                    SetSearcher();
                }
            }

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
                IndexWriter writer = null;
                try
                {
                    writer = new IndexWriter(FSDirectory.Open(INDEXPATH), Analyzer, false,IndexWriter.MaxFieldLength.UNLIMITED);
                    var id = values.Where(x => x.Key.Equals(FieldKeys.ID)).FirstOrDefault().Value;
                    writer.DeleteDocuments(new Term(FieldKeys.ID, id));
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
                    writer.AddDocument(doc);
                    writer.Flush(true,true,true);                    
                }
                catch (Exception ex)
                {
                    logger.Log(ex);
                }
                finally {
                    writer.Dispose(true);
                    writer = null;
                    SetSearcher();
                }
            }
        }

        public void Delete(string id)
        {
            lock (write_lock)
            {
                IndexWriter writer = null;
                try
                {
                    writer = new IndexWriter(FSDirectory.Open(INDEXPATH), Analyzer, false,IndexWriter.MaxFieldLength.UNLIMITED);
                    writer.DeleteDocuments(new Term(FieldKeys.ID, id));
                    writer.Flush(true,true,true);                    
                }
                catch (Exception ex)
                {
                    logger.Log(ex);
                }
                finally
                {
                    writer.Dispose(true);
                    writer = null;
                    SetSearcher();
                }
            }
        }

        public void Optimize() {
            lock (write_lock)
            {
                IndexWriter writer = null;
                try
                {
                    writer = new IndexWriter(FSDirectory.Open(INDEXPATH), Analyzer, false,IndexWriter.MaxFieldLength.UNLIMITED);
                    writer.Optimize();                    
                }
                catch (Exception ex)
                {
                    logger.Log(ex);
                }
                finally
                {
                    writer.Dispose(true);
                    writer = null;
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
                IndexWriter writer = null;
                try
                {
                    writer = new IndexWriter(FSDirectory.Open(INDEXPATH), Analyzer, create, IndexWriter.MaxFieldLength.UNLIMITED);
                    writer.Optimize();
                    writer.Flush(true, true, true);
                    writer.Dispose(true);
                    writer = null;

                }
                catch (Exception ex)
                {
                    logger.Log(ex);
                }
                finally { }
            }
            SetSearcher();
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
        
        public IDictionary<string, string> Query(string terms)
        {
            BooleanQuery bq = new BooleanQuery();
            Lucene.Net.QueryParsers.QueryParser snowParser = new Lucene.Net.QueryParsers.QueryParser(Lucene.Net.Util.Version.LUCENE_30,"text",Analyzer);
            Lucene.Net.Search.Query contentQuery = snowParser.Parse(terms);
            bq.Add(contentQuery,Occur.MUST);
            var collector = TopScoreDocCollector.Create(int.MaxValue,true);
            Searcher.Search(bq,collector);
            var hits=collector.TopDocs().ScoreDocs;
            
            var result = new Dictionary<string, string>();
            for(var i=0;i<hits.Count();i++){
                var doc = Searcher.Doc(i);
                result.Add(
                    doc.GetValues(FieldKeys.ID).First(),
                    doc.GetValues(FieldKeys.PuckValue).First()
                );
            }
            return result;            
        }
        public IList<T> Query<T>(string qstr) {
            var parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30,FieldKeys.PuckDefaultField,Analyzer);
            var q = parser.Parse(qstr);
            var coll = TopScoreDocCollector.Create(int.MaxValue,true);
            Searcher.Search(q, coll);
            var hits = coll.TopDocs().ScoreDocs;
            var results = new List<T>();
            for (var i = 0; i < hits.Count(); i++) {
                var doc = Searcher.Doc(i);
                T result = JsonConvert.DeserializeObject<T>(doc.GetValues(FieldKeys.PuckValue)[0]);
                results.Add(result);
            }
            return results;
        }
        public IList<T> Get<T>()
        {
            var t = new Term(FieldKeys.PuckType,typeof(T).FullName);
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
