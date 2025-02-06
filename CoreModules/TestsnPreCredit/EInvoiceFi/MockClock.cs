using NTech;
using NTech.Core;
using System;

namespace TestsnPreCredit.Credit
{
    public class MockClock : IClock, ICoreClock
    {
        public DateTimeOffset Now => new DateTimeOffset(2018, 2, 19, 13, 58, 3, 12, TimeSpan.FromHours(1));
        public DateTime Today => Now.Date;

        public DateTime HistoricalDate(TimeSpan ago)
        {
            return Today.Date.Subtract(ago).Date;
        }
    }
}
