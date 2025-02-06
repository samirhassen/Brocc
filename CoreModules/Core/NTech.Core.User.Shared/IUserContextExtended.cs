using NTech.Core.Module.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using nUser.DbModel;
using System.Linq;

namespace NTech.Core.User.Shared
{
    public interface IUserContextExtended : IUserContext, INTechDbContext
    {
        T FillInfrastructureFields<T>(T b) where T : InfrastructureBaseItem;
        ICoreClock CoreClock { get; }
        INTechCurrentUserMetadata CurrentUser { get; }
    }

    public interface IUserContext : INTechDbContext
    {
        IQueryable<KeyValueItem> KeyValueItemsQueryable { get; }
        void RemoveKeyValueItem(KeyValueItem item);
        void AddKeyValueItem(KeyValueItem item);

        int SaveChanges();
    }

}
