namespace nSavings.Migrations
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
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.DatedSavingsAccountString",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    SavingsAccountNr = c.String(nullable: false, maxLength: 128),
                    Name = c.String(nullable: false, maxLength: 100),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    ValidFromDate = c.DateTime(nullable: false, storeType: "date"),
                    BusinessEventId = c.Int(nullable: false),
                    Value = c.String(nullable: false, maxLength: 100),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.SavingsAccountHeader", t => t.SavingsAccountNr, cascadeDelete: true)
                .ForeignKey("dbo.BusinessEvent", t => t.BusinessEventId, cascadeDelete: true)
                .Index(t => t.SavingsAccountNr)
                .Index(t => t.BusinessEventId);

            CreateTable(
                "dbo.SavingsAccountHeader",
                c => new
                {
                    SavingsAccountNr = c.String(nullable: false, maxLength: 128),
                    MainCustomerId = c.Int(nullable: false),
                    Status = c.String(nullable: false, maxLength: 100),
                    CreatedByBusinessEventId = c.Int(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.SavingsAccountNr)
                .ForeignKey("dbo.BusinessEvent", t => t.CreatedByBusinessEventId, cascadeDelete: false)
                .Index(t => t.CreatedByBusinessEventId);

            CreateTable(
                "dbo.SavingsAccountComment",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    SavingsAccountNr = c.String(nullable: false, maxLength: 128),
                    EventType = c.String(maxLength: 100),
                    CommentDate = c.DateTimeOffset(nullable: false, precision: 7),
                    Attachment = c.String(),
                    CommentById = c.Int(nullable: false),
                    CommentText = c.String(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.SavingsAccountHeader", t => t.SavingsAccountNr, cascadeDelete: true)
                .Index(t => t.SavingsAccountNr);

            CreateTable(
                "dbo.DatedSavingsAccountValue",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    SavingsAccountNr = c.String(nullable: false, maxLength: 128),
                    Name = c.String(nullable: false, maxLength: 100),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    ValidFromDate = c.DateTime(nullable: false, storeType: "date"),
                    BusinessEventId = c.Int(nullable: false),
                    Value = c.Decimal(nullable: false, precision: 18, scale: 2),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.SavingsAccountHeader", t => t.SavingsAccountNr, cascadeDelete: true)
                .ForeignKey("dbo.BusinessEvent", t => t.BusinessEventId, cascadeDelete: true)
                .Index(t => t.SavingsAccountNr)
                .Index(t => t.BusinessEventId);

            CreateTable(
                "dbo.LedgerAccountTransaction",
                c => new
                {
                    Id = c.Long(nullable: false, identity: true),
                    AccountCode = c.String(nullable: false, maxLength: 100),
                    BusinessEventId = c.Int(nullable: false),
                    SavingsAccountNr = c.String(maxLength: 128),
                    Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    BookKeepingDate = c.DateTime(nullable: false, storeType: "date"),
                    InterestFromDate = c.DateTime(nullable: false, storeType: "date"),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.SavingsAccountHeader", t => t.SavingsAccountNr)
                .ForeignKey("dbo.BusinessEvent", t => t.BusinessEventId, cascadeDelete: true)
                .Index(t => t.BusinessEventId)
                .Index(t => t.SavingsAccountNr);

            CreateTable(
                "dbo.SharedDatedValue",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Name = c.String(nullable: false, maxLength: 100),
                    TransactionDate = c.DateTime(nullable: false, storeType: "date"),
                    ValidFromDate = c.DateTime(nullable: false, storeType: "date"),
                    BusinessEventId = c.Int(nullable: false),
                    Value = c.Decimal(nullable: false, precision: 18, scale: 2),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.BusinessEvent", t => t.BusinessEventId, cascadeDelete: true)
                .Index(t => t.BusinessEventId);

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

            CreateTable(
                "dbo.OcrPaymentReferenceNrSequence",
                c => new
                {
                    Id = c.Long(nullable: false, identity: true),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.SavingsAccountKeySequence",
                c => new
                {
                    Id = c.Long(nullable: false, identity: true),
                })
                .PrimaryKey(t => t.Id);

            Sql("DBCC CHECKIDENT ('dbo.SavingsAccountKeySequence', RESEED, 10000)");
        }

        public override void Down()
        {
            DropForeignKey("dbo.SharedDatedValue", "BusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.SavingsAccountHeader", "CreatedByBusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.LedgerAccountTransaction", "BusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.DatedSavingsAccountValue", "BusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.DatedSavingsAccountString", "BusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.LedgerAccountTransaction", "SavingsAccountNr", "dbo.SavingsAccountHeader");
            DropForeignKey("dbo.DatedSavingsAccountValue", "SavingsAccountNr", "dbo.SavingsAccountHeader");
            DropForeignKey("dbo.DatedSavingsAccountString", "SavingsAccountNr", "dbo.SavingsAccountHeader");
            DropForeignKey("dbo.SavingsAccountComment", "SavingsAccountNr", "dbo.SavingsAccountHeader");
            DropIndex("dbo.SharedDatedValue", new[] { "BusinessEventId" });
            DropIndex("dbo.LedgerAccountTransaction", new[] { "SavingsAccountNr" });
            DropIndex("dbo.LedgerAccountTransaction", new[] { "BusinessEventId" });
            DropIndex("dbo.DatedSavingsAccountValue", new[] { "BusinessEventId" });
            DropIndex("dbo.DatedSavingsAccountValue", new[] { "SavingsAccountNr" });
            DropIndex("dbo.SavingsAccountComment", new[] { "SavingsAccountNr" });
            DropIndex("dbo.SavingsAccountHeader", new[] { "CreatedByBusinessEventId" });
            DropIndex("dbo.DatedSavingsAccountString", new[] { "BusinessEventId" });
            DropIndex("dbo.DatedSavingsAccountString", new[] { "SavingsAccountNr" });
            DropTable("dbo.SavingsAccountKeySequence");
            DropTable("dbo.OcrPaymentReferenceNrSequence");
            DropTable("dbo.EncryptedValue");
            DropTable("dbo.SharedDatedValue");
            DropTable("dbo.LedgerAccountTransaction");
            DropTable("dbo.DatedSavingsAccountValue");
            DropTable("dbo.SavingsAccountComment");
            DropTable("dbo.SavingsAccountHeader");
            DropTable("dbo.DatedSavingsAccountString");
            DropTable("dbo.BusinessEvent");
        }
    }
}
