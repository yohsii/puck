using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using puck.core.Abstract;
using System.Web;
using System.Web.Caching;

namespace puck.core.Concrete
{
    public class DefaultCache:I_Puck_Cache
    {

        public void Add(string key,object value, int minutes)
        {
            HttpContext.Current.Cache.Insert(key, value, null, Cache.NoAbsoluteExpiration, TimeSpan.FromMinutes(minutes));
        }

        public void Add(string key,object value)
        {
            HttpContext.Current.Cache.Insert(key, value);
        }

        public void Remove(string key)
        {
            HttpContext.Current.Cache.Remove(key);
        }
    }
}
