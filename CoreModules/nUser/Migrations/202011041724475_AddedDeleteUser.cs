namespace nUser.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedDeleteUser : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.User", "DeletedById", c => c.Int());
            AddColumn("dbo.User", "DeletionDate", c => c.DateTime());
        }

        public override void Down()
        {
            DropColumn("dbo.User", "DeletionDate");
            DropColumn("dbo.User", "DeletedById");
        }
    }
}
