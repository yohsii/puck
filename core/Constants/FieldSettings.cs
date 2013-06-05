using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using puck.core.Abstract;
using puck.core.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Analysis;
namespace puck.core.Helpers
{
    public static class FieldSettings
    {
        public static Dictionary<Type,Type> DefaultPropertyTransformers = new Dictionary<Type,Type>
        {
            {typeof(DateTime),typeof(DateTransformer)}
        };
        public static Field.Index FieldIndexSetting = Field.Index.NOT_ANALYZED;
        public static Field.Store FieldStoreSetting = Field.Store.YES;
                
    }
}
