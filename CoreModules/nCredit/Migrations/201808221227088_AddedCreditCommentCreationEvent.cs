namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCreditCommentCreationEvent : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.IncomingDirectDebitStatusChangeFileHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    FileArchiveKey = c.String(maxLength: 128),
                    Filename = c.String(maxLength: 128),
                    CreatedByEventId = c.Int(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.BusinessEvent", t => t.CreatedByEventId, cascadeDelete: true)
                .Index(t => t.CreatedByEventId);

            AddColumn("dbo.CreditComment", "CreatedByEventId", c => c.Int());
            CreateIndex("dbo.CreditComment", "CreatedByEventId");
            AddForeignKey("dbo.CreditComment", "CreatedByEventId", "dbo.BusinessEvent", "Id");
        }

        public override void Down()
        {
            DropForeignKey("dbo.IncomingDirectDebitStatusChangeFileHeader", "CreatedByEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.CreditComment", "CreatedByEventId", "dbo.BusinessEvent");
            DropIndex("dbo.IncomingDirectDebitStatusChangeFileHeader", new[] { "CreatedByEventId" });
            DropIndex("dbo.CreditComment", new[] { "CreatedByEventId" });
            DropColumn("dbo.CreditComment", "CreatedByEventId");
            DropTable("dbo.IncomingDirectDebitStatusChangeFileHeader");
        }
    }
}
