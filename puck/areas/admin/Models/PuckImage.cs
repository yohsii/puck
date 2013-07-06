using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using puck.Transformers;

namespace puck.areas.admin.Models
{
    [PuckImageTransformer()]
    public class PuckImage
    {
        public string ImageSize {get;set;}
        public string ImagePath { get; set; }
        public string Extension { get; set; }
        public HttpPostedFileBase File { get; set; }
    }
}