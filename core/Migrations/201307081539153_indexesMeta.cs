namespace puck.core.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class indexesMeta : DbMigration
    {
        public override void Up()
        {
            CreateIndex("PuckMetas", "Name");
            CreateIndex("PuckMetas", "Key");            
        }

        public override void Down()
        {
            DropIndex("PuckMetas", "Name");
            DropIndex("PuckMetas", "Key");            
        }
    }
}
