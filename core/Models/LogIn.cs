using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace puck.core.Models
{
    public class LogIn
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool PersistentCookie { get; set; }
        public string Error { get;set;}
    }
}
