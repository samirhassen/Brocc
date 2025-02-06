using System;
using System.Collections.Generic;
using System.Linq;

namespace nSavings.DbModel.BusinessEvents
{
    public class LatestItemCache<ItemType, ValueType> where ValueType : struct
    {
        private readonly List<ItemType> orderedItems;
        private readonly Func<ItemType, DateTime> getDate;
        private readonly Func<ItemType, ValueType?> getValue;

        private int nextIndex = 0;
        private DateTime currentDate;
        private ValueType? currentValue = null;

        public LatestItemCache(IEnumerable<ItemType> items, Func<ItemType, DateTime> getDate, Func<ItemType, ValueType?> getValue, bool itemsIsOrderedAscAlready)
        {
            //BEWARE: This leans on the fact that orderby is a stable sort (this is in the specs) so you change to like .net core or something else make sure the new one is stable as well or multiple items with the same date will have undefined behaviour
            this.orderedItems = itemsIsOrderedAscAlready ? items.ToList() : items.OrderBy(getDate).ToList();
            this.getValue = getValue;
            this.getDate = getDate;
            this.currentDate = DateTime.MinValue;
        }

        public ValueType? GetCurrentValue(DateTime d)
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
        public static LatestItemCache<ItemType, ValueType> Create<ItemType, ValueType>(IEnumerable<ItemType> items, Func<ItemType, DateTime> getDate, Func<ItemType, ValueType?> getValue, bool itemsIsOrderedAscAlready) where ValueType : struct
        {
            return new LatestItemCache<ItemType, ValueType>(items, getDate, getValue, itemsIsOrderedAscAlready);
        }
    }
}