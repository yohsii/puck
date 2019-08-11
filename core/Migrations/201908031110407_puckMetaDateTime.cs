namespace puck.core.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class puckMetaDateTime : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PuckMetas", "Dt", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.PuckMetas", "Dt");
        }
    }
}
