using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using puck.core.Base;
using System.Web;
using System.Threading.Tasks;
using puck.core.Abstract;
using System.Web.Mvc;
using puck.core.Concrete;
using System.Text.RegularExpressions;
using puck.core.Models;
using puck.core.Constants;
using System.Globalization;
using Newtonsoft.Json;
using Ninject;
using puck.core.Entities;
using puck.core.Exceptions;
using puck.core.Events;
using System.Web.Security;
namespace puck.core.Helpers
{
    public partial class ApiHelper
    {
        static ApiHelper()
        {
            BeforeIndex += new EventHandler<BeforeIndexingEventArgs>(DelegateBeforeIndexing);
            AfterIndex += new EventHandler<IndexingEventArgs>(DelegateAfterIndexing);
            BeforeDelete += new EventHandler<BeforeIndexingEventArgs>(DelegateBeforeDelete);
            AfterDelete += new EventHandler<IndexingEventArgs>(DelegateAfterDelete);
            BeforeMove += new EventHandler<BeforeMoveEventArgs>(DelegateBeforeMove);
            AfterMove += new EventHandler<MoveEventArgs>(DelegateAfterMove);
        }
        private static void DelegateBeforeEvent(Dictionary<string, Tuple<Type, Action<object, BeforeIndexingEventArgs>, bool>> list, object n, BeforeIndexingEventArgs e)
        {
            var type = e.Node.GetType();
            //refactor:can probably use is operator to implement event propagation
            var types = ApiHelper.BaseTypes(type);
            types.Add(type);
            list.Where(x => x.Value.Item1 == type || (x.Value.Item3 && types.Contains(x.Value.Item1)))
                .ToList().ForEach(x =>
                {
                    x.Value.Item2(n, e);
                });
        }
        private static void DelegateAfterEvent(Dictionary<string, Tuple<Type, Action<object, IndexingEventArgs>, bool>> list, object n, IndexingEventArgs e)
        {
            var type = e.Node.GetType();
            //refactor:can probably use is operator to implement event propagation
            var types = ApiHelper.BaseTypes(type);
            types.Add(type);
            list.Where(x => x.Value.Item1 == type || (x.Value.Item3 && types.Contains(x.Value.Item1)))
                .ToList().ForEach(x =>
                {
                    x.Value.Item2(n, e);
                });
        }

        private static void DelegateBeforeMoveEvent(Dictionary<string, Tuple<Type, Action<object, BeforeMoveEventArgs>, bool>> list, object n, BeforeMoveEventArgs e)
        {
            var type = e.Nodes.First().GetType();
            //refactor:can probably use is operator to implement event propagation
            var types = ApiHelper.BaseTypes(type);
            types.Add(type);
            list.Where(x => x.Value.Item1 == type || (x.Value.Item3 && types.Contains(x.Value.Item1)))
                .ToList().ForEach(x =>
                {
                    x.Value.Item2(n, e);
                });
        }
        private static void DelegateAfterMoveEvent(Dictionary<string, Tuple<Type, Action<object, MoveEventArgs>, bool>> list, object n, MoveEventArgs e)
        {
            var type = e.Nodes.First().GetType();
            //refactor:can probably use is operator to implement event propagation
            var types = ApiHelper.BaseTypes(type);
            types.Add(type);
            list.Where(x => x.Value.Item1 == type || (x.Value.Item3 && types.Contains(x.Value.Item1)))
                .ToList().ForEach(x =>
                {
                    x.Value.Item2(n, e);
                });
        }

        private static void DelegateBeforeIndexing(object n, BeforeIndexingEventArgs e)
        {
            DelegateBeforeEvent(BeforeIndexActionList, n, e);
        }
        private static void DelegateAfterIndexing(object n, IndexingEventArgs e)
        {
            DelegateAfterEvent(AfterIndexActionList, n, e);
        }
        private static void DelegateBeforeDelete(object n, BeforeIndexingEventArgs e)
        {
            DelegateBeforeEvent(BeforeDeleteActionList, n, e);
        }
        private static void DelegateAfterDelete(object n, IndexingEventArgs e)
        {
            DelegateAfterEvent(AfterIndexActionList, n, e);
        }
        private static void DelegateBeforeMove(object n, BeforeMoveEventArgs e)
        {
            DelegateBeforeMoveEvent(BeforeMoveActionList, n, e);
        }
        private static void DelegateAfterMove(object n, MoveEventArgs e)
        {
            DelegateAfterMoveEvent(AfterMoveActionList, n, e);
        }

        public static Dictionary<string, Tuple<Type, Action<object, BeforeIndexingEventArgs>, bool>> BeforeIndexActionList =
            new Dictionary<string, Tuple<Type, Action<object, BeforeIndexingEventArgs>, bool>>();

        public static Dictionary<string, Tuple<Type, Action<object, IndexingEventArgs>, bool>> AfterIndexActionList =
            new Dictionary<string, Tuple<Type, Action<object, IndexingEventArgs>, bool>>();

