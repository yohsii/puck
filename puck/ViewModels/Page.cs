using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Lucene.Net.Analysis.Snowball;
using Lucene.Net.Documents;
using puck.areas.admin.Models;
using puck.core.Attributes;
using puck.core.Base;
using puck.core.Models;

namespace puck.areas.admin.ViewModels
{
    public class Page:BaseModel
    {
        [Required]
        [Display(Name = "Keywords")]
        [IndexSettings(FieldIndexSetting = Field.Index.ANALYZED, Analyzer = typeof(SnowballAnalyzer))]
        public string MetaKeywords { get; set; }
        
        [Required]
        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string MetaDescription { get; set; }
        
        [Required]
        public string Title { get; set; }
        
        [Required]
        [UIHint("rte")]
        [Display(Name="Main Content")]
        [IndexSettings(FieldIndexSetting = Field.Index.ANALYZED, Analyzer = typeof(SnowballAnalyzer))]
        public string MainContent { get; set; }
        
        public PuckImage Image { get; set; }

        public GeoPosition Location { get; set; }                
    }
}