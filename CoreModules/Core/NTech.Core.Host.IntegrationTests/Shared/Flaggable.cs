
namespace NTech.Core.Host.IntegrationTests.Shared
{
    internal class Flaggable<T>
    {
        public Flaggable(T item)
        {
            Item = item;
        }

        public T Item { get; set; }
        public bool IsFlagged { get; set; }
    }

    internal class Flaggable
    {
        public static Flaggable<U> Create<U>(U item) => new Flaggable<U>(item);
    }
}
