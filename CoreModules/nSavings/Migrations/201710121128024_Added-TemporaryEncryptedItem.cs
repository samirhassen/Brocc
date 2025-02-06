namespace nSavings.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedTemporaryEncryptedItem : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.TemporaryExternallyEncryptedItem",
                c => new
                {
                    Id = c.String(nullable: false, maxLength: 128),
                    CipherText = c.String(nullable: false),
                    ProtocolVersionName = c.String(nullable: false, maxLength: 100),
                    AddedDate = c.DateTime(nullable: false),
                    DeleteAfterDate = c.DateTime(nullable: false),
                })
                .PrimaryKey(t => t.Id);

        }

        public override void Down()
        {
            DropTable("dbo.TemporaryExternallyEncryptedItem");
        }
    }
}
