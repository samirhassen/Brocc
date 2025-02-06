using System;
using System.Collections.Generic;

namespace nCredit
{
    public class Quarter : IEquatable<Quarter>
    {
        public static Quarter FromYearAndOrdinal(int year, int inYearOrdinalNr)
        {
            switch (inYearOrdinalNr)
            {
                case 1:
                    return new Quarter
                    {
                        InYearOrdinalNr = inYearOrdinalNr,
                        FromDate = new DateTime(year, 1, 1),
                        ToDate = new DateTime(year, 3, 1).AddMonths(1).AddDays(-1),
                        Name = $"Q1_{year}"
                    };
                case 2:
                    return new Quarter
                    {
                        InYearOrdinalNr = inYearOrdinalNr,
                        FromDate = new DateTime(year, 4, 1),
                        ToDate = new DateTime(year, 6, 1).AddMonths(1).AddDays(-1),
                        Name = $"Q2_{year}"
                    };
                case 3:
                    return new Quarter
                    {
                        InYearOrdinalNr = inYearOrdinalNr,
                        FromDate = new DateTime(year, 7, 1),
                        ToDate = new DateTime(year, 9, 1).AddMonths(1).AddDays(-1),
                        Name = $"Q3_{year}"
                    };
                case 4:
                    return new Quarter
                    {
                        InYearOrdinalNr = inYearOrdinalNr,
                        FromDate = new DateTime(year, 10, 1),
                        ToDate = new DateTime(year, 12, 1).AddMonths(1).AddDays(-1),
                        Name = $"Q4_{year}"
                    };
                default:
                    throw new ArgumentException("inYearOrdinalNr < 1 || inYearOrdinalNr > 4", "inYearOrdinalNr");
            }
        }

        public static Quarter ContainingDate(DateTime d)
        {
            if (d.Month <= 3)
                return FromYearAndOrdinal(d.Year, 1);
            else if (d.Month <= 6)
                return FromYearAndOrdinal(d.Year, 2);
            else if (d.Month <= 9)
                return FromYearAndOrdinal(d.Year, 3);
            else
                return FromYearAndOrdinal(d.Year, 4);
        }

        public int InYearOrdinalNr { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Name { get; set; }

        public Quarter GetNext()
        {
            return ContainingDate(ToDate.AddDays(1));
        }

        public Quarter GetPrevious()
        {
            return ContainingDate(FromDate.AddDays(-1));
        }

        public static IEnumerable<Quarter> GetAllBetween(DateTime fromDate, DateTime toDate)
        {
            if (fromDate > toDate)
                throw new Exception("fromDate > toDate");

            var lastQuarter = ContainingDate(toDate);

            var q = ContainingDate(fromDate);
            var guard = 0;
            while (guard++ < 1000)
            {
                yield return q;

                if (q.Equals(lastQuarter))
                    break;
                else if (guard > 1000)
                    throw new Exception("Hit infinite loop guard.");

                q = q.GetNext();
            }
        }

        public override string ToString()
        {
            return $"Q{InYearOrdinalNr}-{FromDate.Year}";
        }

        public override int GetHashCode()
        {
            return ToDate.GetHashCode();
        }

        public bool Equals(Quarter other)
        {
            return other.ToDate.Equals(ToDate);
        }

        public bool ContainsDate(DateTime startDate)
        {
            var d = startDate.Date;
            return d >= FromDate && d <= ToDate;
        }
    }
}