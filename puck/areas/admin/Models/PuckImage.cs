using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using puck.Transformers;

namespace puck.areas.admin.Models
{
    [PuckImageTransformer()]
    public class PuckImage
    {
        [UIHint("SettingsReadOnly")]
        public string Path { get; set; }
        [UIHint("SettingsReadOnly")]
        public string Size {get;set;}
        [UIHint("SettingsReadOnly")]
        public string Extension { get; set; }
        public HttpPostedFileBase File { get; set; }
    }
}