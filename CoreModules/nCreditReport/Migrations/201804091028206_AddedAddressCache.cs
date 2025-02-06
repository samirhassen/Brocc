namespace nCreditReport.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedAddressCache : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AddressLookupCachedResult",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ProviderName = c.String(nullable: false, maxLength: 100),
                    CustomerId = c.Int(nullable: false),
                    RequestDate = c.DateTimeOffset(nullable: false, precision: 7),
                    EncryptionKeyName = c.String(nullable: false),
                    EncryptedData = c.Binary(nullable: false),
                    DeleteAfterDate = c.DateTimeOffset(nullable: false, precision: 7),
                })
                .PrimaryKey(t => t.Id)
                .Index(t => new { t.CustomerId, t.ProviderName, t.RequestDate }, name: "AddressLookupCachedResultLookupIdx")
                .Index(t => t.DeleteAfterDate);

        }

        public override void Down()
        {
            DropIndex("dbo.AddressLookupCachedResult", new[] { "DeleteAfterDate" });
            DropIndex("dbo.AddressLookupCachedResult", "AddressLookupCachedResultLookupIdx");
            DropTable("dbo.AddressLookupCachedResult");
        }
    }
}
