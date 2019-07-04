using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Lucene.Net.Analysis.Snowball;
using Lucene.Net.Documents;
using puck.core.Attributes;
using puck.core.Base;
using puck.core.Models;
using puck.Models;

namespace puck.ViewModels
{
    public class Page:BaseModel
    {
        public List<string> Names { get { return new List<string> { "name1", "name2" }; } }
        
        [UIHint("PuckImage")]
        public PuckImage Image { get; set; }

        [Required]
        [Display(Name = "Keywords")]
        [IndexSettings(FieldIndexSetting = Field.Index.ANALYZED, Analyzer = typeof(SnowballAnalyzer))]
        public string MetaKeywords { get; set; }
        
        [Required]
        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        [IndexSettings(FieldIndexSetting = Field.Index.ANALYZED, Analyzer = typeof(SnowballAnalyzer))]
        public string MetaDescription { get; set; }
        
        [Required]
        [IndexSettings(FieldIndexSetting = Field.Index.ANALYZED, Analyzer = typeof(SnowballAnalyzer))]
        [Display(Description="enter a description here")]
        public string Title { get; set; }
        
        [Required]
        [UIHint("rte")]
        [Display(Name="Main Content")]
        [IndexSettings(FieldIndexSetting = Field.Index.ANALYZED, Analyzer = typeof(SnowballAnalyzer))]
        public string MainContent { get; set; }

        
        [UIHint("PuckGoogleLongLat")]
        public GeoPosition Location { get; set; }                
    }
}