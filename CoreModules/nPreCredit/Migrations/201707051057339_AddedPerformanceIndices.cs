namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedPerformanceIndices : DbMigration
    {
        public override void Up()
        {
            this.Sql("CREATE INDEX [CreditApplicationComment_PerfIdx1] ON [dbo].[CreditApplicationComment] ([ApplicationNr] ASC, [EventType] ASC)");
            this.Sql("CREATE INDEX [CreditApplicationComment_PerfIdx2] ON [dbo].[CreditApplicationComment] ([EventType] ASC, [ApplicationNr] ASC) INCLUDE ([CommentDate])");
            this.Sql("CREATE INDEX [CreditApplicationOneTimeToken_PerfIdx1] ON [dbo].[CreditApplicationOneTimeToken]([ApplicationNr] ASC, [Token] ASC, [TokenType] ASC)");
            this.Sql("CREATE INDEX [CreditApplicationHeader_PerfIdx1] ON [dbo].[CreditApplicationHeader]([IsActive] ASC,[CreditCheckStatus] ASC,[CustomerCheckStatus] ASC,[AgreementStatus] ASC,[FraudCheckStatus] ASC,[IsPartiallyApproved] ASC)INCLUDE ([ApplicationNr],[ProviderName],[ApplicationDate],[IsFinalDecisionMade])");
            this.Sql("CREATE INDEX [CreditApplicationItem_PerfIdx1] ON [dbo].[CreditApplicationItem]([ApplicationNr] ASC,[Id] ASC)INCLUDE ([AddedInStepName])");
        }

        public override void Down()
        {
            this.Sql("DROP INDEX [CreditApplicationComment_PerfIdx1] ON [dbo].[CreditApplicationComment]");
            this.Sql("DROP INDEX [CreditApplicationComment_PerfIdx2] ON [dbo].[CreditApplicationComment]");
            this.Sql("DROP INDEX [CreditApplicationOneTimeToken_PerfIdx1] ON [dbo].[CreditApplicationOneTimeToken]");
            this.Sql("DROP INDEX [CreditApplicationHeader_PerfIdx1] ON [dbo].[CreditApplicationHeader]");
            this.Sql("DROP INDEX [CreditApplicationItem_PerfIdx1] ON [dbo].[CreditApplicationItem]");
        }
    }
}
