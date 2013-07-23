using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Util;
using puck.core.Base;
using puck.core.Constants;

namespace puck.core.PuckLucene
{
    public class PuckQueryParser<T>:QueryParser where T : BaseModel
    {
        public static List<string> NumericFieldTypes = new List<string>() { 
            typeof(int).AssemblyQualifiedName,typeof(long).AssemblyQualifiedName,typeof(double).AssemblyQualifiedName,typeof(float).AssemblyQualifiedName
        };
        private string TypeName = typeof(T).AssemblyQualifiedName;
        public PuckQueryParser(Lucene.Net.Util.Version version, string field, Analyzer analyzer) 
            : base(version, field, analyzer) { 
        }
        protected override Query NewTermQuery(Lucene.Net.Index.Term term)
        {
            try
            {
                string fieldTypeName = PuckCache.TypeFields[TypeName][term.Field];
                if (fieldTypeName.Equals(typeof(int).AssemblyQualifiedName))
                {
                    return new TermQuery(new Term(term.Field,NumericUtils.IntToPrefixCoded(int.Parse(term.Text))));
                }
                else if (fieldTypeName.Equals(typeof(long).AssemblyQualifiedName))
                {
                    return new TermQuery(new Term(term.Field, NumericUtils.LongToPrefixCoded(long.Parse(term.Text))));
                }
                else if (fieldTypeName.Equals(typeof(float).AssemblyQualifiedName))
                {
                    return new TermQuery(new Term(term.Field, NumericUtils.FloatToPrefixCoded(float.Parse(term.Text))));
                }
                else if (fieldTypeName.Equals(typeof(double).AssemblyQualifiedName))
                {
                    return new TermQuery(new Term(term.Field, NumericUtils.DoubleToPrefixCoded(double.Parse(term.Text))));
                }
            }
            catch (Exception ex)
            {

            }
            return base.NewTermQuery(term);
        }
        protected override Lucene.Net.Search.Query GetRangeQuery(string field, string part1, string part2, bool inclusive)
        {
            try
            {
                string fieldTypeName = PuckCache.TypeFields[TypeName][field];
                if (fieldTypeName.Equals(typeof(int).AssemblyQualifiedName))
                {
                    return NumericRangeQuery.NewIntRange(field, int.Parse(part1), int.Parse(part2), inclusive, inclusive);
                }
                else if (fieldTypeName.Equals(typeof(long).AssemblyQualifiedName))
                {
                    return NumericRangeQuery.NewLongRange(field, long.Parse(part1), long.Parse(part2), inclusive, inclusive);
                }
                else if (fieldTypeName.Equals(typeof(float).AssemblyQualifiedName))
                {
                    return NumericRangeQuery.NewFloatRange(field, float.Parse(part1), float.Parse(part2), inclusive, inclusive);
                }
                else if (fieldTypeName.Equals(typeof(double).AssemblyQualifiedName))
                {
                    return NumericRangeQuery.NewDoubleRange(field, double.Parse(part1), double.Parse(part2), inclusive, inclusive);
                }
            }
            catch (Exception ex) { 
            
            }
            return base.GetRangeQuery(field, part1, part2, inclusive);
        }

    }
}
