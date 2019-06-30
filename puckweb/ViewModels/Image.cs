using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using puck.core.Attributes;
using puck.core.Base;
using puck.Models;
namespace puck.ViewModels
{
    [FriendlyClassName(Name="Image")]
    public class ImageModel:BaseModel
    {
        public PuckImage Image { get; set; }
    }
}