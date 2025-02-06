namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedTableApplicationReportCasheRow : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ApplicationReportCasheRow",
                c => new
                {
                    ApplicationNr = c.String(nullable: false, maxLength: 128),
                    ApplicationDate = c.DateTime(nullable: false),
                    Overrided = c.String(),
                    Handler = c.String(),
                    SysRecomendation = c.String(),
                    SysRecomendationMaxAmount = c.Decimal(precision: 18, scale: 2),
                    SysRecomendationInterestRate = c.Decimal(precision: 18, scale: 2),
                    SysRecomendationomendationAmount = c.Decimal(precision: 18, scale: 2),
                    SysRecomendationomendationRepaymentTime = c.Int(),
                    SysRecomendationNotificationFee = c.Decimal(precision: 18, scale: 2),
                    SysRecomendationRejectionReasons = c.String(),
                    Decision = c.String(),
                    DecisionInterestRate = c.Decimal(precision: 18, scale: 2),
                    DecisionAmount = c.Decimal(precision: 18, scale: 2),
                    DecisionRepaymentTime = c.Int(),
                    DecisionNotificationFee = c.Decimal(precision: 18, scale: 2),
                    DecisionRejectionReasons = c.String(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.ApplicationNr);

        }

        public override void Down()
        {
            DropTable("dbo.ApplicationReportCasheRow");
        }
    }
}
