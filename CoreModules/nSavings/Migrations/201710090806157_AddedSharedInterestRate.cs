namespace nSavings.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedSharedInterestRate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SharedSavingsInterestRate",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    AccountTypeCode = c.String(nullable: false, maxLength: 100),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    ValidFromDate = c.DateTime(nullable: false, storeType: "date"),
                    BusinessEventId = c.Int(nullable: false),
                    InterestRatePercent = c.Decimal(nullable: false, precision: 18, scale: 2),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.BusinessEvent", t => t.BusinessEventId, cascadeDelete: true)
                .Index(t => t.BusinessEventId);

            AddColumn("dbo.SavingsAccountHeader", "AccountTypeCode", c => c.String(nullable: false, maxLength: 100));
        }

        public override void Down()
        {
            DropForeignKey("dbo.SharedSavingsInterestRate", "BusinessEventId", "dbo.BusinessEvent");
            DropIndex("dbo.SharedSavingsInterestRate", new[] { "BusinessEventId" });
            DropColumn("dbo.SavingsAccountHeader", "AccountTypeCode");
            DropTable("dbo.SharedSavingsInterestRate");
        }
    }
}
