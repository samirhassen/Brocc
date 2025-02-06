namespace nCredit.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedPaymentPlans : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AlternatePaymentPlanHeader",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CreditNr = c.String(nullable: false, maxLength: 128),
                        CreatedByEventId = c.Int(nullable: false),
                        CancelledByEventId = c.Int(),
                        FullyPaidByEventId = c.Int(),
                        MinCapitalizedDueDate = c.DateTime(storeType: "date"),
                        FuturePaymentPlanMonthCount = c.Int(nullable: false),
                        Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                        ChangedById = c.Int(nullable: false),
                        ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                        InformationMetaData = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditHeader", t => t.CreditNr, cascadeDelete: true)
                .ForeignKey("dbo.BusinessEvent", t => t.CancelledByEventId)
                .ForeignKey("dbo.BusinessEvent", t => t.CreatedByEventId, cascadeDelete: true)
                .ForeignKey("dbo.BusinessEvent", t => t.FullyPaidByEventId)
                .Index(t => t.CreditNr)
                .Index(t => t.CreatedByEventId)
                .Index(t => t.CancelledByEventId)
                .Index(t => t.FullyPaidByEventId);
            
            CreateTable(
                "dbo.AlternatePaymentPlanMonth",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AlternatePaymentPlanId = c.Int(nullable: false),
                        DueDate = c.DateTime(nullable: false, storeType: "date"),
                        MonthAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TotalAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                        ChangedById = c.Int(nullable: false),
                        ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                        InformationMetaData = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AlternatePaymentPlanHeader", t => t.AlternatePaymentPlanId, cascadeDelete: true)
                .Index(t => new { t.AlternatePaymentPlanId, t.DueDate }, unique: true);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AlternatePaymentPlanMonth", "AlternatePaymentPlanId", "dbo.AlternatePaymentPlanHeader");
            DropForeignKey("dbo.AlternatePaymentPlanHeader", "FullyPaidByEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.AlternatePaymentPlanHeader", "CreatedByEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.AlternatePaymentPlanHeader", "CancelledByEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.AlternatePaymentPlanHeader", "CreditNr", "dbo.CreditHeader");
            DropIndex("dbo.AlternatePaymentPlanMonth", new[] { "AlternatePaymentPlanId", "DueDate" });
            DropIndex("dbo.AlternatePaymentPlanHeader", new[] { "FullyPaidByEventId" });
            DropIndex("dbo.AlternatePaymentPlanHeader", new[] { "CancelledByEventId" });
            DropIndex("dbo.AlternatePaymentPlanHeader", new[] { "CreatedByEventId" });
            DropIndex("dbo.AlternatePaymentPlanHeader", new[] { "CreditNr" });
            DropTable("dbo.AlternatePaymentPlanMonth");
            DropTable("dbo.AlternatePaymentPlanHeader");
        }
    }
}
