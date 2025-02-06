namespace nCustomer.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCustomerRelation : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CustomerRelation",
                c => new
                {
                    CustomerId = c.Int(nullable: false),
                    RelationType = c.String(nullable: false, maxLength: 100),
                    RelationId = c.String(nullable: false, maxLength: 100),
                    StartDate = c.DateTime(storeType: "date"),
                    EndDate = c.DateTime(storeType: "date"),
                })
                .PrimaryKey(t => new { t.CustomerId, t.RelationType, t.RelationId });

        }

        public override void Down()
        {
            DropTable("dbo.CustomerRelation");
        }
    }
}
