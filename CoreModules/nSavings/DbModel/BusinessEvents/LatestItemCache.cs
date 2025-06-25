using System;
using System.Collections.Generic;
using System.Linq;

namespace nSavings.DbModel.BusinessEvents
{
    public class LatestItemCache<TItem, TValue> where TValue : struct
    {
        private readonly List<TItem> orderedItems;
        private readonly Func<TItem, DateTime> getDate;
        private readonly Func<TItem, TValue?> getValue;

        private int nextIndex;
        private DateTime currentDate;
        private TValue? currentValue;

        public LatestItemCache(IEnumerable<TItem> items, Func<TItem, DateTime> getDate,
            Func<TItem, TValue?> getValue, bool itemsIsOrderedAscAlready)
        {
            //BEWARE: This leans on the fact that orderby is a stable sort (this is in the specs) so you change to like .net core or something else make sure the new one is stable as well or multiple items with the same date will have undefined behaviour
            orderedItems = itemsIsOrderedAscAlready ? items.ToList() : items.OrderBy(getDate).ToList();
            this.getValue = getValue;
            this.getDate = getDate;
            currentDate = DateTime.MinValue;
        }

        public TValue? GetCurrentValue(DateTime d)
        {
            if (currentDate == d)
                return currentValue;
            if (currentDate > d)
                throw new Exception("Cannot go back in time using this");

            currentDate = d;
            while (nextIndex < orderedItems.Count)
            {
                var item = orderedItems[nextIndex];
                var itemDate = getDate(item);
                if (itemDate > currentDate)
                    break;
                currentValue = getValue(item);
                nextIndex++;
            }

            return currentValue;
        }
    }

    public static class LatestItemCache
    {
        public static LatestItemCache<TItem, TValue> Create<TItem, TValue>(IEnumerable<TItem> items,
            Func<TItem, DateTime> getDate, Func<TItem, TValue?> getValue, bool itemsIsOrderedAscAlready)
            where TValue : struct
        {
            return new LatestItemCache<TItem, TValue>(items, getDate, getValue, itemsIsOrderedAscAlready);
        }
    }
}