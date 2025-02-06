namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.BusinessEvent",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    EventType = c.String(nullable: false, maxLength: 100),
                    EventDate = c.DateTimeOffset(nullable: false, precision: 7),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    BookKeepingDate = c.DateTime(nullable: false, storeType: "date"),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.DatedCreditValue",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CreditNr = c.String(nullable: false, maxLength: 128),
                    Name = c.String(nullable: false, maxLength: 100),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    BusinessEventId = c.Int(nullable: false),
                    Value = c.Decimal(nullable: false, precision: 18, scale: 2),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditHeader", t => t.CreditNr, cascadeDelete: true)
                .ForeignKey("dbo.BusinessEvent", t => t.BusinessEventId, cascadeDelete: true)
                .Index(t => t.CreditNr)
                .Index(t => t.BusinessEventId);

            CreateTable(
                "dbo.CreditHeader",
                c => new
                {
                    CreditNr = c.String(nullable: false, maxLength: 128),
                    ProviderName = c.String(nullable: false, maxLength: 100),
                    NrOfApplicants = c.Int(nullable: false),
                    StartDate = c.DateTimeOffset(nullable: false, precision: 7),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.CreditNr)
                .Index(t => t.StartDate);

            CreateTable(
                "dbo.CreditNotificationHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CreditNr = c.String(nullable: false, maxLength: 128),
                    NotificationDate = c.DateTime(nullable: false, storeType: "date"),
                    DueDate = c.DateTime(nullable: false, storeType: "date"),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    BookKeepingDate = c.DateTime(nullable: false, storeType: "date"),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditHeader", t => t.CreditNr, cascadeDelete: true)
                .Index(t => t.CreditNr);

            CreateTable(
                "dbo.CreditReminderHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ReminderDate = c.DateTime(nullable: false, storeType: "date"),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    BookKeepingDate = c.DateTime(nullable: false, storeType: "date"),
                    CreditNr = c.String(nullable: false, maxLength: 128),
                    NotificationId = c.Int(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditNotificationHeader", t => t.NotificationId)
                .ForeignKey("dbo.CreditHeader", t => t.CreditNr, cascadeDelete: true)
                .Index(t => t.CreditNr)
                .Index(t => t.NotificationId);

            CreateTable(
                "dbo.AccountTransaction",
                c => new
                {
                    Id = c.Long(nullable: false, identity: true),
                    AccountCode = c.String(nullable: false, maxLength: 100),
                    CreditNotificationId = c.Int(),
                    BusinessEventId = c.Int(nullable: false),
                    CreditNr = c.String(maxLength: 128),
                    PaymentId = c.Int(),
                    ReminderId = c.Int(),
                    WriteoffId = c.Int(),
                    Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    BookKeepingDate = c.DateTime(nullable: false, storeType: "date"),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.PaymentHeader", t => t.PaymentId)
                .ForeignKey("dbo.WriteoffHeader", t => t.WriteoffId)
                .ForeignKey("dbo.CreditReminderHeader", t => t.ReminderId)
                .ForeignKey("dbo.CreditNotificationHeader", t => t.CreditNotificationId)
                .ForeignKey("dbo.CreditHeader", t => t.CreditNr)
                .ForeignKey("dbo.BusinessEvent", t => t.BusinessEventId, cascadeDelete: true)
                .Index(t => t.CreditNotificationId)
                .Index(t => t.BusinessEventId)
                .Index(t => t.CreditNr)
                .Index(t => t.PaymentId)
                .Index(t => t.ReminderId)
                .Index(t => t.WriteoffId);

            CreateTable(
                "dbo.PaymentHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    BookKeepingDate = c.DateTime(nullable: false, storeType: "date"),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.WriteoffHeader",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    BookKeepingDate = c.DateTime(nullable: false, storeType: "date"),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.CreditKeySequence",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.EncryptedValue",
                c => new
                {
                    Id = c.Long(nullable: false, identity: true),
                    EncryptionKeyName = c.String(nullable: false, maxLength: 100),
                    Value = c.Binary(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    CreatedById = c.Int(nullable: false),
                    CreatedDate = c.DateTimeOffset(nullable: false, precision: 7),
                })
                .PrimaryKey(t => t.Id);

            Sql("create index IX_CreditBalance on AccountTransaction(AccountCode, CreditNr, TransactionDate) include(Amount)");
            Sql("create index IX_NotificationBalance on AccountTransaction(AccountCode, CreditNotificationId, TransactionDate) include(Amount)");
        }

        public override void Down()
        {
            DropForeignKey("dbo.AccountTransaction", "BusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.DatedCreditValue", "BusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.AccountTransaction", "CreditNr", "dbo.CreditHeader");
            DropForeignKey("dbo.CreditReminderHeader", "CreditNr", "dbo.CreditHeader");
            DropForeignKey("dbo.CreditNotificationHeader", "CreditNr", "dbo.CreditHeader");
            DropForeignKey("dbo.AccountTransaction", "CreditNotificationId", "dbo.CreditNotificationHeader");
            DropForeignKey("dbo.CreditReminderHeader", "NotificationId", "dbo.CreditNotificationHeader");
            DropForeignKey("dbo.AccountTransaction", "ReminderId", "dbo.CreditReminderHeader");
            DropForeignKey("dbo.AccountTransaction", "WriteoffId", "dbo.WriteoffHeader");
            DropForeignKey("dbo.AccountTransaction", "PaymentId", "dbo.PaymentHeader");
            DropForeignKey("dbo.DatedCreditValue", "CreditNr", "dbo.CreditHeader");
            DropIndex("dbo.AccountTransaction", new[] { "WriteoffId" });
            DropIndex("dbo.AccountTransaction", new[] { "ReminderId" });
            DropIndex("dbo.AccountTransaction", new[] { "PaymentId" });
            DropIndex("dbo.AccountTransaction", new[] { "CreditNr" });
            DropIndex("dbo.AccountTransaction", new[] { "BusinessEventId" });
            DropIndex("dbo.AccountTransaction", new[] { "CreditNotificationId" });
            DropIndex("dbo.CreditReminderHeader", new[] { "NotificationId" });
            DropIndex("dbo.CreditReminderHeader", new[] { "CreditNr" });
            DropIndex("dbo.CreditNotificationHeader", new[] { "CreditNr" });
            DropIndex("dbo.CreditHeader", new[] { "StartDate" });
            DropIndex("dbo.DatedCreditValue", new[] { "BusinessEventId" });
            DropIndex("dbo.DatedCreditValue", new[] { "CreditNr" });
            DropTable("dbo.EncryptedValue");
            DropTable("dbo.CreditKeySequence");
            DropTable("dbo.WriteoffHeader");
            DropTable("dbo.PaymentHeader");
            DropTable("dbo.AccountTransaction");
            DropTable("dbo.CreditReminderHeader");
            DropTable("dbo.CreditNotificationHeader");
            DropTable("dbo.CreditHeader");
            DropTable("dbo.DatedCreditValue");
            DropTable("dbo.BusinessEvent");
        }
    }
}
