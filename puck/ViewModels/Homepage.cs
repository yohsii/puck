using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.WebPages;
using puck.core.Models;

namespace puck.areas.admin.ViewModels
{
    public class Homepage:Page
    {
        [Display(Name="Carousel Items")]
        [UIHint("PuckPicker")]
        public List<PuckPicker> CarouselItems { get; set; }
    }
}