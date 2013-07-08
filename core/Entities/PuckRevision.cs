using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using puck.core.Base;

namespace puck.core.Entities
{
    public class PuckRevision:BaseModel
    {
        [Key]
        public int RevisionID { get; set; }
        public string LastEditedBy { get; set; }
        public string CreatedBy { get; set; }
        public bool Current { get; set; }
        public string Value { get; set; }
    }
}
