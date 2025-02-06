namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedManualPaymentComment : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PaymentHeader", "NoteText", c => c.String());
        }

        public override void Down()
        {
            DropColumn("dbo.PaymentHeader", "NoteText");
        }
    }
}
