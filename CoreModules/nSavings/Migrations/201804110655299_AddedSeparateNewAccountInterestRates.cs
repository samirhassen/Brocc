namespace nSavings.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedSeparateNewAccountInterestRates : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SharedSavingsInterestRateChangeHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    BusinessEventId = c.Int(nullable: false),
                    InitiatedAndCreatedByUserId = c.Int(nullable: false),
                    InitiatedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    CreatedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    VerifiedByUserId = c.Int(nullable: false),
                    VerifiedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    AllAccountsRateId = c.Int(),
                    NewAccountsOnlyRateId = c.Int(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.SharedSavingsInterestRate", t => t.AllAccountsRateId)
                .ForeignKey("dbo.SharedSavingsInterestRate", t => t.NewAccountsOnlyRateId)
                .ForeignKey("dbo.BusinessEvent", t => t.BusinessEventId, cascadeDelete: true)
                .Index(t => t.BusinessEventId)
                .Index(t => t.AllAccountsRateId)
                .Index(t => t.NewAccountsOnlyRateId);

            AddColumn("dbo.SharedSavingsInterestRate", "RemovedByBusinessEventId", c => c.Int());
            AddColumn("dbo.SharedSavingsInterestRate", "AppliesToAccountsSinceBusinessEventId", c => c.Int());
            CreateIndex("dbo.SharedSavingsInterestRate", "RemovedByBusinessEventId");
            CreateIndex("dbo.SharedSavingsInterestRate", "AppliesToAccountsSinceBusinessEventId");
            AddForeignKey("dbo.SharedSavingsInterestRate", "AppliesToAccountsSinceBusinessEventId", "dbo.BusinessEvent", "Id");
            AddForeignKey("dbo.SharedSavingsInterestRate", "RemovedByBusinessEventId", "dbo.BusinessEvent", "Id");
        }

        public override void Down()
        {
            DropForeignKey("dbo.SharedSavingsInterestRate", "RemovedByBusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.SharedSavingsInterestRate", "AppliesToAccountsSinceBusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.SharedSavingsInterestRateChangeHeader", "BusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.SharedSavingsInterestRateChangeHeader", "NewAccountsOnlyRateId", "dbo.SharedSavingsInterestRate");
            DropForeignKey("dbo.SharedSavingsInterestRateChangeHeader", "AllAccountsRateId", "dbo.SharedSavingsInterestRate");
            DropIndex("dbo.SharedSavingsInterestRate", new[] { "AppliesToAccountsSinceBusinessEventId" });
            DropIndex("dbo.SharedSavingsInterestRate", new[] { "RemovedByBusinessEventId" });
            DropIndex("dbo.SharedSavingsInterestRateChangeHeader", new[] { "NewAccountsOnlyRateId" });
            DropIndex("dbo.SharedSavingsInterestRateChangeHeader", new[] { "AllAccountsRateId" });
            DropIndex("dbo.SharedSavingsInterestRateChangeHeader", new[] { "BusinessEventId" });
            DropColumn("dbo.SharedSavingsInterestRate", "AppliesToAccountsSinceBusinessEventId");
            DropColumn("dbo.SharedSavingsInterestRate", "RemovedByBusinessEventId");
            DropTable("dbo.SharedSavingsInterestRateChangeHeader");
        }
    }
}
