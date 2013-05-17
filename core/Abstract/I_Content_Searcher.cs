using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace puck.core.Abstract
{
    public interface I_Content_Searcher
    {
        IDictionary<string, string> Query(string query);
        IList<T> Query<T>(string query);
        IList<T> Get<T>();        
    }
}
