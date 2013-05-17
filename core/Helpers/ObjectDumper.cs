using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using puck.core.Abstract;
using System.Linq;
using Lucene.Net.Documents;
using puck.core.Attributes;
namespace puck.core.Helpers
{
    public class FlattenedObject {
        public Type Type { get; set; }
        public String Key { get; set; }
        public Object Value {get;set;}
        public Object OriginalValue { get; set; }
        public Object[] Attributes { get; set; }
        public Field.Index FieldIndexSetting { 
            get {
                var attr = Attributes.Where(x => x.GetType() == typeof(IndexSettings));
                if (attr.Any()) {
                    var sattr = (IndexSettings)attr.First();
                    return sattr.FieldIndexSetting;
                }
                return FieldSettings.FieldIndexSetting;
            } 
            
        }
        public Field.Store FieldStoreSetting
        {
            get {
                var attr = Attributes.Where(x => x.GetType() == typeof(IndexSettings));
                if (attr.Any())
                {
                    var sattr = (IndexSettings)attr.First();
                    return sattr.FieldStoreSetting;
                }
                return FieldSettings.FieldStoreSetting;
            }
            
        }
        public void Transform() {
            object attr = null;
            var tattr = Attributes
                .Where(x => x.GetType().GetInterfaces()
                    .Any(y => y.IsGenericType && y.GetGenericTypeDefinition() == typeof(I_Property_Transformer<,>))
                );
            if (tattr.Any())//check for custom transform attribute
            {
                attr = tattr.First();
            }
            else
            { //check for default transform for type
                if (FieldSettings.DefaultPropertyTransformers.ContainsKey(Type))
                {
                    attr = Activator.CreateInstance(FieldSettings.DefaultPropertyTransformers[Type]);
                }
            }
            //transform if possible
            if (attr != null)
            {
                var newValue = attr.GetType().GetMethod("Transform").Invoke(attr, new[] { Value });
                OriginalValue = Value;
                Value = newValue;
            }
        }
        
    }
    public class ObjectDumper
    {
        public static Dictionary<string,object> ToDictionary(List<FlattenedObject> props){
            var result = new Dictionary<string, object>();
            foreach (var p in props) {
                result.Add(p.Key.ToLower(),p.Value??string.Empty);
            }
            return result;
        }
        public static List<FlattenedObject> Write(object element, int depth)
        {
            ObjectDumper dumper = new ObjectDumper(depth);
            dumper.WriteObject("", element);
            dumper.result.ForEach(x=>x.Transform());
            return dumper.result;
        }

        int level;
        int depth;
        public List<FlattenedObject> result = new List<FlattenedObject>();

        private ObjectDumper(int depth)
        {
            this.depth = depth;
        }

        private void WriteObject(string prefix, object element)
        {
            if (element == null || element is ValueType || element is string)
            {
                var fo = new FlattenedObject
                {
                    Key = prefix
                    ,
                    Value = element == null
                    ,
                    Type = element == null ? (element is string ? typeof(String) : null) : element.GetType()
                };
                result.Add(fo);
            }
            else
            {
                IEnumerable enumerableElement = element as IEnumerable;
                if (enumerableElement != null)
                {
                    foreach (object item in enumerableElement)
                    {
                        if (item is IEnumerable && !(item is string))
                        {
                            if (level < depth)
                            {
                                level++;
                                WriteObject(prefix, item);
                                level--;
                            }
                        }
                        else
                        {
                            WriteObject(prefix, item);
                        }
                    }
                }
                else
                {
                    MemberInfo[] members = element.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);
                    bool propWritten = false;
                    foreach (MemberInfo m in members)
                    {
                        FieldInfo f = m as FieldInfo;
                        PropertyInfo p = m as PropertyInfo;
                        if (f != null || p != null)
                        {
                            if (propWritten)
                            {
                                
                            }
                            else
                            {
                                propWritten = true;
                            }
                            Type t = f != null ? f.FieldType : p.PropertyType;
                            if (t.IsValueType || t == typeof(string))
                            {
                                result.Add(new FlattenedObject
                                {
                                    Key = prefix+m.Name
                                    ,Value = f != null ? f.GetValue(element) : p.GetValue(element, null)!=null?p.GetValue(element, null):null
                                    ,Type = f != null ? Type.GetType(f.FieldType.AssemblyQualifiedName) : Type.GetType(p.PropertyType.AssemblyQualifiedName)
                                    ,Attributes = m.GetCustomAttributes(false)
                                });                                
                            }
                            else
                            {
                                if (typeof(IEnumerable).IsAssignableFrom(t))
                                {
                                    //"..."
                                }
                                else
                                {
                                    //"{ }"
                                }
                            }
                        }
                    }
                    if (level < depth)
                    {
                        foreach (MemberInfo m in members)
                        {
                            FieldInfo f = m as FieldInfo;
                            PropertyInfo p = m as PropertyInfo;
                            if (f != null || p != null)
                            {
                                Type t = f != null ? f.FieldType : p.PropertyType;
                                if (!(t.IsValueType || t == typeof(string)))
                                {
                                    object value = f != null ? f.GetValue(element) : p.GetValue(element, null);
                                    if (value != null)
                                    {
                                        level++;
                                        WriteObject(m.Name + ".", value);
                                        level--;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }               

    }
}
