using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace puck.areas.admin.Models
{
    public class Person
    {
        public int Age { get; set; }
        public string Name { get; set; }
        
        public PuckImage Image { get; set; }

        public TextSingle TextGroup { get; set; }
    }
}