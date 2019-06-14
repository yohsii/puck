using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Security;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity;
using System.Threading.Tasks;
using System.Security.Claims;

namespace puck.core.Entities
{
    public class PuckUser : IdentityUser
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<PuckUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
        public PuckUser() {
            StartNodeId = Guid.Empty;
        }

        [UIHint("SettingsUserVariant")]
        [Display(Name="User Language")]
        public string UserVariant { get; set;}

        public Guid StartNodeId { get; set; }

        //public string StartNodeVariant { get; set; }
        
    } 
    
}
