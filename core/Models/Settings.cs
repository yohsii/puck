using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace puck.core.Models
{
    public class Settings
    {
        [UIHint("SettingsDefaultLanguage")]
        public String DefaultLanguage { get; set; }
        
        [UIHint("SettingsLanguages")]
        public List<String> Languages { get; set; }

        [UIHint("SettingsRedirect")]
        public Dictionary<string, string> Redirect { get; set; }

        public Dictionary<string, string> PathToLocale { get; set; }

        public bool EnableLocalePrefix { get; set; }
    }
}
