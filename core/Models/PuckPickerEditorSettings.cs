using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using puck.core.Abstract;
using System.ComponentModel.DataAnnotations;

namespace puck.core.Models
{
    public enum PuckPickerSelectionType { node, variant, both };
    
    public class PuckPickerEditorSettings:I_Puck_Editor_Settings
    {   
        [UIHint("PuckPicker")]
        public List<PuckPicker> StartPath { get; set; }
        public int MaxPick { get; set; }
        [UIHint("PuckPickerSelectionType")]
        public string SelectionType { get; set; }
        public bool AllowUnpublished { get; set; }
        public bool AllowDuplicates { get; set; }
    }
}
