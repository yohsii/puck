using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using puck.core.Base;

namespace puck.core.Events
{
    public class IndexingEventArgs : EventArgs
    {
        public BaseModel Node { get; set; }
    }
    public class BeforeIndexingEventArgs:IndexingEventArgs {
        public bool Cancel { get; set; }
    }
    public class DispatchEventArgs:EventArgs {
        public BaseTask Task { get; set; }
    }

    public class MoveEventArgs : EventArgs
    {
        public List<BaseModel> Nodes { get; set; }
        public List<BaseModel> DestinationNodes { get; set; }
    }
    public class BeforeMoveEventArgs : MoveEventArgs {
        public bool Cancel{get;set;}
    }
}
