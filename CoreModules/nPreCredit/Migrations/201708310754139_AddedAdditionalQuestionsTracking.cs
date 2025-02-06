namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedAdditionalQuestionsTracking : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditApplicationHeader", "CanSkipAdditionalQuestions", c => c.Boolean(nullable: false, defaultValue: false));
            CreateIndex("dbo.CreditApplicationHeader", "CanSkipAdditionalQuestions");
        }

        public override void Down()
        {
            DropIndex("dbo.CreditApplicationHeader", new[] { "CanSkipAdditionalQuestions" });
            DropColumn("dbo.CreditApplicationHeader", "CanSkipAdditionalQuestions");
        }
    }
}
