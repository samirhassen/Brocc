namespace nSavings.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCreationRemarks : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SavingsAccountCreationRemark",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    SavingsAccountNr = c.String(nullable: false, maxLength: 128),
                    CreatedByBusinessEventId = c.Int(nullable: false),
                    RemarkCategoryCode = c.String(nullable: false, maxLength: 100),
                    RemarkData = c.String(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.SavingsAccountHeader", t => t.SavingsAccountNr, cascadeDelete: true)
                .ForeignKey("dbo.BusinessEvent", t => t.CreatedByBusinessEventId, cascadeDelete: true)
                .Index(t => t.SavingsAccountNr)
                .Index(t => t.CreatedByBusinessEventId);

        }

        public override void Down()
        {
            DropForeignKey("dbo.SavingsAccountCreationRemark", "CreatedByBusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.SavingsAccountCreationRemark", "SavingsAccountNr", "dbo.SavingsAccountHeader");
            DropIndex("dbo.SavingsAccountCreationRemark", new[] { "CreatedByBusinessEventId" });
            DropIndex("dbo.SavingsAccountCreationRemark", new[] { "SavingsAccountNr" });
            DropTable("dbo.SavingsAccountCreationRemark");
        }
    }
}
