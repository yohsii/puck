using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace puck.core.Entities
{
    public partial class PuckContext : IdentityDbContext<PuckUser>
    {
        public PuckContext()
            : base("PuckContext", throwIfV1Schema: false)
        {
        }

        public static PuckContext Create()
        {
            return new PuckContext();
        }

        public DbSet<PuckMeta> PuckMeta { get; set; }
        public DbSet<PuckRevision> PuckRevision { get; set; }
        public DbSet<GeneratedModel> GeneratedModel { get; set; }
        public DbSet<GeneratedProperty> GeneratedProperty { get; set; }
        public DbSet<GeneratedAttribute> GeneratedAttribute { get; set; }
        public DbSet<PuckInstruction> PuckInstruction { get; set; }
        public DbSet<PuckAudit> PuckAudit { get; set; }
    }
}
