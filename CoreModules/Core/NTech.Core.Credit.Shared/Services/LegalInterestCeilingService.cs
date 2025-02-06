using System;

namespace nCredit.Code.Services
{
    public class LegalInterestCeilingService
    {
        private readonly Lazy<decimal?> legalInterestCeilingPercent;
        private decimal? LegalInterestCeilingPercent => legalInterestCeilingPercent.Value;

        public LegalInterestCeilingService(ICreditEnvSettings envSettings)
        {
            legalInterestCeilingPercent = new Lazy<decimal?>(() => envSettings.LegalInterestCeilingPercent);
        }

        private LegalInterestCeilingService(decimal? legalInterestCeilingPercent)
        {
            this.legalInterestCeilingPercent = new Lazy<decimal?>(() => legalInterestCeilingPercent);
        }

        public static LegalInterestCeilingService Create(decimal? legalInterestCeilingPercent) => new LegalInterestCeilingService(legalInterestCeilingPercent);

        public decimal GetConstrainedMarginInterestRate(decimal referenceInterestRate, decimal requestedMarginInterstRate)
        {
            var requestedTotalInterestRate = referenceInterestRate + requestedMarginInterstRate;
            if (LegalInterestCeilingPercent.HasValue && requestedTotalInterestRate > LegalInterestCeilingPercent.Value)
            {
                var totalConstrainedInterestRate = Math.Max(Math.Min(referenceInterestRate + requestedMarginInterstRate, LegalInterestCeilingPercent.Value), 0);
                return totalConstrainedInterestRate - referenceInterestRate;
            }
            else if (requestedTotalInterestRate < 0m)
            {
                return -referenceInterestRate;
            }
            else
            {
                return requestedMarginInterstRate;
            }
        }

        public ConstrainedInterestChange HandleMarginInterestRateChange(
            decimal referenceInterestRate,
            decimal? currentRequestedMarginInterstRate,
            decimal? currentMarginInterstRate,
            decimal newRequestedMarginInterstRate)
        {
            var change = new ConstrainedInterestChange();
            var newMarginInterestRate = GetConstrainedMarginInterestRate(referenceInterestRate, newRequestedMarginInterstRate);
            if (!currentMarginInterstRate.HasValue || currentMarginInterstRate.Value != newMarginInterestRate)
                change.NewMarginInterestRate = newMarginInterestRate;

            if (currentRequestedMarginInterstRate.HasValue)
            {
                if (currentRequestedMarginInterstRate.Value != newRequestedMarginInterstRate)
                    change.NewRequestedMarginInterestRate = newRequestedMarginInterstRate;
            }
            else if (newMarginInterestRate != newRequestedMarginInterstRate)
                change.NewRequestedMarginInterestRate = newRequestedMarginInterstRate;

            return change;
        }

        public ConstrainedInterestChange HandleReferenceInterestRateChange(
            decimal newReferenceInterestRate,
            decimal? currentRequestedMarginInterstRate,
            decimal currentMarginInterstRate)
        {
            return HandleMarginInterestRateChange(newReferenceInterestRate, currentRequestedMarginInterstRate, currentMarginInterstRate, currentRequestedMarginInterstRate ?? currentMarginInterstRate);
        }
    }

    public class ConstrainedInterestChange
    {
        public decimal? NewMarginInterestRate { get; set; }
        public decimal? NewRequestedMarginInterestRate { get; set; }
    }
}