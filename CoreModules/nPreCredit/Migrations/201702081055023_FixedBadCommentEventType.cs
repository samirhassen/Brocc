namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class FixedBadCommentEventType : DbMigration
    {
        public override void Up()
        {
            Sql("update CreditApplicationComment set EventType = 'AgreementSigned' where CommentText like 'Agreement signed by applicant %' and EventType = 'AgreementSentForSigning'");
        }

        public override void Down()
        {
        }
    }
}
