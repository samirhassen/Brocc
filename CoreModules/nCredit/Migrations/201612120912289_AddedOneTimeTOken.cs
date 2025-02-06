namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedOneTimeTOken : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.OneTimeToken",
                c => new
                {
                    Token = c.String(nullable: false, maxLength: 60),
                    TokenType = c.String(nullable: false, maxLength: 100),
                    TokenExtraData = c.String(),
                    CreationDate = c.DateTimeOffset(nullable: false, precision: 7),
                    CreatedBy = c.Int(nullable: false),
                    ValidUntilDate = c.DateTimeOffset(nullable: false, precision: 7),
                    RemovedDate = c.DateTimeOffset(precision: 7),
                    RemovedBy = c.Int(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Token);

        }

        public override void Down()
        {
            DropTable("dbo.OneTimeToken");
        }
    }
}
