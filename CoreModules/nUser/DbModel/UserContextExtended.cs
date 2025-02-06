using NTech.Core;
using NTech.Core.Module.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.User.Shared;
using NTech.Legacy.Module.Shared.Infrastructure;

namespace nUser.DbModel
{
    public class UserContextExtended : UsersContext, IUserContextExtended
    {
        public UserContextExtended(INTechCurrentUserMetadata ntechCurrentUserMetadata)
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