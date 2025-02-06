using Dapper;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Data;

namespace NTech.Core.Credit.Shared.Services
{
    public class CalendarDateService
    {
        private readonly DateTime earliestCalendarDate;
        private readonly CreditContextFactory creditContextFactory;
        public static readonly DateTime EarliestCalendarDateProduction = new DateTime(1950, 1, 1);

        public CalendarDateService(DateTime earliestCalendarDate, CreditContextFactory creditContextFactory)
        {
            this.earliestCalendarDate = earliestCalendarDate;
            this.creditContextFactory = creditContextFactory;
        }

        public void EnsureCalendarDates(DateTime toDate)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                EnsureCalendarDatesComposable(earliestCalendarDate, toDate, context.GetConnection());
            }
        }

        private static void EnsureCalendarDatesComposable(DateTime fromDate, DateTime toDate, IDbConnection connection)
        {
            if (toDate <= fromDate)
                throw new NTechCoreWebserviceException("toDate <= fromDate") { IsUserFacing = true, ErrorHttpStatusCode = 400, ErrorCode = "dateIntervalError" };

            void AddTenYearsToCalendar(DateTime startFromDate, DateTime endAtDate)
            {
                connection.Execute(
@"WITH Dates 
AS 
(
SELECT	TheDate = @startFromDate
UNION ALL 
SELECT	TheDate = DATEADD(DAY, 1, TheDate)
FROM	Dates
WHERE	TheDate < @endAtDate
)
insert into CalendarDate
(TheDate)
select d.TheDate from Dates d 
OPTION (MAXRECURSION 6000)", new { startFromDate, endAtDate });
            };
            int loopGuard = 0;
            while (loopGuard++ < 500)
            {
                var maxDate = connection.QueryFirst<DateTime?>("select max(TheDate) from CalendarDate");
                if (!maxDate.HasValue || maxDate < toDate)
                {
                    var startFromDate = maxDate.HasValue ? maxDate.Value.AddDays(1) : fromDate;
                    var endAtDate = Dates.Min(startFromDate.AddYears(10), toDate);
                    AddTenYearsToCalendar(maxDate.HasValue ? maxDate.Value.AddDays(1) : fromDate, endAtDate);
                }
                else
                    break;
            }
            if (loopGuard > 400)
                throw new Exception("Hit guard code. Infinite loop detected");
        }

        private class MinMaxDate
        {
            public DateTime? MinDate { get; set; }
            public DateTime? MaxDate { get; set; }
        }
    }
}
