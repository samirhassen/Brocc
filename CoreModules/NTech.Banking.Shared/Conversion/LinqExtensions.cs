using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Linq
{
    public static class NTechEnumerableExtensions
    {
        /// <summary>
        /// Slower and more wasteful version of distinct that preserves the order
        /// </summary>
        public static IEnumerable<TSource> DistinctPreservingOrder<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
                yield break;

            var k = new HashSet<TSource>();
            foreach (var s in source)
            {
                if (!k.Contains(s))
                {
                    k.Add(s);
                    yield return s;
                }
            }
        }
    }

    public static class Enumerables
    {
        public static IEnumerable<T> Singleton<T>(T item)
        {
            yield return item;
        }

        public static IEnumerable<T> ChainSkipNulls<T>(T[][] items) where T : class
        {
            if (items == null)
                return null;
            return items.SelectMany(SkipNulls);
        }

        public static IEnumerable<T> SkipNulls<T>(params T[] items) where T : class
        {
            return items?.Where(x => x != null);
        }

        public static T[] Array<T>(params T[] items)
        {
            return items;
        }
    }
}