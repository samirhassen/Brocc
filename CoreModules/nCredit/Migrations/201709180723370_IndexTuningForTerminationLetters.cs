namespace nCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class IndexTuningForTerminationLetters : DbMigration
    {
        public override void Up()
        {
            Sql(
@"SET ANSI_PADDING ON

go

CREATE NONCLUSTERED INDEX [_dta_index_AccountTransaction_7_405576483__K2_K3_K1_K4_9] ON [dbo].[AccountTransaction]
(
	[AccountCode] ASC,
	[CreditNotificationId] ASC,
	[Id] ASC,
	[BusinessEventId] ASC
)
INCLUDE ( 	[Amount]) WITH (SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
go

SET ANSI_PADDING ON

go

CREATE NONCLUSTERED INDEX [_dta_index_CreditNotificationHeader_7_341576255__K12_K1_K2_K4] ON [dbo].[CreditNotificationHeader]
(
	[ClosedTransactionDate] ASC,
	[Id] ASC,
	[CreditNr] ASC,
	[DueDate] ASC
)WITH (SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
go

SET ANSI_PADDING ON

go

CREATE NONCLUSTERED INDEX [_dta_index_DatedCreditDate_7_466100701__K7D_K6_1_2_3_11] ON [dbo].[DatedCreditDate]
(
	[Timestamp] DESC,
	[Value] ASC
)
INCLUDE ( 	[Id],
	[CreditNr],
	[Name],
	[RemovedByBusinessEventId]) WITH (SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
go

SET ANSI_PADDING ON

go

CREATE NONCLUSTERED INDEX [_dta_index_CreditTerminationLetterHeader_7_338100245__K6_K4_1] ON [dbo].[CreditTerminationLetterHeader]
(
	[CreditNr] ASC,
	[DueDate] ASC
)
INCLUDE ( 	[Id]) WITH (SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
go");
        }

        public override void Down()
        {
            Sql(@"drop index [_dta_index_AccountTransaction_7_405576483__K2_K3_K1_K4_9] ON [dbo].[AccountTransaction]");
            Sql(@"drop index [_dta_index_CreditNotificationHeader_7_341576255__K12_K1_K2_K4] ON [dbo].[CreditNotificationHeader]");
            Sql(@"drop index [_dta_index_DatedCreditDate_7_466100701__K7D_K6_1_2_3_11] ON [dbo].[DatedCreditDate]");
            Sql(@"drop index [_dta_index_CreditTerminationLetterHeader_7_338100245__K6_K4_1] ON [dbo].[CreditTerminationLetterHeader]");
        }
    }
}
