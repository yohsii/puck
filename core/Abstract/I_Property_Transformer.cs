using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace puck.core.Abstract
{
    interface I_Property_Transformer<TIn,TOut>
    {
        TOut Transform(TIn p);
    }
}
