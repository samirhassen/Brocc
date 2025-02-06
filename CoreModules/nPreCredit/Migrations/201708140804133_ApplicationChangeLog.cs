namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ApplicationChangeLog : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditApplicationChangeLogItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ApplicationNr = c.String(nullable: false, maxLength: 100),
                    Name = c.String(nullable: false, maxLength: 100),
                    GroupName = c.String(nullable: false, maxLength: 100),
                    OldValue = c.String(nullable: false, maxLength: 100),
                    TransactionType = c.String(nullable: false, maxLength: 100),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .Index(t => t.ApplicationNr, name: "CreditApplicationChangeLogItemApplicationNrsIndex")
                .Index(t => t.Name, name: "CreditApplicationChangeLogItemNamesIndex")
                .Index(t => t.GroupName, name: "CreditApplicationChangeLogItemGroupNamesIndex");

        }

        public override void Down()
        {
            DropIndex("dbo.CreditApplicationChangeLogItem", "CreditApplicationChangeLogItemGroupNamesIndex");
            DropIndex("dbo.CreditApplicationChangeLogItem", "CreditApplicationChangeLogItemNamesIndex");
            DropIndex("dbo.CreditApplicationChangeLogItem", "CreditApplicationChangeLogItemApplicationNrsIndex");
            DropTable("dbo.CreditApplicationChangeLogItem");
        }
    }
}
