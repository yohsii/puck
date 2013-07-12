using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using puck.core.Abstract;
using System.Text.RegularExpressions;
using System.Linq.Expressions;
using Lucene.Net.QueryParsers;
using puck.core.Constants;
using puck.core.Base;
using System.Web;
using System.Globalization;
using Lucene.Net.Search;
using System.Threading;

namespace puck.core.Helpers
{
    public static class QueryExtensions {
        //term modifier string extensions
        public static string WildCardSingle(this string s, bool perWord = false)
        {
            if (perWord)
                return string.Join(" ", s.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x + "?"));
            else
                return s + "?";
        }

        public static string WildCardMulti(this string s, bool perWord = false)
        {
            if (perWord)
                return string.Join(" ", s.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x + "*"));
            else
                return s + "*";
        }

        public static string Fuzzy(this string s, float? fuzzyness = null, bool perWord = false)
        {
            if (perWord)
                return string.Join(" ", s.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x + "~" + (fuzzyness.HasValue ? fuzzyness.ToString() : "")));
            else
                return s + "~";
        }

        public static string Boost(this string s, float? boost = null, bool perWord = false)
        {
            if (perWord)
                return string.Join(" ", s.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x + "^" + (boost.HasValue ? boost.ToString() : "")));
            else
                return s + "^";
        }

        public static string Proximity(this string s, float? proximity = null)
        {
            return s + "~" + (proximity.HasValue ? proximity.ToString() : "");
        }

        public static string Escape(this string s)
        {
            return QueryParser.Escape(s);
        }

        public static string Wrap(this string s)
        {
            return string.Concat("\"", s, "\"");
        }
        //retrieval extensions
        public static List<T> Parent<T>(this BaseModel n) where T : BaseModel
        {
            var qh = new QueryHelper<T>();
            string path = n.Path.Substring(0, n.Path.LastIndexOf('/'));
            qh
                .And()
                .Field(x => x.Path, path);
            return qh.GetAll();
        }
        public static List<T> Ancestors<T>(this BaseModel n) where T : BaseModel {
            var qh = new QueryHelper<T>();
            string nodePath = n.Path;
            while (nodePath.Count(x => x == '/') > 1)
            {
                nodePath = nodePath.Substring(0, nodePath.LastIndexOf('/'));
                qh
                    .And()
                    .Field(x=>x.Path,nodePath);            
            }
            return qh.GetAll();
        }
        public static List<T> Siblings<T>(this BaseModel n) where T : BaseModel {
            var qh = new QueryHelper<T>();
            qh
                    .And()
                    .Field(x => x.Path, ApiHelper.DirOfPath(n.Path).WildCardMulti())
                    .Not()
                    .Field(x => x.Path, ApiHelper.DirOfPath(n.Path).WildCardMulti() + "/*")
                    .Not()
                    .Field(x => x.Id, n.Id.ToString().Wrap());                   
            return qh.GetAll();                
        }
        public static List<T> Variants<T>(this BaseModel n) where T : BaseModel
        {
            var qh = new QueryHelper<T>();
            qh      
                    .And()
                    .Field(x => x.Id, n.Id.ToString())
                    .Not()
                    .Field(x => x.Variant, n.Variant);
            return qh.GetAll();
        }
        public static List<T> Children<T>(this BaseModel n) where T : BaseModel
        {
            var qh = new QueryHelper<T>();
            qh      
                    .And()
                    .Field(x => x.Path, n.Path + "/".WildCardMulti())
                    .Not()
                    .Field(x => x.Path, n.Path+"/".WildCardMulti() + "/*");
            return qh.GetAll();
        }
        public static List<T> Descendants<T>(this BaseModel n) where T : BaseModel {
            var qh = new QueryHelper<T>();
            qh.Field(x => x.Path, n.Path+"/".WildCardMulti());
            return qh.GetAll();
        }
        
        public static Dictionary<string, Dictionary<string, T>> GroupByID<T>(this List<T> items) where T : BaseModel
        {
            var d = new Dictionary<string, Dictionary<string, T>>();
            items.GroupBy(x => x.Id).ToList().ForEach(x =>
            {
                d.Add(x.Key.ToString(), new Dictionary<string, T>());
                x.ToList().ForEach(y => d[x.Key.ToString()][y.Variant] = y);
            });
            return d;
        }

        public static void Delete<T>(this List<T> toDelete) where T:BaseModel {
            var indexer = PuckCache.PuckIndexer;
            indexer.Delete(toDelete);
        }

        public static Dictionary<string, Dictionary<string, T>> GroupByPath<T>(this List<T> items) where T : BaseModel
        {
            var d = new Dictionary<string, Dictionary<string, T>>();
            items.GroupBy(x => x.Path).ToList().ForEach(x =>
            {
                d.Add(x.Key, new Dictionary<string, T>());
                x.ToList().ForEach(y => d[x.Key][y.Variant] = y);
            });
            return d;
        }
    }
    public class QueryHelper<TModel> where TModel : BaseModel
    {
        public static I_Content_Searcher searcher = PuckCache.PuckSearcher;

        //query builders append to this string
        string query;
        Sort sort = new Sort();
        static string namePattern = @"(?:[A-Za-z0-9]*\()?[A-Za-z0-9]\.([A-Za-z0-9.]*)";
        static string nameArrayPattern = @"\.get_Item\(\d\)";
        static string paramPattern = @"((?:[a-zA-Z0-9]+\.?)+)\)";
        static string queryPattern = @"^\(*""(.*)""\s";
        static string fieldPattern = @"@";
        static string dateFormat = "yyyyMMddHHmmss";
        
        //regexes compiled on startup and reused since they will be used frequently
        static Regex nameRegex = new Regex(namePattern,RegexOptions.Compiled);
        static Regex nameArrayRegex = new Regex(nameArrayPattern, RegexOptions.Compiled);
        static Regex paramRegex = new Regex(paramPattern, RegexOptions.Compiled);
        static Regex queryRegex = new Regex(queryPattern, RegexOptions.Compiled);
        static Regex fieldRegex = new Regex(fieldPattern,RegexOptions.Compiled);

        //static helpers
        public static IList<Dictionary<string,string>> Query(string q){
            return searcher.Query(q);
        }
        public static IList<Dictionary<string, string>> Query(Query q)
        {
            return searcher.Query(q);
        }
        public static string Escape (string q){
            return QueryParser.Escape(q);
        }
        private static string getName(string str) {
            str = nameArrayRegex.Replace(str, "");
            var match = nameRegex.Match(str);
            string result = match.Groups[1].Value;
            result = result.ToLower();
            return result;
        }

        public static string GetName<TModel>(Expression<Func<TModel, object>> exp)
        {
            return getName(exp.Body.ToString());            
        }

        public static string Format<TModel>(Expression<Func<TModel, object>> exp)
        {
            return Format<TModel>(exp, null);
        }

        public static string Format<TModel>(Expression<Func<TModel, object>> exp, params string[] values)
        {
            values = values.Select(x => Escape(x)).ToArray();
            string bodystr = exp.Body.ToString();
            var pmatches =paramRegex.Matches(bodystr);
            var qmatch = queryRegex.Matches(bodystr);
            var query = qmatch[0].Groups[1].Value;

            for (var i = 0; i < pmatches.Count; i++)
            {
                var p = pmatches[i].Groups[1].Value;
                p = getName(p);
                query = fieldRegex.Replace(query, p, 1);
            }
            if (values != null)
            {
                query = string.Format(query, values);
            }
            return query;
        }

        public static List<T> GetAll<T>() where T:BaseModel{
            return searcher.Get<T>().ToList();
        }

        public static List<T> CurrentAll<T>() where T : BaseModel
        {
            string path = HttpContext.Current.Request.Url.LocalPath;
            return searcher.Query<T>(string.Format("+{0}:{1} +{2}:{3}",FieldKeys.PuckTypeChain,typeof(T).FullName,FieldKeys.Path,path)).ToList();
        }

        public static T Current<T>() where T : BaseModel
        {
            var variant = CultureInfo.CurrentCulture.Name;
            string path = HttpContext.Current.Request.Url.LocalPath;
            return searcher.Query<T>(
                string.Format("+{0}:{1} +{2}:{3} +{4}:{5}", 
                    FieldKeys.PuckTypeChain, typeof(T).FullName, FieldKeys.Path, path,FieldKeys.Variant,variant
                ))
                .FirstOrDefault();
        }

        //constructor
        public QueryHelper(bool prependTypeTerm=true)
        {
            if(prependTypeTerm)
                query += "+"+this.Field(FieldKeys.PuckTypeChain, typeof(TModel).FullName.Wrap())+" ";
        }

        public QueryHelper<TModel> New() {
            return new QueryHelper<TModel>(prependTypeTerm:false);
        }

        //query builders
        public void Clear() {
            query = "+" + this.Field(FieldKeys.PuckTypeChain, Escape(typeof(TModel).FullName).Wrap()) + " ";
        }

        public QueryHelper<TModel> Format(Expression<Func<TModel, object>> exp) {
            query+= QueryHelper<TModel>.Format<TModel>(exp);
            return this;
        }

        public QueryHelper<TModel> Format(Expression<Func<TModel, object>> exp,params string[] values)
        {
            query += QueryHelper<TModel>.Format<TModel>(exp,values);
            return this;
        }

        public QueryHelper<TModel> Range(Expression<Func<TModel, object>> exp,string start,string end,bool inclusiveStart=true,bool inclusiveEnd=true)
        {
            string key=getName(exp.Body.ToString());
            string openTag = inclusiveStart ? "[" : "{";
            string closeTag = inclusiveEnd ? "]" : "}";
            query += string.Concat(key , openTag ,Escape(start)," TO ",Escape(end),closeTag," ");
            return this;
        }

        public QueryHelper<TModel> Range(Expression<Func<TModel, object>> exp, int start, int end, bool inclusiveStart = true, bool inclusiveEnd = true)
        {
            return this.Range(exp,start.ToString(),end.ToString(),inclusiveStart,inclusiveEnd);
        }

        public QueryHelper<TModel> Range(Expression<Func<TModel, object>> exp, DateTime start, DateTime end, bool inclusiveStart = true, bool inclusiveEnd = true)
        {
            string key = getName(exp.Body.ToString());
            string openTag = inclusiveStart ? "[" : "{";
            string closeTag = inclusiveEnd ? "]" : "}";
            query += string.Concat(key , openTag, start.ToString(dateFormat), " TO ", end.ToString(dateFormat), closeTag," ");
            return this;
        }
        /*
        public QueryHelper<TModel> Sort(Expression<Func<TModel, object>> exp, bool descending)
        {
            string key = getName(exp.Body.ToString());
            sort.SetSort(new SortField(key,descending));
            query += string.Concat(key, ":", value, " ");
            return this;
        }
        */

        public QueryHelper<TModel> CurrentLanguage() {
            var key = FieldKeys.Variant;
            var variant = Thread.CurrentThread.CurrentCulture.Name;
            query += string.Concat("+",key,":",variant," ");
            return this;
        }

        public QueryHelper<TModel> Level(int level) {
            var includePath =string.Join("",Enumerable.Range(0, level).ToList().Select(x=>"/*"));
            var excludePath = includePath + "/";
            var key = FieldKeys.Path;
            query += string.Concat("+",key,":",includePath," +",key,":",excludePath," ");
            return this;
        }

        public QueryHelper<TModel> AllFields(string value)
        {
            foreach (var k in PuckCache.TypeFields[typeof(TModel).AssemblyQualifiedName]){
                query += string.Concat(k, ":", Escape(value), " ");
            }            
            return this;
        }

        public QueryHelper<TModel> Field(string key, string value)
        {
            query += string.Concat(key, ":", Escape(value)," ");
            return this;
        }

        public QueryHelper<TModel> Field(Expression<Func<TModel, object>> exp, bool value)
        {
            string key = getName(exp.Body.ToString());
            query += string.Concat(key, ":", value.ToString(), " ");
            return this;
        }

        public QueryHelper<TModel> Field(Expression<Func<TModel, object>> exp, string value)
        {
            string key = getName(exp.Body.ToString());
            query += string.Concat(key , ":",  Escape(value)," ");
            return this;
        }

        public QueryHelper<TModel> Field(Expression<Func<TModel, object>> exp, int ivalue)
        {
            return this.Field(exp,ivalue.ToString());
        }

        public QueryHelper<TModel> Field(Expression<Func<TModel, object>> exp, double dvalue)
        {
            return this.Field(exp, dvalue.ToString());
        }

        public QueryHelper<TModel> Field(Expression<Func<TModel, object>> exp, float fvalue)
        {
            return this.Field(exp, fvalue.ToString());
        }

        public QueryHelper<TModel> Field(Expression<Func<TModel, object>> exp, long lvalue)
        {
            return this.Field(exp, lvalue.ToString());
        }

        public QueryHelper<TModel> ExplicitType<AType>()
        {
            string key = FieldKeys.PuckType;
            query += string.Concat("+",key, ":", Escape(typeof(AType).AssemblyQualifiedName), " ");
            return this;
        }

        public QueryHelper<TModel> ExplicitType()
        {
            string key = FieldKeys.PuckType;
            query += string.Concat("+",key, ":", Escape(typeof(TModel).AssemblyQualifiedName), " ");
            return this;
        }

        public QueryHelper<TModel> Variant(string value)
        {
            string key = FieldKeys.Variant;
            query += string.Concat(key, ":", Escape(value), " ");
            return this;
        }

        public QueryHelper<TModel> ID(string value)
        {
            string key = FieldKeys.ID;
            query += string.Concat(key, ":", Escape(value), " ");
            return this;
        }

        public QueryHelper<TModel> ID(Guid value)
        {
            string key = FieldKeys.ID;
            query += string.Concat(key, ":", Escape(value.ToString()), " ");
            return this;
        }

        public QueryHelper<TModel> Directory(string value) {
            string key = FieldKeys.Path;
            if (!value.EndsWith("/"))
                value += "/";
            query += string.Concat("+",key,":",value.WildCardMulti()," -",key,":",value.WildCardMulti()+"/".WildCardMulti());
            return this;
        }

        public QueryHelper<TModel> And(QueryHelper<TModel> q=null)
        {
            if (q == null)
            {
                query += "+";
            }
            else {
                query += string.Concat("AND(",q.query,") ");
            }
            return this;
        }

        public QueryHelper<TModel> Or(QueryHelper<TModel> q = null)
        {
            if (q == null)
            {
                query += "OR ";
            }
            else
            {
                query += string.Concat("OR(", q.query, ") ");
            }
            return this;
        }

        public QueryHelper<TModel> Not(QueryHelper<TModel> q = null)
        {
            if (q == null)
            {
                query += "-";
            }
            else
            {
                query += string.Concat("-(", q.query, ") ");
            }
            return this;
        }

        //overrides
        public override string ToString()
        {
            return query;
        }

        //query executors
        public List<TModel> GetAll()
        {
            var result = searcher.Query<TModel>(query).ToList();
            return result;
        }

        public TModel Get()
        {
            var result = searcher.Query<TModel>(query).FirstOrDefault();
            return result;
        }
    }
}
