using NTech;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.PreCredit.Shared;

namespace nPreCredit.Code.Services
{
    public class PreCreditContextFactoryService : IPreCreditContextFactoryService
    {
        private readonly IClock clock;
        private readonly INTechCurrentUserMetadata currentUserMetadata;

        public PreCreditContextFactoryService(IClock clock, INTechCurrentUserMetadata currentUserMetadata)
        {
            this.clock = clock;
            this.currentUserMetadata = currentUserMetadata;
        }

        public IPreCreditContext Create()
        {
            return new PreCreditContext();
        }

        public PreCreditContextExtended CreateExtendedConcrete()
        {
            return new PreCreditContextExtended(currentUserMetadata, clock);
        }

        public IPreCreditContextExtended CreateExtended()
        {
            return new PreCreditContextExtended(currentUserMetadata, clock);
        }
    }
}