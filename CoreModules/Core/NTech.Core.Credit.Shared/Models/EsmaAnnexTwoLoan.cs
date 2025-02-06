using System;

namespace NTech.Core.Credit.Shared.Models
{
    public class EsmaAnnexTwoLoan
    {
        /// <summary>
        /// Original/New Underlying Exposure Identifier (RREL2, RREL3)
        /// </summary>
        public string CreditNr { get; set; }

        /// <summary>
        /// Original/New Obligor Identifier
        /// </summary>
        public int MainCustomerId { get; set; }

        /// <summary>
        /// Current loan owner name. See also LoanOwnerDate
        /// </summary>
        public string LoanOwnerName { get; set; }

        /// <summary>
        /// Pool Addition Date (RREL7)
        /// When the current loan owner name was set in the system
        /// </summary>
        public DateTime? LoanOwnerDate { get; set; }

        /// <summary>
        /// Closed date. Can compute Redemption Date (RREL9) when ClosedStatus = Settled and Default Date (RREL72) when ClosedStatus = SentToDebtCollection
        /// When the loan was closed
        /// </summary>
        public DateTime? ClosedDate { get; set; }

        /// <summary>
        /// Closed status. Settled or SentToDebtCollection
        /// </summary>
        public string ClosedStatus { get; set; }

        /// <summary>
        /// Origination Date (RREL23)
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Maturity Date (RREL24)
        /// </summary>
        public DateTime? CurrentLastAmortizationPlanDueDate { get; set; }

        /// <summary>
        /// Latest sent notification due date
        /// </summary>
        public DateTime? LatestNotificationDueDate { get; set; }

        /// <summary>
        /// Same as LatestNotificationDueDate if that date is in the future otherwise the next due date after that
        /// RREL39 - Payment Due
        /// </summary>
        public DateTime? NextFutureNotificationDueDate { get; set; }

        /// <summary>
        /// Amortization per month. Note that this will not change if the customer has an amortization exception. See also CurrentAmortizationExceptionUntilDate
        /// </summary>
        public decimal CurrentMonthlyAmortizationAmount { get; set; }

        /// <summary>
        /// Until this date the customer pays CurrentExceptionAmortizationAmount rather than CurrentMonthlyAmortizationAmount.
        ///  This + CurrentExceptionAmortizationAmount = 0 could be one source of RREL36 
        /// </summary>
        public DateTime? CurrentAmortizationExceptionUntilDate { get; set; }

        /// <summary>
        ///  Until CurrentAmortizationExceptionUntilDate the customer pays this rather than CurrentMonthlyAmortizationAmount.
        ///  This = 0 + CurrentAmortizationExceptionUntilDate could be one source of RREL36 
        /// </summary>
        public decimal? CurrentExceptionAmortizationAmount { get; set; }

        /// <summary>
        /// The theoretical end date from the loan agreement. (Typically ~40 years from the agreement date)
        /// </summary>
        public DateTime? CurrentLoanAgreementEndDate { get; set; }

        /// <summary>
        /// Initial capital/principal
        /// Original Principal Balance - RREL29
        /// </summary>
        public decimal InitialCapitalDebt { get; set; }

        /// <summary>
        /// Current capital/principal yet to be paid.
        /// Differs from CurrentNotNotifiedCapitalDebt in that CurrentCapitalDebt just cares about payments and writeoffs.
        /// Current Principal Balance - RREL30
        /// </summary>
        public decimal CurrentCapitalDebt { get; set; }

        /// <summary>
        /// Current capital/principal yet to be notified.
        /// Writeoffs and extra amortization work the same as CurrentCapitalDebt but this counts down when a notification is created rather than when it's paid.
        /// </summary>
        public decimal CurrentNotNotifiedCapitalDebt { get; set; }

        /// <summary>
        /// Repayment time when the loan was created (Original Term - RREL25)
        /// </summary>
        public int InitialRepaymentTimeInMonths { get; set; }

