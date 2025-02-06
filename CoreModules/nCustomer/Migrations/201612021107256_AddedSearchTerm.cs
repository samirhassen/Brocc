namespace nCustomer.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedSearchTerm : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CustomerSearchTerm",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CustomerId = c.Int(nullable: false),
                    TermCode = c.String(nullable: false, maxLength: 100),
                    Value = c.String(nullable: false, maxLength: 100),
                    IsActive = c.Boolean(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .Index(t => t.CustomerId)
                .Index(t => new { t.TermCode, t.Value, t.IsActive }, name: "CustomerSearchIdx1");
            Sql("CREATE NONCLUSTERED INDEX CustomerFetchIdx1 ON [dbo].[CustomerProperty] ([IsCurrentData],[CustomerId],[Name])");
            Sql("CREATE NONCLUSTERED INDEX SearchTermUpdateIdx1 ON [dbo].[CustomerSearchTerm] ([TermCode]) INCLUDE ([Id],[CustomerId],[IsActive])");
        }

        public override void Down()
        {
            Sql("DROP INDEX SearchTermUpdateIdx1 ON [dbo].[CustomerSearchTerm]");
            DropIndex("dbo.CustomerSearchTerm", "CustomerSearchIdx1");
            DropIndex("dbo.CustomerSearchTerm", new[] { "CustomerId" });
            DropTable("dbo.CustomerSearchTerm");
            Sql("DROP INDEX CustomerFetchIdx1 on [dbo].[CustomerProperty]");
        }
    }
}
