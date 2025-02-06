namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCreditApplicationCustomerList : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditApplicationCustomerListOperation",
                c => new
                {
                    Id = c.Long(nullable: false, identity: true),
                    ApplicationNr = c.String(nullable: false, maxLength: 128),
                    ListName = c.String(nullable: false, maxLength: 128),
                    CustomerId = c.Int(nullable: false),
                    IsAdd = c.Boolean(nullable: false),
                    OperationDate = c.DateTimeOffset(nullable: false, precision: 7),
                    ByUserId = c.Int(nullable: false),
                    CreditApplicationEventId = c.Int(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditApplicationEvent", t => t.CreditApplicationEventId)
                .ForeignKey("dbo.CreditApplicationHeader", t => t.ApplicationNr, cascadeDelete: true)
                .Index(t => t.ApplicationNr)
                .Index(t => t.CreditApplicationEventId);

            CreateTable(
                "dbo.CreditApplicationCustomerListMember",
                c => new
                {
                    ApplicationNr = c.String(nullable: false, maxLength: 128),
                    CustomerId = c.Int(nullable: false),
                    ListName = c.String(nullable: false, maxLength: 128),
                })
                .PrimaryKey(t => new { t.ApplicationNr, t.CustomerId, t.ListName })
                .ForeignKey("dbo.CreditApplicationHeader", t => t.ApplicationNr, cascadeDelete: true)
                .Index(t => t.ApplicationNr);

        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditApplicationCustomerListMember", "ApplicationNr", "dbo.CreditApplicationHeader");
            DropForeignKey("dbo.CreditApplicationCustomerListOperation", "ApplicationNr", "dbo.CreditApplicationHeader");
            DropForeignKey("dbo.CreditApplicationCustomerListOperation", "CreditApplicationEventId", "dbo.CreditApplicationEvent");
            DropIndex("dbo.CreditApplicationCustomerListMember", new[] { "ApplicationNr" });
            DropIndex("dbo.CreditApplicationCustomerListOperation", new[] { "CreditApplicationEventId" });
            DropIndex("dbo.CreditApplicationCustomerListOperation", new[] { "ApplicationNr" });
            DropTable("dbo.CreditApplicationCustomerListMember");
            DropTable("dbo.CreditApplicationCustomerListOperation");
        }
    }
}
