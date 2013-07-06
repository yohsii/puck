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
        public string UserName { get; set; }
        //[System.ComponentModel.DataAnnotations.em]
        public string Email { get; set; }
        public List<string> Roles { get; set; }
        public List<PuckPicker> StartNode { get; set; }
        [Required]
        public string Password { get; set; }
        [System.Web.Mvc.Compare("Password")]
        public string PasswordConfirm { get; set; }
    }
}
