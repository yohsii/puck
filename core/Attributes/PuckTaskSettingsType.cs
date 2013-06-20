using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace puck.core.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PuckTaskSettingsType : Attribute
    {
        public Type SettingsType { get; set; }
    }
}
