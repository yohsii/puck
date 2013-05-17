using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using puck.core.Abstract;

namespace puck.core.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    class DateTransformer :Attribute, I_Property_Transformer<DateTime,String>
    {
        public string Transform(DateTime dt)
        {
            return dt.ToString("yyyyMMddHHmmss");
        }
                
    }
}
