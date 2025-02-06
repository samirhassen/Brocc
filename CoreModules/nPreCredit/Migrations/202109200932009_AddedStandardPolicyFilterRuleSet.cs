namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedStandardPolicyFilterRuleSet : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.StandardPolicyFilterRuleSet",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    SlotName = c.String(maxLength: 128),
                    RuleSetName = c.String(nullable: false, maxLength: 256),
                    RuleSetModelData = c.String(nullable: false),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id);

            //We use null to be inactive and so A,B,Pending will be all unique (and any future slots added)
            Sql("CREATE UNIQUE NONCLUSTERED INDEX Idx_StandardPolicyFilterRuleSet_UniqueSlotNames ON dbo.StandardPolicyFilterRuleSet(SlotName) where SlotName IS NOT NULL");
        }

        public override void Down()
        {
            DropTable("dbo.StandardPolicyFilterRuleSet");
        }
    }
}
