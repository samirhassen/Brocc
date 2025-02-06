namespace nSavings.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCustomerSecureMessageId : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.SavingsAccountComment", "CustomerSecureMessageId", c => c.Int());
        }

        public override void Down()
        {
            DropColumn("dbo.SavingsAccountComment", "CustomerSecureMessageId");
        }
    }
}
