namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedApplicationsFilterIndex : DbMigration
    {
        public override void Up()
        {
            Sql("CREATE NONCLUSTERED INDEX CategoryCountIdx1 ON [dbo].[CreditApplicationHeader] ([IsActive],[IsFinalDecisionMade]) INCLUDE ([ApplicationNr],[ApplicationDate],[IsPartiallyApproved],[CreditCheckStatus],[CustomerCheckStatus],[AgreementStatus],[FraudCheckStatus],[WaitingForAdditionalInformationDate])");
        }

        public override void Down()
        {
            Sql("DROP INDEX CategoryCountIdx1 ON [dbo].[CreditApplicationHeader]");
        }
    }
}
