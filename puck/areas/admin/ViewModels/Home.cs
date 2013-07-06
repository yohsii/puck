using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using puck.areas.admin.Models;
using puck.core.Base;
using puck.core.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Analysis.Snowball;
using puck.core.Models;
namespace puck.areas.admin.ViewModels
{
    public class Home:BaseModel
    {
        [Display(Name="Page Title")]
        [Required]
        [IndexSettings(FieldIndexSetting = Field.Index.ANALYZED,Analyzer=typeof(SnowballAnalyzer))]
        public string PageTitle { get; set; }
        
        [IndexSettings(FieldIndexSetting=Field.Index.ANALYZED,Analyzer=typeof(SnowballAnalyzer))]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        public Person Person { get; set; }
        
        [UIHint("PuckPicker")]
        public List<PuckPicker> Picker { get; set; }

        
    }
}