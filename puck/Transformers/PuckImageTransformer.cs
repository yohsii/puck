using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using puck.core.Abstract;
using puck.areas.admin.Models;
using puck.core.Base;
namespace puck.Transformers
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public class PuckImageTransformer : Attribute, I_Property_Transformer<PuckImage, PuckImage>
    {
        public PuckImage Transform(BaseModel m,string propertyName,string ukey,PuckImage p)
        {
            p.File = null;
            return p;
        }
    }    
}