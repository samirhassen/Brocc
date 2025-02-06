namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedFraudControl : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.CreditApplicationSearchTerm", "CreditApplicationSearchTermCoveringIndex");
            CreateTable(
                "dbo.FraudControl",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ApplicationNr = c.String(nullable: false, maxLength: 128),
                    ApplicantNr = c.Int(nullable: false),
                    Status = c.String(nullable: false),
                    RejectionReasons = c.String(),
                    IsCurrentData = c.Boolean(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                    ReplacesFraudControl_Id = c.Int(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.FraudControl", t => t.ReplacesFraudControl_Id)
                .ForeignKey("dbo.CreditApplicationHeader", t => t.ApplicationNr, cascadeDelete: true)
                .Index(t => t.ApplicationNr)
                .Index(t => t.ReplacesFraudControl_Id);

            CreateTable(
                "dbo.FraudControlItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Key = c.String(),
                    Value = c.String(),
                    Status = c.String(),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                    FraudControl_Id = c.Int(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.FraudControl", t => t.FraudControl_Id)
                .Index(t => t.FraudControl_Id);

            CreateTable(
                "dbo.FraudControlProperty",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CustomerId = c.Int(nullable: false),
                    Value = c.String(nullable: false),
                    Name = c.String(nullable: false, maxLength: 128),
                    IsCurrentData = c.Boolean(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                    ReplacesFraudControlProperty_Id = c.Int(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.FraudControlProperty", t => t.ReplacesFraudControlProperty_Id)
                .Index(t => t.ReplacesFraudControlProperty_Id);

            CreateIndex("dbo.CreditApplicationSearchTerm", "ApplicationNr", name: "CreditApplicationSearchTeBrmCoveringIndex");
            CreateIndex("dbo.CreditApplicationSearchTerm", new[] { "Name", "Value" }, name: "CreditApplicationSearchTermCoveringIndex");
        }

        public override void Down()
        {
            DropForeignKey("dbo.FraudControlProperty", "ReplacesFraudControlProperty_Id", "dbo.FraudControlProperty");
            DropForeignKey("dbo.FraudControl", "ApplicationNr", "dbo.CreditApplicationHeader");
            DropForeignKey("dbo.FraudControl", "ReplacesFraudControl_Id", "dbo.FraudControl");
            DropForeignKey("dbo.FraudControlItem", "FraudControl_Id", "dbo.FraudControl");
            DropIndex("dbo.FraudControlProperty", new[] { "ReplacesFraudControlProperty_Id" });
            DropIndex("dbo.CreditApplicationSearchTerm", "CreditApplicationSearchTermCoveringIndex");
            DropIndex("dbo.CreditApplicationSearchTerm", "CreditApplicationSearchTeBrmCoveringIndex");
            DropIndex("dbo.FraudControlItem", new[] { "FraudControl_Id" });
            DropIndex("dbo.FraudControl", new[] { "ReplacesFraudControl_Id" });
            DropIndex("dbo.FraudControl", new[] { "ApplicationNr" });
            DropTable("dbo.FraudControlProperty");
            DropTable("dbo.FraudControlItem");
            DropTable("dbo.FraudControl");
            CreateIndex("dbo.CreditApplicationSearchTerm", new[] { "Name", "Value", "ApplicationNr" }, name: "CreditApplicationSearchTermCoveringIndex");
        }
    }
}
