using NTech.Core.Module.Shared.Database;

namespace nCredit
{
    public enum SystemItemCode
    {
        DwLatestMergedTimestamp_Dimension_Credit,
        DwLatestMergedTimestamp_Fact_CreditSnapshot,
        DwLatestMergedTimestamp_Fact_CreditBalanceEvent,
        DwQuarterlyRATI_LatestCompleteQuarterToDate,
        DwMonthlyLiquidityExposure_LatestCompleteMonth,
        TrapetsAml_LatestTimestamp_Account,
        TrapetsAml_LatestTimestamp_Asset,
        TrapetsAml_LatestTimestamp_Customer,
        TrapetsAml_LatestTimestamp_Transaction,
        TrapetsAml_LatestTimestamp_KycQuestionsAndAnswers,
        DwQuarterlyRATIBusinessEvents_LatestCompleteQuarterToDate,
        DwLatestPaymentFileBusinessEventId_Fact_CreditOutgoingPayment,
        DwLatestMergedTimestamp_Fact_CreditNotificationState,
        DwLatestNewCreditBusinessEventId_Fact_InitialEffectiveInterestRate,
        Dw_LatestMergedBusinessEventId_CreditNotificationBalanceSnapshot,
        UcCreditRegistry_Daily_LatestReportedBusinessEventId,
        Cm1Aml_LatestId_Transaction,
        Cm1Aml_LatestId_Customer,
        PositiveCreditRegisterExport_ExportRun,
        PositiveCreditRegisterExport_NewLoans,
        PositiveCreditRegisterExport_LoanChanges,
        PositiveCreditRegisterExport_LoanRepayments,
        PositiveCreditRegisterExport_DelayedRepayments,
        PositiveCreditRegisterExport_TerminatedLoans,
        PositiveCreditRegisterExport_CheckBatchStatus
    }

    public class SystemItem : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
}