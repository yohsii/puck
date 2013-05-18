using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Documents;
using puck.core.Helpers;
using Lucene.Net.Analysis;
namespace puck.core.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class IndexSettings:Attribute
    {
        private Field.Store _FieldStoreSetting = FieldSettings.FieldStoreSetting;
        public Field.Store FieldStoreSetting { 
            get{
                return _FieldStoreSetting;
            } set{_FieldStoreSetting=value;} }

        private Field.Index _FieldIndexSetting = FieldSettings.FieldIndexSetting;
        public Field.Index FieldIndexSetting {
            get {
                return _FieldIndexSetting;
            }
            set { _FieldIndexSetting = value; }
        }
        public Analyzer Analyzer { get; set; }
        
    }
}
