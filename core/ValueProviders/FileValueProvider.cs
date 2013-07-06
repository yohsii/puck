using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web;
using System.Globalization;

namespace puck.core.ValueProviders
{
    public class FileValueProvider:IValueProvider
    {
        public bool ContainsPrefix(string prefix)
        {
            return true;
        }
        public ValueProviderResult GetValue(string key)
        {
            var f = HttpContext.Current.Request.Files[key];
            if (f == null)
                return null;
            HttpPostedFileBase file = new HttpPostedFileWrapper(f);
            return file!=null ?
            new ValueProviderResult(file, null, CultureInfo.InvariantCulture)
            : null;
        }
    }
}
