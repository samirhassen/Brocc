using System;
using System.Collections.Generic;
using static NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models.BaseLoanExportRequestModel;

namespace NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models
{
    public class LoanRepaymentsRequestModel
    {
        public TargetEnvironment TargetEnvironment { get; set; }
        public Owner Owner { get; set; }
        public List<Repayment> Repayments { get; set; }
    }

    public class Repayment
    {
        public string ReportReference { get; set; }
        public LoanType LoanType { get; set; }
        public ReportType ReportType { get; set; }
        public LoanNumber LoanNumber { get; set; }
        public DateTime ReportCreationTimeUtc { get; set; }
        public LumpSumLoanRepayment LumpSumLoanRepayment { get; set; }
    }

    public enum ReportType
    {
        Unknown,
        NewReport,
        ErrorCorrection,
        Cancellation
    }

    public class LumpSumLoanRepayment
    {
        public decimal? AmortizationPaid { get; set; }
        public decimal Balance { get; set; }
        public decimal? InterestPaid { get; set; } 
        public decimal? OtherExpenses { get; set; } 
        public string PaymentDate { get; set; } //string for formatting control
    }
}
