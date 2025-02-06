namespace nSavings.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class PerformancePass1 : DbMigration
    {
        public override void Up()
        {
            Sql("CREATE INDEX [BusinessEventPerfIdx1] ON [dbo].[BusinessEvent]([Id]) INCLUDE ([TransactionDate])");

            Sql("CREATE INDEX [DatedAccountStrPerfIdx1] ON [dbo].[DatedSavingsAccountString]([TransactionDate] DESC, [Timestamp] DESC) INCLUDE ([SavingsAccountNr], [Name], [ChangedDate])");
            Sql("CREATE INDEX [DatedAccountStrPerfIdx2] ON [dbo].[DatedSavingsAccountString]([BusinessEventId] DESC, [Value] ASC) INCLUDE ([SavingsAccountNr], [Name])");

            Sql("CREATE INDEX [LedgerAccountTransactionPerfIdx1] ON [dbo].[LedgerAccountTransaction]([Timestamp] DESC) INCLUDE ([AccountCode], [SavingsAccountNr])");
            Sql("CREATE INDEX [LedgerAccountTransactionPerfIdx2] ON [dbo].[LedgerAccountTransaction]([AccountCode] ASC, [SavingsAccountNr] ASC) INCLUDE ([Amount])");
            Sql("CREATE INDEX [LedgerAccountTransactionPerfIdx3] ON [dbo].[LedgerAccountTransaction]([AccountCode] ASC, [SavingsAccountNr] ASC,	[TransactionDate] ASC) INCLUDE ([Amount])");

            Sql("CREATE INDEX [SavingsAccountHeaderPerfIdx1] ON [dbo].[SavingsAccountHeader]([SavingsAccountNr] ASC) INCLUDE ([MainCustomerId], [Status],[Timestamp])");
            Sql("CREATE INDEX [SavingsAccountHeaderPerfIdx2] ON [dbo].[SavingsAccountHeader]([CreatedByBusinessEventId] ASC) INCLUDE ([SavingsAccountNr], [Status], [ChangedDate])");
        }

        public override void Down()
        {
            Sql("DROP INDEX [BusinessEventPerfIdx1] ON [dbo].[BusinessEvent]");

            Sql("DROP INDEX [DatedAccountStrPerfIdx1] ON [dbo].[DatedSavingsAccountString]");
            Sql("DROP INDEX [DatedAccountStrPerfIdx2] ON [dbo].[DatedSavingsAccountString]");

            Sql("DROP INDEX [LedgerAccountTransactionPerfIdx1] ON [dbo].[LedgerAccountTransaction]");
            Sql("DROP INDEX [LedgerAccountTransactionPerfIdx2] ON [dbo].[LedgerAccountTransaction]");
            Sql("DROP INDEX [LedgerAccountTransactionPerfIdx3] ON [dbo].[LedgerAccountTransaction]");

            Sql("DROP INDEX [SavingsAccountHeaderPerfIdx1] ON [dbo].[SavingsAccountHeader]");
            Sql("DROP INDEX [SavingsAccountHeaderPerfIdx2] ON [dbo].[SavingsAccountHeader]");
        }
    }
}
