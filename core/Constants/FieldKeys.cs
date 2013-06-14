using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace puck.core.Constants
{
    public static class FieldKeys
    {
        public static string PuckDefaultField = "";
        public static string PuckValue = "_puckvalue";
        public static string PuckTypeChain = "typechain";
        public static string PuckType = "type";
        public static string ID="id";
        public static string Path="path";
        public static string Variant = "variant";
    }
    public static class DBNames {
        public static string Redirect = "redirect";
        public static string PathToLocale = "pathtolocale";        
        public static string Settings = "settings";
        public static string FieldGroups = "fieldgroups:";
    }
    public static class DBKeys
    {
        public static string Languages = "languages";
        public static string DefaultLanguage = "defaultlanguage";
        public static string EnableLocalePrefix = "enablelocaleprefix";
    }
}
