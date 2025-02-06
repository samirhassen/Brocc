using System;
using System.Collections.Generic;
using static NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models.BaseLoanExportRequestModel;

namespace NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models
{
    public class TerminatedLoansRequestModel
    {
        public TargetEnvironment TargetEnvironment { get; set; }
        public Owner Owner { get; set; }
        public List<TerminateLoanDto> LoanTerminations { get; set; }
    }

    public class TerminateLoanDto
    {
        public string ReportReference { get; set; }
        public ReportType ReportType { get; set; }
        public LoanNumber LoanNumber { get; set; }
        public TerminationDto Termination { get; set; }
    }

    public class TerminationDto
    {
        public bool IsTerminated { get; set; }
        public string EndDate { get; set; } //string for formatting control
        public bool IsTransferredToAnotherLender { get; set; }
    }
}
