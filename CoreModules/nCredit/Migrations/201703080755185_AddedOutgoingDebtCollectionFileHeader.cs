namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedOutgoingDebtCollectionFileHeader : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.OutgoingDebtCollectionFileHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ExternalId = c.String(nullable: false, maxLength: 128),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    FileArchiveKey = c.String(maxLength: 100),
                    XlsFileArchiveKey = c.String(maxLength: 100),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .Index(t => t.ExternalId);

        }

        public override void Down()
        {
            DropIndex("dbo.OutgoingDebtCollectionFileHeader", new[] { "ExternalId" });
            DropTable("dbo.OutgoingDebtCollectionFileHeader");
        }
    }
}
