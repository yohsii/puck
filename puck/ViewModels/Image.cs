using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using puck.areas.admin.Models;
using puck.core.Attributes;
using puck.core.Base;

namespace puck.ViewModels
{
    [FriendlyClassName(Name="Image")]
    public class ImageModel:BaseModel
    {
        public PuckImage Image { get; set; }
    }
}