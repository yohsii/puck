using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using puck.core.Base;
using puck.core.Events;

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
        event EventHandler<BeforeIndexingEventArgs> BeforeIndex;
        event EventHandler<IndexingEventArgs> AfterIndex;
        event EventHandler<BeforeIndexingEventArgs> BeforeDelete;
        event EventHandler<IndexingEventArgs> AfterDelete;
        void RegisterBeforeIndexHandler<T>(string Name, Action<object, BeforeIndexingEventArgs> Handler, bool Propagate = false) where T : BaseModel;
        void RegisterAfterIndexHandler<T>(string Name, Action<object, IndexingEventArgs> Handler, bool Propagate = false) where T : BaseModel;
        void RegisterBeforeDeleteHandler<T>(string Name, Action<object, BeforeIndexingEventArgs> Handler, bool Propagate = false) where T : BaseModel;
        void RegisterAfterDeleteHandler<T>(string Name, Action<object, IndexingEventArgs> Handler, bool Propagate = false) where T : BaseModel;
        void UnRegisterBeforeIndexHandler<T>(string Name);
        void UnRegisterAfterIndexHandler<T>(string Name);
        void UnRegisterBeforeDeleteHandler<T>(string Name);
        void UnRegisterAfterDeleteHandler<T>(string Name);

    }
}
