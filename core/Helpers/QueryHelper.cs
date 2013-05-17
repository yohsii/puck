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
    public static class QueryHelper<TModel>
    {
        private static I_Content_Searcher searcher = DependencyResolver.Current.GetService<I_Content_Searcher>();

        //query builders append to this string
        string query;
        
        static string namePattern = @"^[a-zA-Z0-9]*\.";
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
            string result = nameRegex.Replace(str, "");
            return result;
        }

        public static string GetName<TModel>(Expression<Func<TModel, string>> exp)
        {
            return getName(exp.Body.ToString());            
        }

        public static string Format<TModel>(Expression<Func<TModel, string>> exp)
        {
            return Format<TModel>(exp, null);
        }

        public static string Format<TModel>(Expression<Func<TModel, string>> exp, params string[] values)
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
        public QueryHelper(TModel type)
        {
            query += this.Field(FieldKeys.PuckType, typeof(TModel).FullName, false);
        }

        //overrides
        public override string ToString()
        {
            return query;
        }
        
        //query builders
        public void Clear() {
            query = string.Empty;
        }

        public QueryHelper<TModel> Format(Expression<Func<TModel, string>> exp) {
            query+= QueryHelper<TModel>.Format<TModel>(exp);
            return this;
        }

        public QueryHelper<TModel> Format(Expression<Func<TModel, string>> exp,params string[] values)
        {
            query += QueryHelper<TModel>.Format<TModel>(exp,values);
            return this;
        }

        public QueryHelper<TModel> Range(Expression<Func<TModel, string>> exp,string start,string end,bool inclusiveStart=true,bool inclusiveEnd=true)
        {
            string key=getName(exp.Body.ToString());
            string openTag = inclusiveStart ? "[" : "{";
            string closeTag = inclusiveEnd ? "]" : "}";
            query += string.Concat(key , openTag ,start," TO ",end,closeTag);
            return this;
        }

        public QueryHelper<TModel> Range(Expression<Func<TModel, string>> exp, int start, int end, bool inclusiveStart = true, bool inclusiveEnd = true)
        {
            return this.Range(exp,start.ToString(),end.ToString(),inclusiveStart,inclusiveEnd);
        }

        public QueryHelper<TModel> Range(Expression<Func<TModel, string>> exp, DateTime start, DateTime end, bool inclusiveStart = true, bool inclusiveEnd = true)
        {
            string key = getName(exp.Body.ToString());
            string openTag = inclusiveStart ? "[" : "{";
            string closeTag = inclusiveEnd ? "]" : "}";
            query += string.Concat(key , openTag, start.ToString(dateFormat), " TO ", end.ToString(dateFormat), closeTag);
            return this;
        }

        //particularly inefficient, cache result!
        public QueryHelper<TModel> AllFields(string value, bool encapsulateString = true)
        {
            var props = ObjectDumper.Write(Activator.CreateInstance(typeof(TModel)),int.MaxValue);
            value = QueryParser.Escape(value);
            if (encapsulateString)
                value = string.Concat("\"", value, "\"");
            foreach (var p in props){
                query += string.Concat(p.Key, ":", value, " ");
            }            
            return this;
        }

        public QueryHelper<TModel> Field(string key, string value, bool encapsulateString = true)
        {
            value = QueryParser.Escape(value);
            if (encapsulateString)
                value = string.Concat("\"", value, "\"");
            query += string.Concat(key, ":", value," ");
            return this;
        }

        public QueryHelper<TModel> Field(Expression<Func<TModel, string>> exp, string value, bool encapsulateString = true)
        {
            string key = getName(exp.Body.ToString());
            value = QueryParser.Escape(value);
            if (encapsulateString)
                value = string.Concat("\"", value, "\"");
            query += string.Concat(key , ":",  value," ");
            return this;
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
                query += "OR";
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
        
        //term modifier string extensions


        //query executors
        public List<TModel> GetAll<TModel>()
        {
            var result = searcher.Query<TModel>(query).ToList();
            return result;
        }

        public TModel Get<TModel>()
        {
            var result = searcher.Query<TModel>(query).FirstOrDefault();
            return result;
        }
    }
}
