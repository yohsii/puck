using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Hosting;

namespace puck.core.Abstract
{
    public interface I_Task_Dispatcher: IRegisteredObject
    {
        void Start();
        bool ShouldRun(I_Puck_Task t);
    }
}
