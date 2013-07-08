namespace puck.core.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class indexes : DbMigration
    {
        public override void Up()
        {
            CreateIndex("PuckRevisions", "Path");
            CreateIndex("PuckRevisions", "Id");
            CreateIndex("PuckRevisions", "RevisionID");
            CreateIndex("PuckRevisions", "Variant");
        }
        
        public override void Down()
        {
            DropIndex("PuckRevisions", "Path");
            DropIndex("PuckRevisions", "Id");
            DropIndex("PuckRevisions", "RevisionID");
            DropIndex("PuckRevisions", "Variant");
        }
    }
}
