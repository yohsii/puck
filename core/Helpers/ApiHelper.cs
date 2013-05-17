using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using puck.core.Base;
using System.Web;

namespace puck.core.Helpers
{
    public class ApiHelper
    {
        public static Dictionary<string,object>GetIndexableDictionary(List<FlattenedObject> props){
            var result = new Dictionary<string, object>();
            foreach(var prop in props){
                
            }
            return result;
        }

        public static IEnumerable<Type> FindDerivedClasses(Assembly assembly, Type baseType, List<Type> excluded) {
            excluded = excluded ?? new List<Type>();
            assembly=assembly??Assembly.GetCallingAssembly();
            return assembly.GetTypes().Where(x=> x != baseType && baseType.IsAssignableFrom(x) && ! excluded.Contains(x));
        }
        
        public static List<Type> Models { 
            get {
                return FindDerivedClasses(
                    Assembly.LoadFrom(HttpContext.Current.Server.MapPath("~/bin")+"\\puck.ui.dll")
                    ,typeof(BaseModel)
                    ,null
                    ).ToList();
            }
        }
        

    }
}
