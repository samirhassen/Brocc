namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCampaigns : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CampaignCode",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CampaignId = c.String(nullable: false, maxLength: 128),
                    Code = c.String(nullable: false, maxLength: 256),
                    StartDate = c.DateTime(storeType: "date"),
                    EndDate = c.DateTime(storeType: "date"),
                    CreatedDate = c.DateTime(nullable: false),
                    CreatedByUserId = c.Int(nullable: false),
                    DelatedDate = c.DateTime(),
                    DeletedByUserId = c.Int(),
                    CommentText = c.String(),
                    IsGoogleCampaign = c.Boolean(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Campaign", t => t.CampaignId, cascadeDelete: true)
                .Index(t => t.CampaignId);

            CreateTable(
                "dbo.Campaign",
                c => new
                {
                    Id = c.String(nullable: false, maxLength: 128),
                    Name = c.String(nullable: false, maxLength: 256),
                    CreatedDate = c.DateTime(nullable: false),
                    CreatedByUserId = c.Int(nullable: false),
                    IsActive = c.Boolean(nullable: false),
                    IsDeleted = c.Boolean(nullable: false),
                    InactivatedOrDeletedByUserId = c.Int(),
                    InactivatedOrDeletedDate = c.DateTime(),
                })
                .PrimaryKey(t => t.Id);

        }

        public override void Down()
        {
            DropForeignKey("dbo.CampaignCode", "CampaignId", "dbo.Campaign");
            DropIndex("dbo.CampaignCode", new[] { "CampaignId" });
            DropTable("dbo.Campaign");
            DropTable("dbo.CampaignCode");
        }
    }
}
