namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedOneTimeTokens : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditApplicationOneTimeToken",
                c => new
                {
                    Token = c.String(nullable: false, maxLength: 60),
                    ApplicationNr = c.String(nullable: false, maxLength: 128),
                    TokenType = c.String(nullable: false, maxLength: 100),
                    TokenExtraData = c.String(),
                    CreationDate = c.DateTimeOffset(nullable: false, precision: 7),
                    ValidUntilDate = c.DateTimeOffset(nullable: false, precision: 7),
                    RemovedDate = c.DateTimeOffset(precision: 7),
                    RemovedBy = c.Int(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Token)
                .ForeignKey("dbo.CreditApplicationHeader", t => t.ApplicationNr, cascadeDelete: true)
                .Index(t => t.ApplicationNr);

            AddColumn("dbo.CreditApplicationItem", "AddedInStepName", c => c.String());
            Sql("update dbo.CreditApplicationItem set AddedInStepName = 'Initial'");
        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditApplicationOneTimeToken", "ApplicationNr", "dbo.CreditApplicationHeader");
            DropIndex("dbo.CreditApplicationOneTimeToken", new[] { "ApplicationNr" });
            DropColumn("dbo.CreditApplicationItem", "AddedInStepName");
            DropTable("dbo.CreditApplicationOneTimeToken");
        }
    }
}
