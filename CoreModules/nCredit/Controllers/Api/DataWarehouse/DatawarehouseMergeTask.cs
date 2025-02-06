using NTech;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.Controllers.Api.DataWarehouse
{
    public abstract class DatawarehouseMergeTask
    {
        public abstract bool IsEnabled { get; }

        public abstract void Merge(INTechCurrentUserMetadata currentUser, IClock clock);

        //TODO: Get rid of this whenwe migrate to core
        protected PaymentOrderService CreatePaymentOrderService(INTechCurrentUserMetadata currentUser, IClock clock)
        {
            var contextFactory = new CreditContextFactory(() => new CreditContextExtended(currentUser, clock));
            var cache = new PaymentOrderAndCostTypeCache();
            return new PaymentOrderService(contextFactory, new CustomCostTypeService(contextFactory, cache), cache, NEnv.ClientCfgCore);
        }

        protected static class BinaryComparer
        {
            public static int Compare(byte[] b1, byte[] b2)
            {
                throw new NotImplementedException();
            }
        }

        protected IEnumerable<IEnumerable<T>> SplitIntoGroupsOfN<T>(T[] array, int n)
        {
            for (var i = 0; i < (float)array.Length / n; i++)
            {
                yield return array.Skip(i * n).Take(n);
            }
        }

        protected void RepeatWithGuard(Func<bool> a)
        {
            var guard = 0;
            while (guard++ < 10000)
            {
                var hadResult = a();
                if (!hadResult)
                    return;
            }
            throw new Exception("Hit guard code");
        }

        protected Tuple<int, DateTime, DateTime> GetQuarter(DateTime d)
        {
            if (d.Month <= 3)
                return Tuple.Create(1, new DateTime(d.Year, 1, 1), new DateTime(d.Year, 3, 1).AddMonths(1).AddDays(-1));
            else if (d.Month <= 6)
                return Tuple.Create(2, new DateTime(d.Year, 4, 1), new DateTime(d.Year, 6, 1).AddMonths(1).AddDays(-1));
            else if (d.Month <= 9)
                return Tuple.Create(3, new DateTime(d.Year, 7, 1), new DateTime(d.Year, 9, 1).AddMonths(1).AddDays(-1));
            else
                return Tuple.Create(4, new DateTime(d.Year, 10, 1), new DateTime(d.Year, 12, 1).AddMonths(1).AddDays(-1));
        }
    }
}