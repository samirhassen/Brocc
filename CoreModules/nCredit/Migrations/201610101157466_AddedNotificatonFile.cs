namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedNotificatonFile : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.CreditNotificationHeader", new[] { "SentForDeliveryDate" });
            CreateTable(
                "dbo.OutgoingCreditNotificationDeliveryFileHeader",
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
                    BusinessEvent_Id = c.Int(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.BusinessEvent", t => t.BusinessEvent_Id)
                .Index(t => t.BusinessEvent_Id);

            AddColumn("dbo.CreditNotificationHeader", "OutgoingCreditNotificationDeliveryFileHeaderId", c => c.Int());
            CreateIndex("dbo.CreditNotificationHeader", "OutgoingCreditNotificationDeliveryFileHeaderId");
            AddForeignKey("dbo.CreditNotificationHeader", "OutgoingCreditNotificationDeliveryFileHeaderId", "dbo.OutgoingCreditNotificationDeliveryFileHeader", "Id");
            DropColumn("dbo.CreditNotificationHeader", "SentForDeliveryDate");
        }

        public override void Down()
        {
            AddColumn("dbo.CreditNotificationHeader", "SentForDeliveryDate", c => c.DateTimeOffset(precision: 7));
            DropForeignKey("dbo.OutgoingCreditNotificationDeliveryFileHeader", "BusinessEvent_Id", "dbo.BusinessEvent");
            DropForeignKey("dbo.CreditNotificationHeader", "OutgoingCreditNotificationDeliveryFileHeaderId", "dbo.OutgoingCreditNotificationDeliveryFileHeader");
            DropIndex("dbo.OutgoingCreditNotificationDeliveryFileHeader", new[] { "BusinessEvent_Id" });
            DropIndex("dbo.CreditNotificationHeader", new[] { "OutgoingCreditNotificationDeliveryFileHeaderId" });
            DropColumn("dbo.CreditNotificationHeader", "OutgoingCreditNotificationDeliveryFileHeaderId");
            DropTable("dbo.OutgoingCreditNotificationDeliveryFileHeader");
            CreateIndex("dbo.CreditNotificationHeader", "SentForDeliveryDate");
        }
    }
}
