using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace puck.core.Entities
{
    public class PuckTask
    {
        [Key]
        public int ID { get; set; }
        public string Type {get;set;}
    }
}
