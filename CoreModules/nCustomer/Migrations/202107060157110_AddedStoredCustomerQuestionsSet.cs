namespace nCustomer.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedStoredCustomerQuestionsSet : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.StoredCustomerQuestionSet",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CustomerId = c.Int(nullable: false),
                    AnswerDate = c.DateTime(nullable: false),
                    SourceType = c.String(nullable: false, maxLength: 128),
                    SourceId = c.String(nullable: false, maxLength: 128),
                    KeyValueStorageKeySpace = c.String(nullable: false, maxLength: 128),
                    KeyValueStorageKey = c.String(nullable: false, maxLength: 128),
                    Timestamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    ChangedById = c.Int(nullable: false),
                    ChangedDate = c.DateTimeOffset(nullable: false, precision: 7),
                    InformationMetaData = c.String(),
                })
                .PrimaryKey(t => t.Id);

        }

        public override void Down()
        {
            DropTable("dbo.StoredCustomerQuestionSet");
        }
    }
}
