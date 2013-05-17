using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using puck.core.Abstract;
using puck.core.Entities;
namespace puck.core.Concrete
{
    public class Puck_Repository : I_Puck_Repository
    {
        public PuckContext context = new PuckContext();

        public void SaveChanges() {
            context.SaveChanges();
        }
    }
}
