namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedKycHeader : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.DailyKycScreenHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    NrOfCustomersScreened = c.Int(nullable: false),
                    NrOfCustomersConflicted = c.Int(nullable: false),
                    ResultModel = c.String(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id);

        }

        public override void Down()
        {
            DropTable("dbo.DailyKycScreenHeader");
        }
    }
}
