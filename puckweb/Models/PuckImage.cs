using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using puck.Transformers;

namespace puck.Models
{
    [PuckAzureBlobImageTransformer()]
    public class PuckImage
    {
        [UIHint("SettingsDisplayImage")]
        public string Path { get; set; }
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }
        [UIHint("SettingsReadOnly")]
        public string Size {get;set;}
        [UIHint("SettingsReadOnly")]
        public string Extension { get; set; }
        [UIHint("SettingsReadOnly")]
        public int? Width { get; set; }
        [UIHint("SettingsReadOnly")]
        public int? Height { get; set; }
        public List<CropInfo> Crops { get; set; }
        public HttpPostedFileBase File { get; set; }
    }
}