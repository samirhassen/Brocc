namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedApplicationComments : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditApplicationComment",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ApplicationNr = c.String(nullable: false, maxLength: 128),
                    EventType = c.String(maxLength: 100),
                    CommentDate = c.DateTimeOffset(nullable: false, precision: 7),
                    CommentById = c.Int(nullable: false),
                    CommentText = c.String(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditApplicationHeader", t => t.ApplicationNr, cascadeDelete: true)
                .Index(t => t.ApplicationNr);

        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditApplicationComment", "ApplicationNr", "dbo.CreditApplicationHeader");
            DropIndex("dbo.CreditApplicationComment", new[] { "ApplicationNr" });
            DropTable("dbo.CreditApplicationComment");
        }
    }
}
