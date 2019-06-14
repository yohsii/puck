using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.WebPages;
using puck.core.Models;
using puck.ViewModels;
//using puck.areas.admin.ViewModels;
namespace puck.ViewModels
{
    public class Homepage:Page
    {
        [Display(Name="Carousel Items")]
        [UIHint("PuckPicker")]
        public List<PuckPicker> CarouselItems { get; set; }
    }
}