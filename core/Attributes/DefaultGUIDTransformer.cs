using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using puck.core.Abstract;

namespace puck.core.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DefaultGUIDTransformer:Attribute,I_Property_Transformer<Guid,string>
    {
        public string Transform(Guid p) {
            if (p == default(Guid))
                return Guid.NewGuid().ToString();
            else
                return p.ToString();
        }
    }
}
