using System;
using System.Collections.Generic;
using System.Linq;

namespace nSavings.DbModel.BusinessEvents
{
    /// <summary>
    /// When computing the total sum of a set of transactions of consecutive days (like when computing interst)
    /// this does one sort and then just keeps a running total while stepping dates forward making it n*logn instead of n*n.
    /// </summary>
    public class RunningTotalCache<T>
    {
        private readonly List<T> orderedItems;
        private readonly Func<T, DateTime> getDate;
        private readonly Func<T, decimal> getAmount;

        private int nextIndex = 0;
        private DateTime currentDate;
        private decimal currentSum = 0m;

        public RunningTotalCache(IEnumerable<T> items, Func<T, DateTime> getDate, Func<T, decimal> getAmount, bool itemsIsOrderedAscAlready)
        {
            this.orderedItems = itemsIsOrderedAscAlready ? items.ToList() : items.OrderBy(getDate).ToList();
            this.getAmount = getAmount;
            this.getDate = getDate;
            this.currentDate = DateTime.MinValue;
        }

        public decimal GetRunningTotal(DateTime d)
        {
            if (currentDate == d)
                return currentSum;
            if (currentDate > d)
                throw new Exception("Cannot go back in time using this");

            currentDate = d;
            while (nextIndex < orderedItems.Count)
            {
                var item = orderedItems[nextIndex];
                var itemDate = getDate(item);
                if (itemDate > currentDate)
                    break;

                currentSum += getAmount(item);
                nextIndex++;
            }
            return currentSum;
        }
    }
    public static class RunningTotalCache
    {
        public static RunningTotalCache<T> Create<T>(IEnumerable<T> items, Func<T, DateTime> getDate, Func<T, decimal> getAmount, bool itemsIsOrderedAscAlready)
        {
            return new RunningTotalCache<T>(items, getDate, getAmount, itemsIsOrderedAscAlready);
        }
    }
}