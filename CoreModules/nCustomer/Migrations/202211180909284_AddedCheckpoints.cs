namespace nCustomer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedCheckpoints : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CustomerCheckpoint",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CustomerId = c.Int(nullable: false),
                        IsCurrentState = c.Boolean(nullable: false),
                        ReasonText = c.String(),
                        StateDate = c.DateTime(nullable: false),
                        StateBy = c.Int(nullable: false),
                        Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                        ChangedById = c.Int(nullable: false),
                        ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                        InformationMetaData = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.CustomerId);
            
            CreateTable(
                "dbo.CustomerCheckpointCode",
                c => new
                    {
                        CustomerCheckpointId = c.Int(nullable: false),
                        Code = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.CustomerCheckpointId, t.Code })
                .ForeignKey("dbo.CustomerCheckpoint", t => t.CustomerCheckpointId, cascadeDelete: true)
                .Index(t => t.CustomerCheckpointId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.CustomerCheckpointCode", "CustomerCheckpointId", "dbo.CustomerCheckpoint");
            DropIndex("dbo.CustomerCheckpointCode", new[] { "CustomerCheckpointId" });
            DropIndex("dbo.CustomerCheckpoint", new[] { "CustomerId" });
            DropTable("dbo.CustomerCheckpointCode");
            DropTable("dbo.CustomerCheckpoint");
        }
    }
}
