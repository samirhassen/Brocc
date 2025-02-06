namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedOutgoingPaymentHeader : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.OutgoingPaymentHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    BookKeepingDate = c.DateTime(nullable: false, storeType: "date"),
                    FromBankAccountNr = c.String(maxLength: 128),
                    ToBankAccountNr = c.String(maxLength: 128),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id);

            AddColumn("dbo.AccountTransaction", "OutgoingPaymentId", c => c.Int());
            CreateIndex("dbo.AccountTransaction", "OutgoingPaymentId");
            AddForeignKey("dbo.AccountTransaction", "OutgoingPaymentId", "dbo.OutgoingPaymentHeader", "Id");
        }

        public override void Down()
        {
            DropForeignKey("dbo.AccountTransaction", "OutgoingPaymentId", "dbo.OutgoingPaymentHeader");
            DropIndex("dbo.AccountTransaction", new[] { "OutgoingPaymentId" });
            DropColumn("dbo.AccountTransaction", "OutgoingPaymentId");
            DropTable("dbo.OutgoingPaymentHeader");
        }
    }
}
