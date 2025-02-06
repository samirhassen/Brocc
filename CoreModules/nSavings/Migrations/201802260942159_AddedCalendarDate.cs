namespace nSavings.Migrations
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
            Sql("create index [DatedSavingsAccountStringPerfIdx1] on [dbo].[DatedSavingsAccountString] ([SavingsAccountNr] asc, [BusinessEventId] asc, [Id] asc, [Name] asc, [Value] asc, [TransactionDate] asc)");
        }

        public override void Down()
        {
            Sql("drop index [DatedSavingsAccountStringPerfIdx1] on [dbo].[DatedSavingsAccountString]");

            Sql("drop index CalendarDateAscIdx on dbo.CalendarDate");
            Sql("drop index CalendarDateDescIdx on dbo.CalendarDate");

            DropTable("dbo.CalendarDate");
        }
    }
}
