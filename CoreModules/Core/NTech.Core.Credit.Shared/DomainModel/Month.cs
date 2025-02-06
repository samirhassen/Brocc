using System;

namespace NTech.Core.Credit.Shared.DomainModel
{
    public class Month : IEquatable<Month>
    {
        private DateTime anyDateInMonth;
        private string CanonicalValue => $"{anyDateInMonth.ToString("yyyy-MM")}";
        public override string ToString() => CanonicalValue;
        public override int GetHashCode() => CanonicalValue.GetHashCode();
        public bool Equals(Month other) => other.CanonicalValue.Equals(CanonicalValue);
        public DateTime FirstDate => anyDateInMonth.AddDays(-(anyDateInMonth.Day - 1)).Date;
        public DateTime LastDate => FirstDate.AddMonths(1).AddDays(-1).Date;
        public static Month ContainingDate(DateTime d) => new Month { anyDateInMonth = d };
        public Month NextMonth => AddMonths(1);
        public Month AddMonths(int count) => ContainingDate(anyDateInMonth.AddMonths(count));
        public Month PreviousMonth => ContainingDate(anyDateInMonth.AddMonths(-1));
        public DateTime GetDayDate(int dayNr) => FirstDate.AddDays(dayNr - 1);
        public int Year => anyDateInMonth.Year;
        public int MonthNr => anyDateInMonth.Month;
        public static int NrOfMonthsBetween(Month m1, Month m2) => Dates.GetAbsoluteNrOfMonthsBetweenDates(m1.FirstDate, m2.FirstDate);
    }
}
