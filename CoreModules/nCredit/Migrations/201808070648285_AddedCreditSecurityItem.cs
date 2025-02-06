namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCreditSecurityItem : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditSecurityItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CreditNr = c.String(nullable: false, maxLength: 128),
                    Name = c.String(nullable: false, maxLength: 128),
                    StringValue = c.String(nullable: false),
                    NumericValue = c.Decimal(precision: 18, scale: 2),
                    DateValue = c.DateTime(storeType: "date"),
                    CreatedByBusinessEventId = c.Int(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditHeader", t => t.CreditNr, cascadeDelete: true)
                .ForeignKey("dbo.BusinessEvent", t => t.CreatedByBusinessEventId, cascadeDelete: true)
                .Index(t => new { t.CreditNr, t.Name, t.CreatedByBusinessEventId }, unique: true, name: "CurValUIdx");

        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditSecurityItem", "CreatedByBusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.CreditSecurityItem", "CreditNr", "dbo.CreditHeader");
            DropIndex("dbo.CreditSecurityItem", "CurValUIdx");
            DropTable("dbo.CreditSecurityItem");
        }
    }
}
