using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace puck.core.ValueProviders
{
    public class FileValueProviderFactory:ValueProviderFactory
    {
        public override IValueProvider GetValueProvider(ControllerContext controllerContext)
        {
            return new FileValueProvider();
        }
    }
}
