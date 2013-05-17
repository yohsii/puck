using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace puck.core.Abstract
{
    public interface I_Content_Indexer
    {
        void Index(Dictionary<string,string> values);
        void Index(List<Dictionary<string, string>> values);
        void Index<T>(T model);
        void Delete(string id);
        void Ini();
        void Optimize();
    }
}
