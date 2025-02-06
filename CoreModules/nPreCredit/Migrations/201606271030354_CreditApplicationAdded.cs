namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class CreditApplicationAdded : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CreditApplication",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CreationDate = c.DateTimeOffset(nullable: false, precision: 7),
                    CreatedById = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .Index(t => t.CreationDate);
        }

        public override void Down()
        {
            DropIndex("dbo.CreditApplication", new[] { "CreationDate" });
            DropTable("dbo.CreditApplication");
        }
    }
}
