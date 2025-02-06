namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedCalendarDate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CalendarDate",
                c => new
                {
                    TheDate = c.DateTime(nullable: false, storeType: "date"),
                })
                .PrimaryKey(t => t.TheDate);

            Sql("create index CalendarDateAscIdx on dbo.CalendarDate (TheDate asc)");
            Sql("create index CalendarDateDescIdx on dbo.CalendarDate (TheDate desc)");
        }

        public override void Down()
        {
            Sql("drop index CalendarDateAscIdx on dbo.CalendarDate");
            Sql("drop index CalendarDateDescIdx on dbo.CalendarDate");
            DropTable("dbo.CalendarDate");
        }
    }
}
