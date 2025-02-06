namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedMortgageFinalCreditCheck : DbMigration
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

            AddColumn("dbo.MortgageLoanCreditApplicationHeaderExtension", "FinalCreditCheckStatus", c => c.String(maxLength: 100));
            CreateIndex("dbo.MortgageLoanCreditApplicationHeaderExtension", "FinalCreditCheckStatus");
        }

        public override void Down()
        {
            DropIndex("dbo.MortgageLoanCreditApplicationHeaderExtension", new[] { "FinalCreditCheckStatus" });
            DropColumn("dbo.MortgageLoanCreditApplicationHeaderExtension", "FinalCreditCheckStatus");
            DropTable("dbo.TemporaryExternallyEncryptedItem");
        }
    }
}
