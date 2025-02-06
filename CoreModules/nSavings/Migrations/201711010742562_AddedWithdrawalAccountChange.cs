namespace nSavings.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedWithdrawalAccountChange : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SavingsAccountWithdrawalAccountChange",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    SavingsAccountNr = c.String(nullable: false, maxLength: 128),
                    PowerOfAttorneyDocumentArchiveKey = c.String(maxLength: 100),
                    CreatedByBusinessEventId = c.Int(nullable: false),
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
            DropForeignKey("dbo.SavingsAccountWithdrawalAccountChange", "CreatedByBusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.SavingsAccountWithdrawalAccountChange", "SavingsAccountNr", "dbo.SavingsAccountHeader");
            DropIndex("dbo.SavingsAccountWithdrawalAccountChange", new[] { "CreatedByBusinessEventId" });
            DropIndex("dbo.SavingsAccountWithdrawalAccountChange", new[] { "SavingsAccountNr" });
            DropTable("dbo.SavingsAccountWithdrawalAccountChange");
        }
    }
}
