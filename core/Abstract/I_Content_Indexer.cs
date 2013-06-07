using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using puck.core.Base;

namespace puck.core.Abstract
{
    public interface I_Content_Indexer
    {
        void Index(Dictionary<string,string> values);
        void Index(List<Dictionary<string, string>> values);
        void Index<T>(T model) where T:BaseModel;
        void Index<T>(List<T> models) where T : BaseModel;
        void Delete(string query);
        void Delete<T>(List<T> toDelete) where T : BaseModel;
        void Delete<T>(T toDelete) where T : BaseModel;
        void Ini();
        void Optimize();
    }
}
