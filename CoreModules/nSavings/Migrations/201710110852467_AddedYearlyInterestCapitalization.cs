namespace nSavings.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedYearlyInterestCapitalization : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SavingsAccountInterestCapitalization",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    FromDate = c.DateTime(nullable: false, storeType: "date"),
                    ToDate = c.DateTime(nullable: false, storeType: "date"),
                    SavingsAccountNr = c.String(nullable: false, maxLength: 128),
                    CalculationDetailsDocumentArchiveKey = c.String(maxLength: 100),
                    CreatedByBusinessEventId = c.Int(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.SavingsAccountHeader", t => t.SavingsAccountNr, cascadeDelete: true)
                .ForeignKey("dbo.BusinessEvent", t => t.CreatedByBusinessEventId, cascadeDelete: true)
                .Index(t => new { t.SavingsAccountNr, t.ToDate }, unique: true, name: "InterestCapitalizationDuplicateGuardIndex")
                .Index(t => t.CreatedByBusinessEventId);

        }

        public override void Down()
        {
            DropForeignKey("dbo.SavingsAccountInterestCapitalization", "CreatedByBusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.SavingsAccountInterestCapitalization", "SavingsAccountNr", "dbo.SavingsAccountHeader");
            DropIndex("dbo.SavingsAccountInterestCapitalization", new[] { "CreatedByBusinessEventId" });
            DropIndex("dbo.SavingsAccountInterestCapitalization", "InterestCapitalizationDuplicateGuardIndex");
            DropTable("dbo.SavingsAccountInterestCapitalization");
        }
    }
}
