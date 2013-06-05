using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace puck.core.Entities
{
    public class PuckRevision
    {
        [Key]
        public int ID { get; set; }
        public string Variant { get; set; }
        public Guid GUID { get; set; }
        public string Value { get; set; }
        public string Type { get; set; }
    }
}
