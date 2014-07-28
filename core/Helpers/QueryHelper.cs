﻿using System;
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
using puck.core.Models;
using Lucene.Net.Spatial.Queries;
using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
using Lucene.Net.Spatial;
using Lucene.Net.Spatial.Vector;

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
        //misc helper extensions
        public static int GetLevel(this BaseModel m)
        {
            int level = m.Path.Count(x => x == '/');
            return level;
        }

        public static string Url(this BaseModel m)
        {
            //remove root from path - roots are determined by domain
            if (m.Path.Count(x => x == '/') == 1)
                return "/";
            var firstOccurrence = m.Path.IndexOf('/');
            var secondOccurrence = m.Path.IndexOf('/', firstOccurrence+1);
            return m.Path.Substring(secondOccurrence);
        }

        public static List<T> GetAll<T>(this List<PuckPicker> pp, bool noCast = false) where T : BaseModel
        {
            if (pp == null)
                return new List<T>();
            var qh = new QueryHelper<T>();
            var qhinner1 = qh.New();
            foreach (var p in pp) {
                var qhinner2 = qhinner1.New().ID(p.Id);
                if (!string.IsNullOrEmpty(p.Variant))
                    qhinner2.Variant(p.Variant.ToLower());
                qhinner1.Group(
                    qhinner2
                );
            }
            qh.And().Group(qhinner1);
            if (noCast)
                return qh.GetAllNoCast();
            else
                return qh.GetAll();
        }

        public static List<T> GetAll<T>(this PuckPicker pp,bool noCast=false) where T : BaseModel
        {
            if (pp == null)
                return new List<T>();
            var qh = new QueryHelper<T>();
            qh.ID(pp.Id);
            if (!string.IsNullOrEmpty(pp.Variant))
                qh.Variant(pp.Variant);
            if (noCast)
                return qh.GetAllNoCast();
            else
                return qh.GetAll();
        }

        public static T Get<T>(this PuckPicker pp,bool noCast=false) where T : BaseModel
        {
            return GetAll<T>(pp, noCast).FirstOrDefault();
        }
        //retrieval extensions
        public static List<T> Parent<T>(this BaseModel n,bool currentLanguage = true,bool noCast=false) where T : BaseModel
        {
            var qh = new QueryHelper<T>();
            string path = n.Path.Substring(0, n.Path.LastIndexOf('/'));
            qh
                .And()
                .Field(x => x.Path, path.ToLower());
            if (currentLanguage)
                qh.CurrentLanguage();
            if (noCast)
                return qh.GetAllNoCast();
            else
                return qh.GetAll();
        }
        public static List<T> Ancestors<T>(this BaseModel n,bool currentLanguage=true,bool noCast = false,bool ExplicitType=false) where T : BaseModel {
            var qh = new QueryHelper<T>();
            string nodePath = n.Path.ToLower();
            var innerQ = qh.New();
            while (nodePath.Count(x => x == '/') > 1)
            {
                nodePath = nodePath.Substring(0, nodePath.LastIndexOf('/'));
                innerQ
                    .Field(x=>x.Path,nodePath);
            }
            qh.And(innerQ);
            if (ExplicitType)
                qh.ExplicitType();
            if (currentLanguage)
                qh.CurrentLanguage();
            if (noCast)
                return qh.GetAllNoCast();
            else
                return qh.GetAll();
        }
        public static List<T> Siblings<T>(this BaseModel n,bool currentLanguage=true,bool noCast=false,bool ExplicitType=false) where T : BaseModel {
            var qh = new QueryHelper<T>();
            qh
                    .And()
                    .Field(x => x.Path, ApiHelper.DirOfPath(n.Path.ToLower()).WildCardMulti())
                    .Not()
                    .Field(x => x.Path, ApiHelper.DirOfPath(n.Path.ToLower()).WildCardMulti() + "/*")
                    .Not()
                    .Field(x => x.Id, n.Id.ToString().Wrap());
            if (ExplicitType)
                qh.ExplicitType();
            if (currentLanguage)
                qh.CurrentLanguage();
            if (noCast)
                return qh.GetAllNoCast();
            else
                return qh.GetAll();                
        }
        public static List<T> Variants<T>(this BaseModel n,bool noCast=false) where T : BaseModel
        {
            var qh = new QueryHelper<T>();
            qh      
                    .And()
                    .Field(x => x.Id, n.Id.ToString())
                    .Not()
                    .Field(x => x.Variant, n.Variant);
            if (noCast)
                return qh.GetAllNoCast();
            else
                return qh.GetAll();
        }
        public static List<T> Children<T>(this BaseModel n,bool currentLanguage=true,bool noCast = false,bool ExplicitType=false) where T : BaseModel
        {
            var qh = new QueryHelper<T>();
            qh      
                    .And()
                    .Field(x => x.Path, n.Path.ToLower() + "/".WildCardMulti())
                    .Not()
                    .Field(x => x.Path, n.Path.ToLower()+"/".WildCardMulti() + "/*");
            if (ExplicitType)
                qh.ExplicitType();
            if (currentLanguage)
                qh.CurrentLanguage();
            if (noCast)
                return qh.GetAllNoCast();
            else
                return qh.GetAll();
        }
        public static List<T> Descendants<T>(this BaseModel n,bool currentLanguage=true,bool noCast = false,bool ExplicitType=false) where T : BaseModel {
            var qh = new QueryHelper<T>();
            qh.And()
                .Field(x => x.Path, n.Path.ToLower()+"/".WildCardMulti());
            if (ExplicitType)
                qh.ExplicitType();
            if (currentLanguage)
                qh.CurrentLanguage();
            if (noCast)
                return qh.GetAllNoCast();
            else
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
        /*
        public static void Delete<T>(this List<T> toDelete) where T:BaseModel {
            var indexer = PuckCache.PuckIndexer;
            indexer.Delete(toDelete);
        }
        */
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
        public static SpatialContext ctx = SpatialContext.GEO;
        public Lucene.Net.Search.Filter filter;
        //query builders append to this string
        string query="";
        int totalHits=0;
        Sort sort = null;
        List<SortField> sorts = null;
        public int TotalHits { get { return totalHits; } }
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
            values = values.Select(x => x).ToArray();
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

        public static string PathPrefix() {
            string domain = HttpContext.Current.Request.Url.Host.ToLower();
            string searchPathPrefix;
            if (!PuckCache.DomainRoots.TryGetValue(domain, out searchPathPrefix))
            {
                if (!PuckCache.DomainRoots.TryGetValue("*", out searchPathPrefix))
                    throw new Exception("domain roots not set. DOMAIN:" + domain);
            }
            return searchPathPrefix.ToLower();
        }

        public static List<TModel> CurrentAll()
        {
            string absPath = HttpContext.Current.Request.Url.AbsolutePath.ToLower();
            string path = PathPrefix() + (absPath == "/" ? "" : absPath);
            var qh = new QueryHelper<TModel>();
            qh.And().Field(x => x.Path, path);
            return qh.GetAll();
        }

        public static TModel Current()
        {
            var variant = CultureInfo.CurrentCulture.Name.ToLower();
            string absPath = HttpContext.Current.Request.Url.AbsolutePath.ToLower();
            string path = PathPrefix() + (absPath == "/" ? "" : absPath);
            var qh = new QueryHelper<TModel>();
            qh.And().Field(x => x.Path, path).Variant(variant);
            return qh.Get();
        }

        //constructor
        public QueryHelper(bool prependTypeTerm=true)
        {
            if(prependTypeTerm)
                this.And().Field(x=>x.TypeChain,typeof(TModel).FullName.Wrap()).And().Field(x=>x.Published,"true");                
        }

        public QueryHelper<TModel> New() {
            return new QueryHelper<TModel>(prependTypeTerm:false);
        }

        //query builders
        public QueryHelper<TModel> Sort(Expression<Func<TModel, object>> exp, bool descending=false,int sortField=-1)
        {
            if (sort == null)
            {
                sort = new Sort();
                sorts = new List<SortField>();
            }
            string key = getName(exp.Body.ToString());
            if (sortField==-1){
                sortField = SortField.STRING;
                string fieldTypeName = PuckCache.TypeFields[typeof(TModel).AssemblyQualifiedName][key];
                if (fieldTypeName.Equals(typeof(int).AssemblyQualifiedName))
                {
                    sortField = SortField.INT;
                }
                else if (fieldTypeName.Equals(typeof(long).AssemblyQualifiedName))
                {
                    sortField = SortField.LONG;
                }
                else if (fieldTypeName.Equals(typeof(float).AssemblyQualifiedName))
                {
                    sortField = SortField.FLOAT;
                }
                else if (fieldTypeName.Equals(typeof(double).AssemblyQualifiedName))
                {
                    sortField = SortField.DOUBLE;
                }
            }
            sorts.Add(new SortField(key,sortField,descending));
            sort.SetSort(sorts.ToArray());
            return this;
        }
        public void Clear() {
            query = "+" + this.Field(FieldKeys.PuckTypeChain, typeof(TModel).FullName.Wrap()) + " ";
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
        
        public QueryHelper<TModel> Range(Expression<Func<TModel, object>> exp,string start,string end,bool inclusiveStart,bool inclusiveEnd)
        {
            string key=getName(exp.Body.ToString());
            string openTag = inclusiveStart ? "[" : "{";
            string closeTag = inclusiveEnd ? "]" : "}";
            query += string.Concat(key,":" , openTag ,start," TO ",end,closeTag," ");
            return this;
        }
        public QueryHelper<TModel> Range(Expression<Func<TModel, object>> exp, string start, string end, bool inclusiveStart)
        {
            return this.Range(exp,start,end,inclusiveStart,true);
        }
        public QueryHelper<TModel> Range(Expression<Func<TModel, object>> exp, string start, string end)
        {
            return this.Range(exp, start, end, true,true);
        }
        
        public QueryHelper<TModel> Range(Expression<Func<TModel, object>> exp, int start, int end, bool inclusiveStart, bool inclusiveEnd)
        {
            return this.Range(exp,start.ToString(),end.ToString(),inclusiveStart,inclusiveEnd);
        }
        public QueryHelper<TModel> Range(Expression<Func<TModel, object>> exp, int start, int end, bool inclusiveStart)
        {
            return this.Range(exp, start.ToString(), end.ToString(), inclusiveStart, true);
        }
        public QueryHelper<TModel> Range(Expression<Func<TModel, object>> exp, int start, int end)
        {
            return this.Range(exp, start.ToString(), end.ToString(), true,true);
        }

        public QueryHelper<TModel> Range(Expression<Func<TModel, object>> exp, long start, long end, bool inclusiveStart, bool inclusiveEnd)
        {
            return this.Range(exp, start.ToString(), end.ToString(), inclusiveStart, inclusiveEnd);
        }
        public QueryHelper<TModel> Range(Expression<Func<TModel, object>> exp, long start, long end, bool inclusiveStart)
        {
            return this.Range(exp, start.ToString(), end.ToString(), inclusiveStart, true);
        }
        public QueryHelper<TModel> Range(Expression<Func<TModel, object>> exp, long start, long end)
        {
            return this.Range(exp, start.ToString(), end.ToString(), true, true);
        }

        public QueryHelper<TModel> Range(Expression<Func<TModel, object>> exp, DateTime start, DateTime end, bool inclusiveStart, bool inclusiveEnd)
        {
            return this.Range(exp, start.ToString(dateFormat), end.ToString(dateFormat), inclusiveStart, inclusiveEnd);            
        }
        public QueryHelper<TModel> Range(Expression<Func<TModel, object>> exp, DateTime start, DateTime end, bool inclusiveStart)
        {
            return this.Range(exp, start.ToString(dateFormat), end.ToString(dateFormat), inclusiveStart, true);
        }
        public QueryHelper<TModel> Range(Expression<Func<TModel, object>> exp, DateTime start, DateTime end)
        {
            return this.Range(exp, start.ToString(dateFormat), end.ToString(dateFormat), true,true);
        }
        //extended range
        public QueryHelper<TModel> GreaterThanEqualTo(Expression<Func<TModel, object>> exp, DateTime start)
        {
            return this.Range(exp, start.ToString(dateFormat), DateTime.MaxValue.ToString(dateFormat), true, true);
        }
        public QueryHelper<TModel> LessThanEqualTo(Expression<Func<TModel, object>> exp, DateTime end)
        {
            return this.Range(exp, DateTime.MinValue.ToString(dateFormat), end.ToString(dateFormat), true, true);
        }

        public QueryHelper<TModel> GreaterThanEqualTo(Expression<Func<TModel, object>> exp, int start)
        {
            return this.Range(exp, start.ToString(), int.MaxValue.ToString(), true, true);
        }
        public QueryHelper<TModel> LessThanEqualTo(Expression<Func<TModel, object>> exp, int end)
        {
            return this.Range(exp, int.MinValue.ToString(), end.ToString(), true, true);
        }

        public QueryHelper<TModel> GreaterThanEqualTo(Expression<Func<TModel, object>> exp, long start)
        {
            return this.Range(exp, start.ToString(), long.MaxValue.ToString(), true, true);
        }
        public QueryHelper<TModel> LessThanEqualTo(Expression<Func<TModel, object>> exp, long end)
        {
            return this.Range(exp, long.MinValue.ToString(), end.ToString(), true, true);
        }

        private QueryHelper<TModel> GeoFilter(Expression<Func<TModel, object>> exp, double longitude, double latitude, double distDEG)
        {
            string name = getName(exp.Body.ToString());
            name = name.IndexOf('.') > -1 ? name.Substring(0, name.LastIndexOf('.')) : name;
            SpatialOperation op = SpatialOperation.Intersects;
            SpatialStrategy strat = new PointVectorStrategy(ctx, name);
            var point = ctx.MakePoint(longitude, latitude);
            var shape = ctx.MakeCircle(point, distDEG);
            var args = new SpatialArgs(op, shape);
            filter = strat.MakeFilter(args);
            return this;
        }

        public QueryHelper<TModel> WithinMiles(Expression<Func<TModel, object>> exp, double latitude, double longitude,int miles)
        {
            var distDEG = DistanceUtils.Dist2Degrees(miles, DistanceUtils.EARTH_MEAN_RADIUS_MI);
            return GeoFilter(exp,longitude,latitude,distDEG);
        }

        public QueryHelper<TModel> WithinKilometers(Expression<Func<TModel, object>> exp, double latitude, double longitude, int kilometers)
        {
            var distDEG = DistanceUtils.Dist2Degrees(kilometers, DistanceUtils.EARTH_MEAN_RADIUS_KM);
            return GeoFilter(exp, longitude, latitude, distDEG);
        }

        public QueryHelper<TModel> AllFields(string value)
        {
            query += "+(";
            foreach (var k in PuckCache.TypeFields[typeof(TModel).AssemblyQualifiedName]){
                query += string.Concat(k.Key, ":", value, " ");
            }
            query+=") ";
            return this;
        }

        public QueryHelper<TModel> Field(string key, string value)
        {
            query += string.Concat(key, ":", value," ");
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
            query += string.Concat(key , ":",  value," ");
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

        //filters
        private void TrimAnd() {
            if (query.EndsWith("+"))
                query = query.TrimEnd('+');
        }
        public QueryHelper<TModel> Ancestors(string path)
        {
            TrimAnd();
            string nodePath = path.ToLower();
            while (nodePath.Count(x => x == '/') > 1)
            {
                nodePath = nodePath.Substring(0, nodePath.LastIndexOf('/'));
                this.And()
                    .Field(x => x.Path, nodePath);
            }
            return this;
        }
        public QueryHelper<TModel> Siblings(string path, Guid id)
        {
            return this.Siblings(path, id.ToString());
        }
        public QueryHelper<TModel> Siblings(string path,string id)
        {
            TrimAnd();
            this
                    .And()
                    .Field(x => x.Path, ApiHelper.DirOfPath(path.ToLower()).WildCardMulti())
                    .Not()
                    .Field(x => x.Path, ApiHelper.DirOfPath(path.ToLower()).WildCardMulti() + "/*")
                    .Not()
                    .Field(x => x.Id, id.Wrap());
            return this;
        }
        public QueryHelper<TModel> Children(string path)
        {
            TrimAnd();
            this
                    .And()
                    .Field(x => x.Path, path.ToLower() + "/".WildCardMulti())
                    .Not()
                    .Field(x => x.Path, path.ToLower() + "/".WildCardMulti() + "/*");
            return this;
        }
        public QueryHelper<TModel> Descendants(string path)
        {
            TrimAnd();
            this.And()
                .Field(x => x.Path, path.ToLower() + "/".WildCardMulti());
            return this;
        }

        public QueryHelper<TModel> Descendants(BaseModel m)
        {
            TrimAnd();
            this.And()
                .Field(x => x.Path, m.Path.ToLower() + "/".WildCardMulti());
            return this;
        }

        public QueryHelper<TModel> CurrentRoot(BaseModel m = null)
        {
            TrimAnd();
            string currentPath=null;
            string currentRoot = null;
            if(m==null)
                currentPath= HttpContext.Current.Request.Url.AbsolutePath.TrimStart('/');
            else
                currentPath = m.Path.TrimStart('/');
            if (currentPath.IndexOf("/") > -1)
                currentRoot = currentPath.Substring(0, currentPath.IndexOf('/'));
            else currentRoot = currentPath;
            currentRoot = "/" + currentRoot + "/";
            this.And()
                .Field(x => x.Path, currentRoot.ToLower().WildCardMulti());
            return this;
        }

        public QueryHelper<TModel> CurrentLanguage()
        {
            TrimAnd();
            var key = FieldKeys.Variant;
            var variant = Thread.CurrentThread.CurrentCulture.Name;
            query += string.Concat("+",key, ":", variant.ToLower(), " ");
            return this;
        }

        public QueryHelper<TModel> Level(int level)
        {
            TrimAnd();
            var includePath = string.Join("", Enumerable.Range(0, level).ToList().Select(x => "/*"));
            var excludePath = includePath + "/";
            var key = FieldKeys.Path;
            query += string.Concat("+", key, ":", includePath, " -", key, ":", excludePath, " ");
            return this;
        }

        public QueryHelper<TModel> ExplicitType<AType>()
        {
            TrimAnd();
            string key = FieldKeys.PuckType;
            query += string.Concat("+",key, ":", typeof(AType).AssemblyQualifiedName.Wrap(), " ");
            return this;
        }

        public QueryHelper<TModel> ExplicitType()
        {
            TrimAnd();
            string key = FieldKeys.PuckType;
            query += string.Concat("+",key, ":", typeof(TModel).AssemblyQualifiedName.Wrap(), " ");
            return this;
        }

        public QueryHelper<TModel> Variant(string value)
        {
            TrimAnd();
            string key = FieldKeys.Variant;
            query += string.Concat("+",key, ":", value.ToLower(), " ");
            return this;
        }

        public QueryHelper<TModel> ID(string value)
        {
            TrimAnd();
            string key = FieldKeys.ID;
            query += string.Concat("+",key, ":", value, " ");
            return this;
        }

        public QueryHelper<TModel> ID(Guid value)
        {
            TrimAnd();
            string key = FieldKeys.ID;
            query += string.Concat("+",key, ":", value.ToString(), " ");
            return this;
        }

        public QueryHelper<TModel> Directory(string value) {
            TrimAnd();
            string key = FieldKeys.Path;
            if (!value.EndsWith("/"))
                value += "/";
            query += string.Concat("+",key,":",value.WildCardMulti()," -",key,":",value.WildCardMulti()+"/".WildCardMulti());
            return this;
        }
        //end filters

        //logical operators
        public QueryHelper<TModel> Group(QueryHelper<TModel> q)
        {
            query += string.Concat("(", q.query, ") ");
            return this;
        }

        public QueryHelper<TModel> And(QueryHelper<TModel> q=null)
        {
            TrimAnd();
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
        public List<TModel> GetAll(int limit=500,int skip = 0)
        {
            var result = searcher.Query<TModel>(query,filter,sort,out totalHits,limit,skip).ToList();
            return result;
        }

        public List<TModel> GetAllNoCast(int limit=500,int skip = 0)
        {
            var result = searcher.QueryNoCast<TModel>(query,filter,sort,out totalHits,limit,skip).ToList();
            return result;
        }

        public TModel Get()
        {
            var result = searcher.Query<TModel>(query,filter,sort,out totalHits,1,0).FirstOrDefault();
            return result;
        }
    }
}
