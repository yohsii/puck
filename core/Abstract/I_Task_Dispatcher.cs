using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Hosting;
using puck.core.Base;

namespace puck.core.Abstract
{
    public interface I_Task_Dispatcher: IRegisteredObject
    {
        List<BaseTask> Tasks { get; set; }
        void Start();
        bool ShouldRunNow(BaseTask t);
        bool CanRun(BaseTask t);
        void Stop(bool immediate);
    }
}
