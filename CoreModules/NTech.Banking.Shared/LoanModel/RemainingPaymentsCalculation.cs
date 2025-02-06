using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.LoanModel
{
    public class RemainingPaymentsCalculation
    {
        public RemainingPaymentsCalculation()
        {

        }

        public class RemainingPaymentsModel
        {
            public DateTime LastPaymentDate { get; set; }
            public int NrOfRemainingPayments { get; set; }
        }

        public RemainingPaymentsModel ComputeWithAnnuity(
            DateTime? lastNotificationDueDate,
            DateTime nextPossibleFutureNotificationDueDate,
            decimal notNotifiedCapitalBalance,
            decimal interestRatePercent,
            decimal annuityAmount)
        {
            var remainingPaymentsCount = (int)Math.Round(ComputeAnnuityFormulaN(interestRatePercent, notNotifiedCapitalBalance, annuityAmount));

            return ComputeWithKnownCount(lastNotificationDueDate, nextPossibleFutureNotificationDueDate, remainingPaymentsCount);
        }

        private RemainingPaymentsModel ComputeWithKnownCount(
            DateTime? lastNotificationDueDate,
            DateTime nextPossibleFutureNotificationDueDate,
            int remainingPaymentsCount)
        {
            DateTime lastPaymentDate;
            if (lastNotificationDueDate.HasValue)
                lastPaymentDate = lastNotificationDueDate.Value.AddMonths(remainingPaymentsCount);
            else
                lastPaymentDate = nextPossibleFutureNotificationDueDate.AddMonths(remainingPaymentsCount - 1);

            return new RemainingPaymentsModel
            {
                NrOfRemainingPayments = remainingPaymentsCount,
                LastPaymentDate = lastPaymentDate
            };
        }

        private decimal ComputeAnnuityFormulaN(decimal interestRatePercent, decimal notNotifiedCapitalBalance, decimal annuityAmount)
        {
            double r = (double)(interestRatePercent / 100m / 12m);
            var pv = (double)notNotifiedCapitalBalance;
            var p = (double)annuityAmount;

            return (decimal)(Math.Log(Math.Pow(1d - (pv * r / p), -1d)) / Math.Log(1d + r));
        }
    }
}
