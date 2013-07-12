using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Highlight;
using puck.core.Base;
using puck.core.Helpers;
using puck.core.Models;

namespace puck.core.Extensions
{
    public static class ExtensionsMisc
    {
        public static int GetLevel(this BaseModel m) {
            int level = m.Path.Count(x=>x=='/');
            return level;
        }



        public static List<T> GetAll<T>(this PuckPicker pp) where T : BaseModel
        {
            var qh = new QueryHelper<T>();
            qh.And().ID(pp.Id);
            if (!string.IsNullOrEmpty(pp.Variant))
                qh.Variant(pp.Variant);
            return qh.GetAll();
        }

        public static T Get<T>(this PuckPicker pp) where T: BaseModel {
            var qh = new QueryHelper<T>();
            qh.And().ID(pp.Id);
            if (!string.IsNullOrEmpty(pp.Variant))
                qh.Variant(pp.Variant);
            return qh.Get();
        }
        public static string Highlight(this string text,string term)
        {
            var bq = new BooleanQuery();
            term.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList()
                .ForEach(x => bq.Add(new TermQuery(new Term("field", x)), Occur.SHOULD));
            var fragmentLength = 100;
            var highlightStartTag = @"<span class='search_highlight'>";
            var highlightEndTag = @"</span>";
            QueryScorer scorer = new QueryScorer(bq);
            var formatter = new SimpleHTMLFormatter(highlightStartTag, highlightEndTag);
            Highlighter highlighter = new Highlighter(formatter, scorer);
            highlighter.TextFragmenter = new SimpleFragmenter(fragmentLength);
            TokenStream stream = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29).TokenStream("field", new StringReader(text));
            return highlighter.GetBestFragments(stream, text, 100, "...");
        }
    }
}
