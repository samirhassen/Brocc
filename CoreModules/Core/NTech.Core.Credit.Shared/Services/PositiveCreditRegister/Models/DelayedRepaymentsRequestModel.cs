using System;
using System.Collections.Generic;
using static NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models.BaseLoanExportRequestModel;

namespace NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models
{
    public class DelayedRepaymentsRequestModel 
    {
        public TargetEnvironment TargetEnvironment { get; set; }
        public Owner Owner { get; set; }
        public List<DelayedRepayment> DelayedRepayments { get; set; }
    }

    public class DelayedAmountDto
    {
        public decimal DelayedInstalment { get; set; }
        public string OriginalDueDate { get; set; } //string for formatting control
    }
    
    public class DelayedRepayment
    {
        public string ReportReference { get; set; }
        public LoanNumber LoanNumber { get; set; }
        public ReportType ReportType { get; set; }
        public bool IsDelay { get; set; }
        public List<DelayedAmountDto> DelayedAmounts { get; set; }
        public bool IsForeclosed { get; set; }
        public DateTime? ForeclosureDate { get; set; }
    }
}
