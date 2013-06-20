using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using puck.core.Abstract;
using puck.core.Constants;
using puck.core.Helpers;

namespace puck.core
{
    public static class Bootstrap
    {
        public static void Ini() {
            ApiHelper.UpdateDomainMappings();
            ApiHelper.UpdatePathLocaleMappings();
            ApiHelper.UpdateTaskMappings();
            
        }
    }
}
