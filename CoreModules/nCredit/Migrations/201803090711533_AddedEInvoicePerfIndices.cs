namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedEInvoicePerfIndices : DbMigration
    {
        public override void Up()
        {
            Sql("CREATE INDEX [DatedCreditStringPerfIdx1] ON [dbo].[DatedCreditString]([CreditNr] ASC,[Name] ASC,[TransactionDate] ASC,[Timestamp] ASC) INCLUDE ([Value])");
        }

        public override void Down()
        {
            Sql("DROP INDEX [DatedCreditStringPerfIdx1] ON [dbo].[DatedCreditString]");
        }
    }
}
