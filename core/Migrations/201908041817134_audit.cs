namespace puck.core.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class audit : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PuckAudits",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ContentId = c.Guid(nullable: false),
                        Variant = c.String(),
                        Action = c.String(),
                        Username = c.String(),
                        Notes = c.String(),
                        Timestamp = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            AddColumn("dbo.PuckMetas", "Username", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.PuckMetas", "Username");
            DropTable("dbo.PuckAudits");
        }
    }
}
