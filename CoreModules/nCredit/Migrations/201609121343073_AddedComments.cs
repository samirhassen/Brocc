namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedComments : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditComment",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CreditNr = c.String(nullable: false, maxLength: 128),
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
                .ForeignKey("dbo.CreditHeader", t => t.CreditNr, cascadeDelete: true)
                .Index(t => t.CreditNr);

        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditComment", "CreditNr", "dbo.CreditHeader");
            DropIndex("dbo.CreditComment", new[] { "CreditNr" });
            DropTable("dbo.CreditComment");
        }
    }
}
