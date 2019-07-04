﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace puck.Models
{
    public class CropInfo
    {
        public string Alias { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public float? Left { get; set; }
        public float? Top { get; set; }
        public float? Right { get; set; }
        public float? Bottom { get; set; }
    }
}