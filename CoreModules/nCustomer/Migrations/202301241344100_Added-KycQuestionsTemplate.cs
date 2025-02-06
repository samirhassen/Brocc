namespace nCustomer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedKycQuestionsTemplate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.KycQuestionTemplate",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        RelationType = c.String(nullable: false, maxLength: 100),
                        CreatedDate = c.DateTimeOffset(nullable: false, precision: 7),
                        CreatedByUserId = c.Int(nullable: false),
                        RemovedDate = c.DateTimeOffset(precision: 7),
                        RemovedByUserId = c.Int(),
                        ModelData = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.KycQuestionTemplate");
        }
    }
}
