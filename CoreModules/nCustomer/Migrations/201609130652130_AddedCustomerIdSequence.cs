namespace nCustomer.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCustomerIdSequence : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CustomerIdSequence",
                c => new
                {
                    CustomerId = c.Int(nullable: false, identity: true),
                    CivicRegNrHash = c.String(nullable: false, maxLength: 100),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                })
                .PrimaryKey(t => t.CustomerId)
                .Index(t => t.CivicRegNrHash, unique: true);

        }

        public override void Down()
        {
            DropIndex("dbo.CustomerIdSequence", new[] { "CivicRegNrHash" });
            DropTable("dbo.CustomerIdSequence");
        }
    }
}
