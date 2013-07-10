using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Security;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;

namespace puck.core.Models
{
    public class PuckUser
    {
        public MembershipUser User { get; set; }
        
        [Required]
        public string UserName { get; set; }
        
        [System.ComponentModel.DataAnnotations.EmailAddress]
        [Required]
        public string Email { get; set; }
        
        [UIHint("SettingsRoles")]
        public List<string> Roles { get; set; }

        [UIHint("SettingsUserVariant")]
        [Display(Name="User Language")]
        public string UserVariant { get; set;}

        [UIHint("PuckPicker")]
        public List<PuckPicker> StartNode { get; set; }
        
        [Required]
        public string Password { get; set; }
        
        [Display(Name="Confirm Password")]
        [System.Web.Mvc.Compare("Password")]
        public string PasswordConfirm { get; set; }

        [Display(Name="Password")]
        public string NewPassword { get; set; }
        
        [Display(Name = "Confirm Password")]
        [System.Web.Mvc.Compare("NewPassword")]
        public string NewPasswordConfirm { get; set; }

    }
}
