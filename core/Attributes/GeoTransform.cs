﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using puck.core.Abstract;
using puck.core.Base;
using puck.core.Models;

namespace puck.core.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class GeoTransform : Attribute, I_Property_Transformer<GeoPosition, GeoPosition>
    {
        public GeoPosition Transform(BaseModel m, string propertyName, string ukey, GeoPosition pos)
        {
            if(pos.Longitude.HasValue && pos.Latitude.HasValue)
                pos.LongLat = string.Concat(pos.Latitude, ",", pos.Longitude);
            return pos;
        }
    }
}
