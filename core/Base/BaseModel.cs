using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Web.Mvc;
using puck.core.Abstract;
using puck.core.Attributes;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
namespace puck.core.Base
{
    public class BaseModel
    {
        public BaseModel() {
            Created = DateTime.Now;
            Updated = DateTime.Now;
            Id = Guid.NewGuid();
            Revision = 0;
            SortOrder = -1;                
        }
        [UIHint("SettingsReadOnly")]
        [DefaultGUIDTransformer()]
        [IndexSettings(FieldIndexSetting=Lucene.Net.Documents.Field.Index.NOT_ANALYZED,Analyzer=typeof(KeywordAnalyzer))]
        public Guid Id { get; set; }

        public String NodeName { get; set; }

        [UIHint("SettingsReadOnly")]
        [IndexSettings(FieldIndexSetting = Lucene.Net.Documents.Field.Index.NOT_ANALYZED, Analyzer = typeof(KeywordAnalyzer))]
        public string Path { get; set; }
        
        [DateTransformer()]
        [UIHint("SettingsReadOnly")]
        public DateTime Created { get; set; }

        [DateTransformer()]
        [UIHint("SettingsReadOnly")]
        public DateTime Updated { get; set; }

        [UIHint("SettingsReadOnly")]
        public int Revision { get; set; }

        [UIHint("SettingsReadOnly")]
        [IndexSettings(FieldIndexSetting = Lucene.Net.Documents.Field.Index.NOT_ANALYZED, Analyzer = typeof(KeywordAnalyzer))]
        public string Variant { get; set; }

        public bool Published { get; set; }

        [UIHint("SettingsReadOnly")]
        public int SortOrder { get; set; }
        [UIHint("SettingsTemplate")]
        [IndexSettings(FieldIndexSetting = Lucene.Net.Documents.Field.Index.NOT_ANALYZED, Analyzer = typeof(KeywordAnalyzer))]
        public string TemplatePath { get; set; }

        [UIHint("SettingsReadOnly")]
        [IndexSettings(FieldIndexSetting=Lucene.Net.Documents.Field.Index.ANALYZED,Analyzer=typeof(StandardAnalyzer),FieldStoreSetting=Lucene.Net.Documents.Field.Store.YES)]
        public string TypeChain { get; set; }

        [UIHint("SettingsReadOnly")]
        [IndexSettings(FieldIndexSetting = Lucene.Net.Documents.Field.Index.ANALYZED, Analyzer = typeof(KeywordAnalyzer), FieldStoreSetting = Lucene.Net.Documents.Field.Store.YES)]
        public string Type { get; set; }
    }
}
