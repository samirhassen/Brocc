using nPreCredit.Code;
using NTech.Core.PreCredit.Shared.Services.UlLegacy;

namespace NTech.Core.PreCredit.Shared.Code.PetrusOnlyScoringService
{
    public class PetrusOnlyCreditCheckRequest
    {
        public PetrusOnlyCreditCheckService.ScoringDataContext DataContext { get; set; }
        public string ProviderName { get; set; }
        public decimal ReferenceInterestRatePercent { get; set; }
    }
}
