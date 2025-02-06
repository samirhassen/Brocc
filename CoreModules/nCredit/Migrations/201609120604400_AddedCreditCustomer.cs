namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCreditCustomer : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditCustomer",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CreditNr = c.String(nullable: false, maxLength: 128),
                    ApplicantNr = c.Int(nullable: false),
                    CustomerId = c.Int(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CreditHeader", t => t.CreditNr, cascadeDelete: true)
                .Index(t => t.CreditNr);

            AddColumn("dbo.CreditHeader", "Status", c => c.String(nullable: false, maxLength: 100));
        }

        public override void Down()
        {
            DropForeignKey("dbo.CreditCustomer", "CreditNr", "dbo.CreditHeader");
            DropIndex("dbo.CreditCustomer", new[] { "CreditNr" });
            DropColumn("dbo.CreditHeader", "Status");
            DropTable("dbo.CreditCustomer");
        }
    }
}