        public static Dictionary<string, Tuple<Type, Action<object, BeforeIndexingEventArgs>, bool>> BeforeDeleteActionList =
            new Dictionary<string, Tuple<Type, Action<object, BeforeIndexingEventArgs>, bool>>();

        public static Dictionary<string, Tuple<Type, Action<object, IndexingEventArgs>, bool>> AfterDeleteActionList =
            new Dictionary<string, Tuple<Type, Action<object, IndexingEventArgs>, bool>>();

        public static Dictionary<string, Tuple<Type, Action<object, BeforeMoveEventArgs>, bool>> BeforeMoveActionList =
            new Dictionary<string, Tuple<Type, Action<object, BeforeMoveEventArgs>, bool>>();

        public static Dictionary<string, Tuple<Type, Action<object, MoveEventArgs>, bool>> AfterMoveActionList =
            new Dictionary<string, Tuple<Type, Action<object, MoveEventArgs>, bool>>();

        public static event EventHandler<BeforeIndexingEventArgs> BeforeIndex;
        public static event EventHandler<IndexingEventArgs> AfterIndex;
        public static event EventHandler<BeforeIndexingEventArgs> BeforeDelete;
        public static event EventHandler<IndexingEventArgs> AfterDelete;
        public static event EventHandler<BeforeMoveEventArgs> BeforeMove;
        public static event EventHandler<MoveEventArgs> AfterMove;

        public static void RegisterBeforeIndexHandler<T>(string Name, Action<object, BeforeIndexingEventArgs> Handler, bool Propagate = false) where T : BaseModel
        {
            BeforeIndexActionList.Add(Name, new Tuple<Type, Action<object, BeforeIndexingEventArgs>, bool>(typeof(T), Handler, Propagate));
        }
        public static void RegisterAfterIndexHandler<T>(string Name, Action<object, IndexingEventArgs> Handler, bool Propagate = false) where T : BaseModel
        {
            AfterIndexActionList.Add(Name, new Tuple<Type, Action<object, IndexingEventArgs>, bool>(typeof(T), Handler, Propagate));
        }
        public static void RegisterBeforeDeleteHandler<T>(string Name, Action<object, BeforeIndexingEventArgs> Handler, bool Propagate = false) where T : BaseModel
        {
            BeforeDeleteActionList.Add(Name, new Tuple<Type, Action<object, BeforeIndexingEventArgs>, bool>(typeof(T), Handler, Propagate));
        }
        public static void RegisterAfterDeleteHandler<T>(string Name, Action<object, IndexingEventArgs> Handler, bool Propagate = false) where T : BaseModel
        {
            AfterDeleteActionList.Add(Name, new Tuple<Type, Action<object, IndexingEventArgs>, bool>(typeof(T), Handler, Propagate));
        }
        public static void RegisterBeforeMoveHandler<T>(string Name, Action<object, BeforeMoveEventArgs> Handler, bool Propagate = false) where T : BaseModel
        {
            BeforeMoveActionList.Add(Name, new Tuple<Type, Action<object, BeforeMoveEventArgs>, bool>(typeof(T), Handler, Propagate));
        }
        public static void RegisterAfterMoveHandler<T>(string Name, Action<object, MoveEventArgs> Handler, bool Propagate = false) where T : BaseModel
        {
            AfterMoveActionList.Add(Name, new Tuple<Type, Action<object, MoveEventArgs>, bool>(typeof(T), Handler, Propagate));
        }

        public static void UnRegisterBeforeIndexHandler(string Name)
        {
            BeforeIndexActionList.Remove(Name);
        }
        public static void UnRegisterAfterIndexHandler(string Name)
        {
            AfterIndexActionList.Remove(Name);
        }
        public static void UnRegisterBeforeDeleteHandler(string Name)
        {
            BeforeDeleteActionList.Remove(Name);
        }
        public static void UnRegisterAfterDeleteHandler(string Name)
        {
            AfterDeleteActionList.Remove(Name);
        }
        public static void UnRegisterBeforeMoveHandler(string Name)
        {
            BeforeMoveActionList.Remove(Name);
        }
        public static void UnRegisterAfterMoveHandler(string Name)
        {
            AfterMoveActionList.Remove(Name);
        }

        public static void OnBeforeIndex(object s, BeforeIndexingEventArgs args)
        {
            if (BeforeIndex != null)
                BeforeIndex(s, args);
        }

        public static void OnAfterIndex(object s, IndexingEventArgs args)
        {
            if (AfterIndex != null)
                AfterIndex(s, args);
        }

        public static void OnBeforeDelete(object s, BeforeIndexingEventArgs args)
        {
            if (BeforeDelete != null)
                BeforeDelete(s, args);
        }

        public static void OnAfterDelete(object s, IndexingEventArgs args)
        {
            if (AfterDelete != null)
                AfterDelete(s, args);
        }

        public static void OnBeforeMove(object s, BeforeMoveEventArgs args)
        {
            if (BeforeMove != null)
                BeforeMove(s, args);
        }

        public static void OnAfterMove(object s, MoveEventArgs args)
        {
            if (AfterMove != null)
                AfterMove(s, args);
        }
    }
}
