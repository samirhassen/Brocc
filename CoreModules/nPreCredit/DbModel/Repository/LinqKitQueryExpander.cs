using LinqKit;
using NTech.Core.PreCredit.Shared.Services.Utilities;
using System.Linq;

namespace nPreCredit.DbModel.Repository
{
    public class LinqKitQueryExpander : ILinqQueryExpander
    {
        public bool IsExpansionNeeded => true;

        public IQueryable<T> AsExpandable<T>(IQueryable<T> query) => query.AsExpandable();

        public static ILinqQueryExpander SharedInstance { get; } = new LinqKitQueryExpander();
    }
}