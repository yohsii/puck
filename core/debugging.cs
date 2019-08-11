using puck.core.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace puck.core
{
    public class debugging
    {
        public static string BaseTypeAQN() {
            return typeof(BaseModel).AssemblyQualifiedName;
        }
        public static Type GetType(string aqn) {
            return Type.GetType(aqn);
        }
    }
}
