using NTech.Core;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Module.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure;

namespace nCustomer.DbModel
{
    public class CustomersContextExtended : CustomersContext, ICustomerContextExtended
    {
        public CustomersContextExtended(INTechCurrentUserMetadata ntechCurrentUserMetadata)
        {
            CoreClock = new CoreClock();
            CurrentUser = ntechCurrentUserMetadata;
        }

        public ICoreClock CoreClock { get; }
        public INTechCurrentUserMetadata CurrentUser { get; }

        public T FillInfrastructureFields<T>(T b) where T : InfrastructureBaseItem
        {
            b.ChangedById = CurrentUser.UserId;
            b.ChangedDate = CoreClock.Now;
            b.InformationMetaData = CurrentUser.InformationMetadata;
            return b;
        }
    }
}