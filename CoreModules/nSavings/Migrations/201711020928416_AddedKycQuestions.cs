namespace nSavings.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedKycQuestions : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SavingsAccountKycQuestion",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    SavingsAccountNr = c.String(nullable: false, maxLength: 128),
                    Name = c.String(nullable: false, maxLength: 100),
                    Group = c.String(nullable: false, maxLength: 100),
                    Value = c.String(nullable: false),
                    BusinessEventId = c.Int(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.SavingsAccountHeader", t => t.SavingsAccountNr, cascadeDelete: true)
                .ForeignKey("dbo.BusinessEvent", t => t.BusinessEventId, cascadeDelete: true)
                .Index(t => t.SavingsAccountNr)
                .Index(t => t.BusinessEventId);

        }

        public override void Down()
        {
            DropForeignKey("dbo.SavingsAccountKycQuestion", "BusinessEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.SavingsAccountKycQuestion", "SavingsAccountNr", "dbo.SavingsAccountHeader");
            DropIndex("dbo.SavingsAccountKycQuestion", new[] { "BusinessEventId" });
            DropIndex("dbo.SavingsAccountKycQuestion", new[] { "SavingsAccountNr" });
            DropTable("dbo.SavingsAccountKycQuestion");
        }
    }
}
