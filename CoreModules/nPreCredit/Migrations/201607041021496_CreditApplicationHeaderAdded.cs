namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class CreditApplicationHeaderAdded : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.CreditApplication", new[] { "CreationDate" });
            CreateTable(
                "dbo.CreditApplicationHeader",
                c => new
                {
                    ApplicationNr = c.String(nullable: false, maxLength: 128),
                    AffiliateName = c.String(nullable: false, maxLength: 100),
                    NrOfApplicants = c.Int(nullable: false),
                    ApplicationDate = c.DateTimeOffset(nullable: false, precision: 7),
                    EncryptionKeyName = c.String(nullable: false),
                    IsActive = c.Boolean(nullable: false),
                    IsCreditCheckApproved = c.Boolean(nullable: false),
                    IsCustomerCheckApproved = c.Boolean(nullable: false),
                    IsAgreementApproved = c.Boolean(nullable: false),
                    IsFinalDecisionMade = c.Boolean(nullable: false),
                    FinalDecisionById = c.Int(),
                    FinalDecisionDate = c.DateTimeOffset(precision: 7),
                    IsPartiallyApproved = c.Boolean(nullable: false),
                    PartiallyApprovedById = c.Int(),
                    PartiallyApprovedDate = c.DateTimeOffset(precision: 7),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                })
                .PrimaryKey(t => t.ApplicationNr)
                .Index(t => t.ApplicationDate)
                .Index(t => t.IsActive)
                .Index(t => t.IsCreditCheckApproved)
                .Index(t => t.IsCustomerCheckApproved)
                .Index(t => t.IsAgreementApproved)
                .Index(t => t.IsFinalDecisionMade)
                .Index(t => t.IsPartiallyApproved);

            CreateTable(
                "dbo.EncryptedCreditApplicationItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ApplicationNr = c.String(nullable: false, maxLength: 128),
                    GroupName = c.String(nullable: false, maxLength: 100),
                    Name = c.String(nullable: false, maxLength: 100),
                    Value = c.Binary(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditApplicationHeader", t => t.ApplicationNr, cascadeDelete: true)
                .Index(t => t.ApplicationNr)
                .Index(t => new { t.GroupName, t.Name }, name: "CreditApplicationEncryptedItemNamesIndex")
                .Index(t => t.GroupName, name: "CreditApplicationEncryptedItemGroupNameIndex")
                .Index(t => t.Name, name: "CreditApplicationEncryptedItemNameIndex");

            CreateTable(
                "dbo.CreditApplicationItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ApplicationNr = c.String(nullable: false, maxLength: 128),
                    GroupName = c.String(nullable: false, maxLength: 100),
                    Name = c.String(nullable: false, maxLength: 100),
                    Value = c.String(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditApplicationHeader", t => t.ApplicationNr, cascadeDelete: true)
                .Index(t => t.ApplicationNr)
                .Index(t => new { t.GroupName, t.Name }, name: "CreditApplicationItemNamesIndex")
                .Index(t => t.GroupName, name: "CreditApplicationItemGroupNameIndex")
                .Index(t => t.Name, name: "CreditApplicationItemNameIndex");

            CreateTable(
                "dbo.CreditApplicationSearchTerm",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ApplicationNr = c.String(nullable: false, maxLength: 128),
                    Name = c.String(nullable: false, maxLength: 100),
                    Value = c.String(nullable: false, maxLength: 100),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditApplicationHeader", t => t.ApplicationNr, cascadeDelete: true)
                .Index(t => new { t.Name, t.Value, t.ApplicationNr }, name: "CreditApplicationSearchTermCoveringIndex")
                .Index(t => t.Name, name: "CreditApplicationSearchTermNameIndex")
                .Index(t => t.Value, name: "CreditApplicationSearchTermValueIndex");

            CreateTable(
                "dbo.CreditApplicationKeySequence",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                })
                .PrimaryKey(t => t.Id);
            Sql("DBCC CHECKIDENT ('dbo.CreditApplicationKeySequence', reseed, 10000)");

            DropTable("dbo.CreditApplication");
        }

        public override void Down()
        {
            CreateTable(
                "dbo.CreditApplication",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CreationDate = c.DateTimeOffset(nullable: false, precision: 7),
                    CreatedById = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id);

            DropForeignKey("dbo.CreditApplicationSearchTerm", "ApplicationNr", "dbo.CreditApplicationHeader");
            DropForeignKey("dbo.CreditApplicationItem", "ApplicationNr", "dbo.CreditApplicationHeader");
            DropForeignKey("dbo.EncryptedCreditApplicationItem", "ApplicationNr", "dbo.CreditApplicationHeader");
            DropIndex("dbo.CreditApplicationSearchTerm", "CreditApplicationSearchTermValueIndex");
            DropIndex("dbo.CreditApplicationSearchTerm", "CreditApplicationSearchTermNameIndex");
            DropIndex("dbo.CreditApplicationSearchTerm", "CreditApplicationSearchTermCoveringIndex");
            DropIndex("dbo.CreditApplicationItem", "CreditApplicationItemNameIndex");
            DropIndex("dbo.CreditApplicationItem", "CreditApplicationItemGroupNameIndex");
            DropIndex("dbo.CreditApplicationItem", "CreditApplicationItemNamesIndex");
            DropIndex("dbo.CreditApplicationItem", new[] { "ApplicationNr" });
            DropIndex("dbo.EncryptedCreditApplicationItem", "CreditApplicationEncryptedItemNameIndex");
            DropIndex("dbo.EncryptedCreditApplicationItem", "CreditApplicationEncryptedItemGroupNameIndex");
            DropIndex("dbo.EncryptedCreditApplicationItem", "CreditApplicationEncryptedItemNamesIndex");
            DropIndex("dbo.EncryptedCreditApplicationItem", new[] { "ApplicationNr" });
            DropIndex("dbo.CreditApplicationHeader", new[] { "IsPartiallyApproved" });
            DropIndex("dbo.CreditApplicationHeader", new[] { "IsFinalDecisionMade" });
            DropIndex("dbo.CreditApplicationHeader", new[] { "IsAgreementApproved" });
            DropIndex("dbo.CreditApplicationHeader", new[] { "IsCustomerCheckApproved" });
            DropIndex("dbo.CreditApplicationHeader", new[] { "IsCreditCheckApproved" });
            DropIndex("dbo.CreditApplicationHeader", new[] { "IsActive" });
            DropIndex("dbo.CreditApplicationHeader", new[] { "ApplicationDate" });
            DropTable("dbo.CreditApplicationKeySequence");
            DropTable("dbo.CreditApplicationSearchTerm");
            DropTable("dbo.CreditApplicationItem");
            DropTable("dbo.EncryptedCreditApplicationItem");
            DropTable("dbo.CreditApplicationHeader");
            CreateIndex("dbo.CreditApplication", "CreationDate");
        }
    }
}
