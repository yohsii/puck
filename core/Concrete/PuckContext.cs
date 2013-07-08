using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;

namespace puck.core.Entities
{
    public class PuckContext : DbContext
    {
        public DbSet<PuckMeta> PuckMeta { get; set; }
        public DbSet<PuckRevision> PuckRevision { get; set; }   
        
    }
}
