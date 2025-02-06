using static NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models.BaseLoanExportRequestModel;

namespace NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models
{
    public class GetLoanRequestModel
    {
        public TargetEnvironment TargetEnvironment { get; set; }
        public Owner Owner { get; set; }
        public LoanNumber LoanNumber { get; set; }
    }
}
