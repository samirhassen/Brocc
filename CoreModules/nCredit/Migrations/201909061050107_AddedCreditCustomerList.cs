namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCreditCustomerList : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditCustomerListMember",
                c => new
                {
                    CreditNr = c.String(nullable: false, maxLength: 128),
                    CustomerId = c.Int(nullable: false),
                    ListName = c.String(nullable: false, maxLength: 128),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => new { t.CreditNr, t.CustomerId, t.ListName })
                .ForeignKey("dbo.CreditHeader", t => t.CreditNr, cascadeDelete: true)
                .Index(t => t.CreditNr);

            CreateTable(
                "dbo.CreditCustomerListOperation",
                c => new
                {
                    Id = c.Long(nullable: false, identity: true),
                    CreditNr = c.String(nullable: false, maxLength: 128),
                    ListName = c.String(nullable: false, maxLength: 128),
                    CustomerId = c.Int(nullable: false),
                    IsAdd = c.Boolean(nullable: false),
                    OperationDate = c.DateTimeOffset(nullable: false, precision: 7),
                    ByUserId = c.Int(nullable: false),
                    ByEventId = c.Int(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.BusinessEvent", t => t.ByEventId)
                .ForeignKey("dbo.CreditHeader", t => t.CreditNr, cascadeDelete: true)
                .Index(t => t.CreditNr)
                .Index(t => t.ByEventId);

        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditCustomerListOperation", "CreditNr", "dbo.CreditHeader");
            DropForeignKey("dbo.CreditCustomerListOperation", "ByEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.CreditCustomerListMember", "CreditNr", "dbo.CreditHeader");
            DropIndex("dbo.CreditCustomerListOperation", new[] { "ByEventId" });
            DropIndex("dbo.CreditCustomerListOperation", new[] { "CreditNr" });
            DropIndex("dbo.CreditCustomerListMember", new[] { "CreditNr" });
            DropTable("dbo.CreditCustomerListOperation");
            DropTable("dbo.CreditCustomerListMember");
        }
    }
}
