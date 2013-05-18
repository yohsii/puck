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
    }
    public class QueryHelper<TModel>
    {
        private static I_Content_Searcher searcher = DependencyResolver.Current.GetService<I_Content_Searcher>();

        //query builders append to this string
        string query;
        bool prepended = false;
        static string namePattern = @"(?:[A-Za-z0-9]*\()?[A-Za-z0-9]\.([A-Za-z0-9.]*)";
        static string paramPattern = @"((?:[a-zA-Z0-9]+\.?)+)\)";
        static string queryPattern = @"^\(*""(.*)""\s";
        static string fieldPattern = @"@";
        static string dateFormat = "yyyyMMddHHmmss";
        
        //regexes compiled on startup and reused since they will be used frequently
        static Regex nameRegex = new Regex(namePattern,RegexOptions.Compiled);
        static Regex paramRegex = new Regex(paramPattern, RegexOptions.Compiled);
        static Regex queryRegex = new Regex(queryPattern, RegexOptions.Compiled);
        static Regex fieldRegex = new Regex(fieldPattern,RegexOptions.Compiled);

        //static helpers
        private static string getName(string str) {
            var match = nameRegex.Match(str);
            string result = match.Groups[1].Value;
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

        //constructor
        public QueryHelper(bool prependTypeTerm=true)
        {
            if(prependTypeTerm)
                query += "+"+this.Field(FieldKeys.PuckType, typeof(TModel).FullName.Wrap())+" ";
        }

        public QueryHelper<TModel> New() {
            return new QueryHelper<TModel>(prependTypeTerm:false);
        }

        //query builders
        public void Clear() {
            query = string.Empty;
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
            query += string.Concat(key , openTag ,start," TO ",end,closeTag," ");
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

        //particularly inefficient, cache result!
        public QueryHelper<TModel> AllFields(string value)
        {
            var props = ObjectDumper.Write(Activator.CreateInstance(typeof(TModel)),int.MaxValue);
            foreach (var p in props){
                query += string.Concat(p.Key, ":", value, " ");
            }            
            return this;
        }

        public QueryHelper<TModel> Field(string key, string value)
        {
            query += string.Concat(key, ":", value," ");
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

        public QueryHelper<TModel> ID(string value)
        {
            string key = FieldKeys.ID;
            query += string.Concat(key, ":", value, " ");
            return this;
        }

        public QueryHelper<TModel> Path(string value)
        {
            string key = FieldKeys.Path;
            query += string.Concat(key, ":", value, " ");
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
