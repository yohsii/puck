using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using puck.areas.admin.Models;
using puck.core.Base;

namespace puck.ViewModels
{
    public class ImageModel:BaseModel
    {
        public PuckImage Image { get; set; }
    }
}