using NTech.Core.Credit.Shared.Database;
using System;
using System.Linq;

namespace nCredit.DomainModel
{
    public class SharedDatedValueDomainModel
    {
        private ICreditContextExtended context;

        public SharedDatedValueDomainModel(ICreditContextExtended context)
        {
            this.context = context;
        }

        public decimal GetReferenceInterestRatePercent(DateTime transactionDate)
        {
            return GetDatedValue(transactionDate, SharedDatedValueCode.ReferenceInterestRate, valueIfMissing: 0m);
        }

        private decimal GetDatedValue(DateTime transactionDate, SharedDatedValueCode code, decimal? valueIfMissing = null)
        {
            var v = context
                .SharedDatedValuesQueryable
                .Where(x => x.Name == code.ToString() && x.TransactionDate <= transactionDate)
                .OrderByDescending(x => x.TransactionDate)
                .ThenByDescending(x => x.Timestamp)
                .Select(x => (decimal?)x.Value)
                .FirstOrDefault();

            if (!v.HasValue)
            {
                if (valueIfMissing.HasValue)
                    return valueIfMissing.Value;
                else
                    throw new Exception($"Shared Value {code} has no value for date {transactionDate}");
            }

            return v.Value;
        }
    }
}