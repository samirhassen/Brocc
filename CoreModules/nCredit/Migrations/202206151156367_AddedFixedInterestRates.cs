namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedFixedInterestRates : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.FixedMortgageLoanInterestRate",
                c => new
                {
                    MonthCount = c.Int(nullable: false),
                    CreatedByBusinessEventId = c.Int(nullable: false),
                    RatePercent = c.Decimal(nullable: false, precision: 18, scale: 2),
                })
                .PrimaryKey(t => t.MonthCount)
                .ForeignKey("dbo.BusinessEvent", t => t.CreatedByBusinessEventId, cascadeDelete: true)
                .Index(t => t.CreatedByBusinessEventId);

            CreateTable(
                "dbo.HFixedMortgageLoanInterestRate",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    MonthCount = c.Int(nullable: false),
                    CreatedByBusinessEventId = c.Int(nullable: false),
                    RatePercent = c.Decimal(nullable: false, precision: 18, scale: 2),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.BusinessEvent", t => t.CreatedByBusinessEventId, cascadeDelete: true)
                .Index(t => new { t.CreatedByBusinessEventId, t.MonthCount }, unique: true);

        }

        public override void Down()
        {
            DropForeignKey("dbo.HFixedMortgageLoanInterestRate", "CreatedByBusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.FixedMortgageLoanInterestRate", "CreatedByBusinessEventId", "dbo.BusinessEvent");
            DropIndex("dbo.HFixedMortgageLoanInterestRate", new[] { "CreatedByBusinessEventId", "MonthCount" });
            DropIndex("dbo.FixedMortgageLoanInterestRate", new[] { "CreatedByBusinessEventId" });
            DropTable("dbo.HFixedMortgageLoanInterestRate");
            DropTable("dbo.FixedMortgageLoanInterestRate");
        }
    }
}
