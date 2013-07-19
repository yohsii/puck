using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Search;


namespace puck.core.Abstract
{
    public interface I_Content_Searcher
    {
        IList<Dictionary<string, string>> Query(Query query);
        IList<Dictionary<string, string>> Query(string query);
        IList<Dictionary<string, string>> Query(string query,string typeName);
        IList<T> Query<T>(string query);
        IList<T> QueryNoCast<T>(string query);
        IList<T> Query<T>(string query,Filter filter);
        IList<T> QueryNoCast<T>(string query,Filter filter);
        IList<T> Get<T>();        
    }
}
