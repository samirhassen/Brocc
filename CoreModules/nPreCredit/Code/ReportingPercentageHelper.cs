using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code
{
    public class ReportingPercentageHelper
    {
        public void GetRoundedListThatSumsCorrectly<T>(List<T> items, Func<T, decimal> getOriginal, Action<T, decimal> setRounded, int nrOfDecimals)
        {
            var roundedValues = GetRoundedListThatSumsCorrectly(items.Select(getOriginal).ToList(), nrOfDecimals);
            foreach (var i in items.Zip(roundedValues, (a, b) => new { a, b }))
            {
                setRounded(i.a, i.b);
            }
        }

        //https://stackoverflow.com/questions/13483430/how-to-make-rounded-percentages-add-up-to-100
        //Using https://en.wikipedia.org/wiki/Largest_remainder_method
        //Basically distribute the badness in a way that minimizes the total error
        public List<decimal> GetRoundedListThatSumsCorrectly(List<decimal> input, int nrOfDecimals)
        {
            var mul = (decimal)Math.Pow(10, nrOfDecimals);

            var listWithDecimals = input.Select(x => x * mul);

            var targetSum = (int)Math.Round(listWithDecimals.Sum());

            var items = listWithDecimals
                .Select((x, i) => new { originalValue = x, i, intPart = (int)Math.Floor(x) });
            var diff = targetSum - items.Sum(x => x.intPart);

            return items
                .OrderByDescending(x => x.originalValue % 1m).Select(x =>
                {
                    if (diff > 0)
                    {
                        diff -= 1;
                        return new { x.i, v = (decimal)(x.intPart + 1) };
                    }
                    else
                        return new { x.i, v = (decimal)x.intPart };
                })
                .OrderBy(x => x.i)
                .Select(x => Math.Round(x.v / mul, nrOfDecimals))
                .ToList();

        }
    }
}