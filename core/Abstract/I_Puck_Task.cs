using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace puck.core.Abstract
{
    public interface I_Puck_Task
    {
        string Name{get;set;}
        bool Recurring{get;set;}
        int IntervalSeconds { get; set; }
        DateTime RunOn { get; set; }
        DateTime? LastRun { get; set; }
        void Run(CancellationToken t);        
    }
}