        /// <summary>
        /// Customers current difference from the list interest in percent.
        /// RREL43 Current Interest rate together with CurrentReferenceInterestRate
        /// </summary>
        public decimal CurrentMarginInterestRate { get; set; }

        /// <summary>
        /// Customers current list interest in percent.
        /// RREL43 Current Interest rate together with CurrentMarginInterestRate
        /// </summary>
        public decimal CurrentReferenceInterestRate { get; set; }

        /// <summary>
        /// Interest Rate Reset Interval RREL47
        /// </summary>
        public int CurrentInterestRebindMonthCount { get; set; }

        /// <summary>
        /// Next interest rebind date
        /// Interest Revision Date 1 - RREL51
        /// </summary>
        public DateTime? NextInterestRebindDate { get; set; }

        /// <summary>
        /// Latest extra amortization (capital/principal payment not against a notification) date
        /// RREL63 Prepayment Date
        /// </summary>
        public DateTime? LatestExtraAmortizationDate { get; set; }

        /// <summary>
        /// Latest term change date
        /// </summary>
        public DateTime? LatestTermChangeDate { get; set; }

        /// <summary>
        /// Latest missed due date
        /// Could be a notification that is still open or if all are paid or not overdue yet then then last closed one that got paid late.
        /// Late here uses the same nr of grace days as the notification process does.
        /// Date Last In Arrears - RREL66
        /// </summary>
        public DateTime? LatestMissedDueDate { get; set; }

        /// <summary>
        /// Days since the duedate of the oldest still unpaid notification was passed or zero if all are paid or not overdue yet.
        /// Number Of Days In Arrears - RREL68
        /// </summary>
        public int CurrentNrOfOverdueDays { get; set; }

        /// <summary>
        /// If ClosedStatus is SentToDebtCollection this will contain the amount of capital/principal written off as part of the debt collection export
        /// Default Amount - RREL71
        /// </summary>
        public decimal? DebtCollectionExportCapitalAmount { get; set; }

        /// <summary>
        /// Total written of capital/principal
        /// RREL73 Allocated Losses
        /// </summary>
        public decimal? TotalWrittenOffCapitalAmount { get; set; }

        /// <summary>
        /// Total written of interest
        /// RREL73 Allocated Losses
        /// </summary>
        public decimal? TotalWrittenOffInterestAmount { get; set; }

        /// <summary>
        /// Total written of fees
        /// RREL73 Allocated Losses
        /// </summary>
        public decimal? TotalWrittenOffFeesAmount { get; set; }

        /// <summary>
        /// Collateral id
        /// Original/New Collateral Identifier RREC3, RREC4
        /// </summary>
        public int CollateralId { get; set; }

        /// <summary>
        /// Collateral type code (seBrf or seFastighet)
        /// RREC5
        /// </summary>
        public string CollateralTypeCode { get; set; }

        /// <summary>
        /// Value of the collateral in the latest snapshot of the amortization basis
        /// RREC13
        /// </summary>
        public decimal? CurrentAmortizationBasisCollateralValue { get; set; }

        /// <summary>
        /// Valuation date for CurrentAmortizationBasisCollateralValue
        /// RREC15
        /// </summary>
        public DateTime? CurrentAmortizationBasisCollateralValueDate { get; set; }

        /// <summary>
        /// LTV as a fraction from the initial amortization basis
        /// Original Loan-To-Value RREC16
        /// </summary>
        public decimal? InitialAmortizationBasiLtvFraction { get; set; }

        /// <summary>
        /// Value of the collateral in the initial snapshot of the amortization basis
        /// RREC17 Original Valuation Amount
        /// </summary>
        public decimal? InitialAmortizationBasisCollateralValue { get; set; }

        /// <summary>
        /// Valuation date for InitialAmortizationBasisCollateralValue
        /// RREC19 Original Valuation Date
        /// </summary>
        public DateTime? InitialAmortizationBasisCollateralValueDate { get; set; }
    }
}
