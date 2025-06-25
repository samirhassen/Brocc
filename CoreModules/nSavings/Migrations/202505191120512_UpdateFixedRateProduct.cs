namespace nSavings.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateFixedRateProduct : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.FixedAccountProduct",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Name = c.String(nullable: false, maxLength: 100),
                        InterestRatePercent = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TermInMonths = c.Int(nullable: false),
                        ValidFrom = c.DateTime(nullable: false, storeType: "date"),
                        ValidTo = c.DateTime(storeType: "date"),
                        ApprovedAt = c.DateTime(),
                        ApprovedBy = c.String(),
                        CreatedAt = c.DateTime(nullable: false, storeType: "date"),
                        CreatedBy = c.String(nullable: false),
                        Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                        ChangedById = c.Int(nullable: false),
                        ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                        InformationMetaData = c.String(),
                        ApprovedAtBusinessEvent_Id = c.Int(),
                        CreatedAtBusinessEvent_Id = c.Int(),
                        UpdatedAtBusinessEvent_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.BusinessEvent", t => t.ApprovedAtBusinessEvent_Id)
                .ForeignKey("dbo.BusinessEvent", t => t.CreatedAtBusinessEvent_Id)
                .ForeignKey("dbo.BusinessEvent", t => t.UpdatedAtBusinessEvent_Id)
                .Index(t => t.ApprovedAtBusinessEvent_Id)
                .Index(t => t.CreatedAtBusinessEvent_Id)
                .Index(t => t.UpdatedAtBusinessEvent_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.FixedAccountProduct", "UpdatedAtBusinessEvent_Id", "dbo.BusinessEvent");
            DropForeignKey("dbo.FixedAccountProduct", "CreatedAtBusinessEvent_Id", "dbo.BusinessEvent");
            DropForeignKey("dbo.FixedAccountProduct", "ApprovedAtBusinessEvent_Id", "dbo.BusinessEvent");
            DropIndex("dbo.FixedAccountProduct", new[] { "UpdatedAtBusinessEvent_Id" });
            DropIndex("dbo.FixedAccountProduct", new[] { "CreatedAtBusinessEvent_Id" });
            DropIndex("dbo.FixedAccountProduct", new[] { "ApprovedAtBusinessEvent_Id" });
            DropTable("dbo.FixedAccountProduct");
        }
    }
}
