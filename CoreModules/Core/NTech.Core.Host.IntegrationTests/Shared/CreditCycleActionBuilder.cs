namespace NTech.Core.Host.IntegrationTests.Shared
{
    internal class CreditCycleActionBuilder<TSupport> where TSupport : CreditSupportShared
    {
        private CreditCycleAction<TSupport> a = new CreditCycleAction<TSupport>();
        internal int? forMonthNr = 1; //nullable so we can remove it in End(). This is to prevent using this instead of t.MonthNr from inside the assertions as fromMonthNr will always be the last month

        internal static CreditCycleActionBuilder<TSupport> Begin() => new CreditCycleActionBuilder<TSupport>();
        internal CreditCycleAction<TSupport> End()
        {
            forMonthNr = null;
            return a;
        }

        internal CreditCycleActionBuilder<TSupport> AddAction(int dayNr, Action<(TSupport Support, int MonthNr, int DayNr, string Prefix)> action)
        {
            a.AddAction(forMonthNr, dayNr, action);
            return this;
        }

        public CreditCycleActionBuilder<TSupport> ForMonth(int monthNr)
        {
            forMonthNr = monthNr;
            return this;
        }
    }

    internal class CreditCycleAction<TSupport> where TSupport : CreditSupportShared
    {
        public Dictionary<int, Dictionary<int, List<Action<(TSupport Support, int MonthNr, int DayNr, string Prefix)>>>> actionByMonthNrAndDayNr = new Dictionary<int, Dictionary<int, List<Action<(TSupport Support, int MonthNr, int DayNr, string Prefix)>>>>();

        public int MaxMonthNr => actionByMonthNrAndDayNr.Keys.Count == 0 ? 1 : actionByMonthNrAndDayNr.Keys.Max();

        public void AddAction(int? monthNrPre, int dayNr, Action<(TSupport Support, int MonthNr, int DayNr, string Prefix)> a)
        {
            var monthNr = monthNrPre!.Value;
            if (!actionByMonthNrAndDayNr.ContainsKey(monthNr))
                actionByMonthNrAndDayNr.Add(monthNr, new Dictionary<int, List<Action<(TSupport Support, int MonthNr, int DayNr, string Prefix)>>>());

            var month = actionByMonthNrAndDayNr[monthNr];

            if (!month.ContainsKey(dayNr))
                month[dayNr] = new List<Action<(TSupport Support, int MonthNr, int DayNr, string Prefix)>>();

            month[dayNr].Add(a);
        }

        public void ExecuteActions(TSupport support, int monthNr, int dayNr)
        {
            var actions = actionByMonthNrAndDayNr?.Opt(monthNr)?.Opt(dayNr);
            if (actions == null || actions.Count == 0)
                return;

            using (var context = support.CreateCreditContextFactory().CreateContext())
            {
                foreach (var action in actions)
                    action((Support: support, MonthNr: monthNr, DayNr: dayNr, Prefix: $"action - monthNr={monthNr}, dayNr={dayNr}: "));
            }
        }
    }
}
