using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using puck.core.Abstract;
using puck.core.Attributes;
using Newtonsoft.Json;
using System.Web.Mvc;

namespace puck.core.Base
{
    public class BaseTask
    {
        [HiddenInput(DisplayValue=false)]
        public int ID { get; set; }
        public string Name { get; set; }
        public bool Recurring { get; set; }
        public int IntervalMinutes { get; set; }
        public DateTime RunOn { get; set; }
        [HiddenInput(DisplayValue = false)]
        public DateTime? LastRun { get; set; }        
        
        public virtual void Run(CancellationToken t){

        }        
    }
}
