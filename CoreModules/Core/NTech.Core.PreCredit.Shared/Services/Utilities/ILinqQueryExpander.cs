using System.Linq;

namespace NTech.Core.PreCredit.Shared.Services.Utilities
{
    //Hack to allow linqkit AsExpandable to be used in legacy ef but not in core (where it seems to worke fine without)
    //This is needed when 'The LINQ expression node type 'Invoke' is not supported in LINQ to Entities.' occurs.
    public interface ILinqQueryExpander
    {
        IQueryable<T> AsExpandable<T>(IQueryable<T> query);
        bool IsExpansionNeeded { get; }
    }

    public class LinqQueryExpanderDoNothing : ILinqQueryExpander
    {
        public bool IsExpansionNeeded => false;
        public IQueryable<T> AsExpandable<T>(IQueryable<T> query) => query;

        public static LinqQueryExpanderDoNothing SharedInstance { get; } = new LinqQueryExpanderDoNothing();
    }
}
