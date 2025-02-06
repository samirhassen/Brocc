namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class SwitchedPaymentFieldsToItemsForEncryption : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.OutgoingPaymentHeaderItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    OutgoingPaymentId = c.Int(nullable: false),
                    Name = c.String(nullable: false, maxLength: 100),
                    IsEncrypted = c.Boolean(nullable: false),
                    Value = c.String(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.OutgoingPaymentHeader", t => t.OutgoingPaymentId, cascadeDelete: true)
                .Index(t => t.OutgoingPaymentId)
                .Index(t => t.Name);

            DropColumn("dbo.OutgoingPaymentHeader", "FromBankAccountNr");
            DropColumn("dbo.OutgoingPaymentHeader", "ToBankAccountNr");
        }

        public override void Down()
        {
            AddColumn("dbo.OutgoingPaymentHeader", "ToBankAccountNr", c => c.String(maxLength: 128));
            AddColumn("dbo.OutgoingPaymentHeader", "FromBankAccountNr", c => c.String(maxLength: 128));
            DropForeignKey("dbo.OutgoingPaymentHeaderItem", "OutgoingPaymentId", "dbo.OutgoingPaymentHeader");
            DropIndex("dbo.OutgoingPaymentHeaderItem", new[] { "Name" });
            DropIndex("dbo.OutgoingPaymentHeaderItem", new[] { "OutgoingPaymentId" });
            DropTable("dbo.OutgoingPaymentHeaderItem");
        }
    }
}
