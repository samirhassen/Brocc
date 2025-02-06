using NTech.Core.Module.Shared.Clients;

namespace nPreCredit.Code.Services
{
    public class ReferenceInterestRateService : IReferenceInterestRateService
    {
        private readonly ICreditClient creditClient;

        public ReferenceInterestRateService(ICreditClient creditClient)
        {
            this.creditClient = creditClient;
        }

        public decimal GetCurrent()
        {
            return creditClient.GetCurrentReferenceInterest();
        }
    }

    public interface IReferenceInterestRateService
    {
        decimal GetCurrent();
    }
}