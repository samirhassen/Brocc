using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    public static class SetExtensions
    {
        public static void AddRange<T>(this ISet<T> source, IEnumerable<T> items)
        {
            if (source == null || items == null)
                return;
            foreach (var i in items)
                source.Add(i);
        }

        public static void RemoveAll<T>(this ISet<T> source, Func<T, bool> predicate)
        {
            if (source == null || predicate == null)
                return;
            var itemsToRemove = source.Where(predicate).ToList();
            foreach (var i in itemsToRemove)
                source.Remove(i);
        }

        public static HashSet<TSource> ToHashSetShared<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
                return null;
            return new HashSet<TSource>(source);
        }
    }
}