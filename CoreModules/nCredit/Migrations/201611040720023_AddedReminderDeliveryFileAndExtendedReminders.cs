namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedReminderDeliveryFileAndExtendedReminders : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.OutgoingCreditReminderDeliveryFileHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    FileArchiveKey = c.String(maxLength: 100),
                    ExternalId = c.String(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id);

            AddColumn("dbo.CreditReminderHeader", "InternalDueDate", c => c.DateTime(nullable: false, storeType: "date"));
            AddColumn("dbo.CreditReminderHeader", "PdfArchiveKey", c => c.String(maxLength: 100));
            AddColumn("dbo.CreditReminderHeader", "ReminderNumber", c => c.Int(nullable: false));
            AddColumn("dbo.CreditReminderHeader", "OutgoingCreditReminderDeliveryFileHeaderId", c => c.Int());
            CreateIndex("dbo.CreditReminderHeader", "OutgoingCreditReminderDeliveryFileHeaderId");
            AddForeignKey("dbo.CreditReminderHeader", "OutgoingCreditReminderDeliveryFileHeaderId", "dbo.OutgoingCreditReminderDeliveryFileHeader", "Id");
        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditReminderHeader", "OutgoingCreditReminderDeliveryFileHeaderId", "dbo.OutgoingCreditReminderDeliveryFileHeader");
            DropIndex("dbo.CreditReminderHeader", new[] { "OutgoingCreditReminderDeliveryFileHeaderId" });
            DropColumn("dbo.CreditReminderHeader", "OutgoingCreditReminderDeliveryFileHeaderId");
            DropColumn("dbo.CreditReminderHeader", "ReminderNumber");
            DropColumn("dbo.CreditReminderHeader", "PdfArchiveKey");
            DropColumn("dbo.CreditReminderHeader", "InternalDueDate");
            DropTable("dbo.OutgoingCreditReminderDeliveryFileHeader");
        }
    }
}
