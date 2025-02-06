namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCustomerCheckpoint : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CustomerCheckpoint",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CustomerId = c.Int(nullable: false),
                    IsCurrentState = c.Boolean(nullable: false),
                    ReasonText = c.String(maxLength: 2000),
                    IsReasonTextEncrypted = c.Boolean(nullable: false),
                    IsCheckpointActive = c.Boolean(nullable: false),
                    StateDate = c.DateTime(nullable: false),
                    StateBy = c.Int(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .Index(t => t.CustomerId);

            Sql("CREATE UNIQUE INDEX IX_CustomerCheckpointOnlyOneDefault ON dbo.CustomerCheckpoint(CustomerId) WHERE IsCurrentState = 1");
        }

        public override void Down()
        {
            DropIndex("dbo.CustomerCheckpoint", new[] { "CustomerId" });
            Sql("drop index IX_CustomerCheckpointOnlyOneDefault ON dbo.CustomerCheckpoint");
            DropTable("dbo.CustomerCheckpoint");
        }
    }
}
