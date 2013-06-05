using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Web.Mvc;
using puck.core.Abstract;
using puck.core.Attributes;
namespace puck.core.Base
{
    public class BaseModel
    {
        public BaseModel() {
            //Id = Guid.NewGuid();
        }
        [UIHint("SettingsReadOnly")]
        [DefaultGUIDTransformer()]
        public Guid Id { get; set; }

        public String NodeName { get; set; }

        [UIHint("SettingsReadOnly")]
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
        public string Variant { get; set; }

        public bool Published { get; set; }

        [UIHint("SettingsReadOnly")]
        public int SortOrder { get; set; }
        [UIHint("SettingsTemplate")]
        public string TemplatePath { get; set; }

        [UIHint("SettingsReadOnly")]
        [IndexSettings(FieldIndexSetting=Lucene.Net.Documents.Field.Index.ANALYZED,FieldStoreSetting=Lucene.Net.Documents.Field.Store.YES)]
        public string TypeChain { get; set; }
    }
}
