namespace nSavings.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedSavingsAccountDocument : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SavingsAccountDocument",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    SavingsAccountNr = c.String(nullable: false, maxLength: 128),
                    DocumentType = c.String(nullable: false, maxLength: 100),
                    DocumentData = c.String(),
                    DocumentDate = c.DateTimeOffset(nullable: false, precision: 7),
                    DocumentArchiveKey = c.String(maxLength: 100),
                    CreatedByBusinessEventId = c.Int(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.SavingsAccountHeader", t => t.SavingsAccountNr, cascadeDelete: true)
                .ForeignKey("dbo.BusinessEvent", t => t.CreatedByBusinessEventId, cascadeDelete: true)
                .Index(t => t.SavingsAccountNr)
                .Index(t => t.CreatedByBusinessEventId);

        }

        public override void Down()
        {
            DropForeignKey("dbo.SavingsAccountDocument", "CreatedByBusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.SavingsAccountDocument", "SavingsAccountNr", "dbo.SavingsAccountHeader");
            DropIndex("dbo.SavingsAccountDocument", new[] { "CreatedByBusinessEventId" });
            DropIndex("dbo.SavingsAccountDocument", new[] { "SavingsAccountNr" });
            DropTable("dbo.SavingsAccountDocument");
        }
    }
}
