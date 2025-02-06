namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedBookKeepingExcelKey : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.OutgoingBookkeepingFileHeader", "XlsFileArchiveKey", c => c.String(maxLength: 100));
        }

        public override void Down()
        {
            DropColumn("dbo.OutgoingBookkeepingFileHeader", "XlsFileArchiveKey");
        }
    }
}
