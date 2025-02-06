namespace nAudit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ChangedToCustomerId : DbMigration
    {
        public override void Up()
        {
            Sql("delete from dbo.PersonalDataViewLogItem");
            AddColumn("dbo.PersonalDataViewLogItem", "CustomerId", c => c.Int());
            CreateIndex("dbo.PersonalDataViewLogItem", "CustomerId");
            DropColumn("dbo.PersonalDataViewLogItem", "EncryptedCivicRegNrId");
        }

        public override void Down()
        {
            AddColumn("dbo.PersonalDataViewLogItem", "EncryptedCivicRegNrId", c => c.Long());
            DropIndex("dbo.PersonalDataViewLogItem", new[] { "CustomerId" });
            DropColumn("dbo.PersonalDataViewLogItem", "CustomerId");
        }
    }
}
