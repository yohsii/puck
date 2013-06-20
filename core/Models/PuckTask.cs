using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using puck.core.Base;
using puck.core.Attributes;

namespace puck.core.Models
{
    public class PuckTask:BaseTask
    {
        public string Label { get; set; }
        public int Number { get; set; }
        public override void Run(System.Threading.CancellationToken t)
        {
            string s = "";
            
        }
    }
}
