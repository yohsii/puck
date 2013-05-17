using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace puck.core.Entities
{
    public class Model
    {
        [Key]
        public int Id { get; set; }
        
        public string Name { get; set; }

        
    }
}
