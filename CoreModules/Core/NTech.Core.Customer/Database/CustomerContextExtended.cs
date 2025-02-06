using NTech.Core.Customer.Shared.Database;
using NTech.Core.Module.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;

namespace NTech.Core.Customer.Database
{
    public class CustomerContextExtended : CustomerContext, ICustomerContextExtended
    {
        public CustomerContextExtended(INTechCurrentUserMetadata currentUser, ICoreClock clock)
        {
            CurrentUser = currentUser;
            CoreClock = clock;
        }

        public INTechCurrentUserMetadata CurrentUser { get; }
        public ICoreClock CoreClock { get; }

        public T FillInfrastructureFields<T>(T b) where T : InfrastructureBaseItem
        {
            b.ChangedById = CurrentUser.UserId;
            b.ChangedDate = CoreClock.Now;
            b.InformationMetaData = CurrentUser.InformationMetadata;
            return b;
        }
    }
}
