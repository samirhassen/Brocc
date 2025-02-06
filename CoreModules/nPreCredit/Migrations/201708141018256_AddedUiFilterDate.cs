namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedUiFilterDate : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CreditApplicationHeader", "HideFromManualListsUntilDate", c => c.DateTimeOffset(precision: 7));
            CreateIndex("dbo.CreditApplicationHeader", "HideFromManualListsUntilDate");
            Sql("DROP INDEX CategoryCountIdx1 ON [dbo].[CreditApplicationHeader]");
            Sql("CREATE NONCLUSTERED INDEX CategoryCountIdx2 ON [dbo].[CreditApplicationHeader] ([IsActive],[IsFinalDecisionMade],[HideFromManualListsUntilDate]) INCLUDE ([ApplicationNr],[IsPartiallyApproved],[CreditCheckStatus],[CustomerCheckStatus],[AgreementStatus],[FraudCheckStatus],[WaitingForAdditionalInformationDate])");
        }

        public override void Down()
        {
            DropIndex("dbo.CreditApplicationHeader", new[] { "HideFromManualListsUntilDate" });
            DropColumn("dbo.CreditApplicationHeader", "HideFromManualListsUntilDate");
            Sql("DROP INDEX CategoryCountIdx2 ON [dbo].[CreditApplicationHeader]");
            Sql("CREATE NONCLUSTERED INDEX CategoryCountIdx1 ON [dbo].[CreditApplicationHeader] ([IsActive],[IsFinalDecisionMade]) INCLUDE ([ApplicationNr],[ApplicationDate],[IsPartiallyApproved],[CreditCheckStatus],[CustomerCheckStatus],[AgreementStatus],[FraudCheckStatus],[WaitingForAdditionalInformationDate])");
        }
    }
}
