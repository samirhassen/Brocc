namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCommentAttachment : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditApplicationComment", "Attachment", c => c.String());
        }

        public override void Down()
        {
            DropColumn("dbo.CreditApplicationComment", "Attachment");
        }
    }
}
