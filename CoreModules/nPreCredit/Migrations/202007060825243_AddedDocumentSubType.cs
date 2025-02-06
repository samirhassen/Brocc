namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedDocumentSubType : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditApplicationDocumentHeader", "DocumentSubType", c => c.String(maxLength: 256));
        }

        public override void Down()
        {
            DropColumn("dbo.CreditApplicationDocumentHeader", "DocumentSubType");
        }
    }
}
