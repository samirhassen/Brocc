namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedApplicationList : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditApplicationListOperation",
                c => new
                {
                    Id = c.Long(nullable: false, identity: true),
                    ApplicationNr = c.String(nullable: false, maxLength: 128),
                    ListName = c.String(nullable: false, maxLength: 128),
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
                "dbo.CreditApplicationListMember",
                c => new
                {
                    ApplicationNr = c.String(nullable: false, maxLength: 128),
                    ListName = c.String(nullable: false, maxLength: 128),
                })
                .PrimaryKey(t => new { t.ApplicationNr, t.ListName })
                .ForeignKey("dbo.CreditApplicationHeader", t => t.ApplicationNr, cascadeDelete: true)
                .Index(t => t.ApplicationNr);

        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditApplicationListMember", "ApplicationNr", "dbo.CreditApplicationHeader");
            DropForeignKey("dbo.CreditApplicationListOperation", "ApplicationNr", "dbo.CreditApplicationHeader");
            DropForeignKey("dbo.CreditApplicationListOperation", "CreditApplicationEventId", "dbo.CreditApplicationEvent");
            DropIndex("dbo.CreditApplicationListMember", new[] { "ApplicationNr" });
            DropIndex("dbo.CreditApplicationListOperation", new[] { "CreditApplicationEventId" });
            DropIndex("dbo.CreditApplicationListOperation", new[] { "ApplicationNr" });
            DropTable("dbo.CreditApplicationListMember");
            DropTable("dbo.CreditApplicationListOperation");
        }
    }
}
