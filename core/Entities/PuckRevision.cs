using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using puck.core.Base;
using puck.core.Attributes;

namespace puck.core.Entities
{
    public class PuckRevision:BaseModel
    {
        [Key]
        [IndexSettings(Ignore=false)]
        public int RevisionID { get; set; }
        [IndexSettings(Ignore = false)]
        public bool Current { get; set; }
        [IndexSettings(Ignore = false)]
        public string Value { get; set; }
        public bool HasNoPublishedRevision { get; set; }
        public bool IsPublishedRevision { get; set; }
        public string IdPath { get; set; }
    }
}
