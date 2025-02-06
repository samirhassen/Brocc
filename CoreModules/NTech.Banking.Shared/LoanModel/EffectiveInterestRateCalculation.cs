using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Banking.LoanModel
{
    public class EffectiveInterestRateCalculation
    {
        private struct EffItem
        {
            public double Amount { get; set; }
            public decimal ExactAmount { get; set; }
            public double Time { get; set; }
            public decimal ExactTime { get; set; }
        }

        private List<EffItem> Loans { get; set; }
        private List<EffItem> Payments { get; set; }

        private EffectiveInterestRateCalculation()
        {
            Loans = new List<EffItem>();
            Payments = new List<EffItem>();
        }

        public static decimal? ComputeSingleNotificationEffectiveInterestRate(decimal initialPaidToCustomerAmount, decimal singleNotificationAmount, int repaymentTimeInDays)
        {
            try
            {
                //X = (D / K) ^ (1 / s) - 1
                var fraction = Math.Pow((double)singleNotificationAmount / (double)initialPaidToCustomerAmount, 365d / ((double)repaymentTimeInDays)) - 1d;
                return Math.Round((decimal)(100d * fraction), 4);
            }
            catch
            {
                return null;
            }
        }

        public static EffectiveInterestRateCalculation WithInitialLoan(decimal amount)
        {
            var c = new EffectiveInterestRateCalculation();
            return c.WithAdditionalLoan(amount, 0m);
        }

        public EffectiveInterestRateCalculation WithAdditionalLoan(decimal amount, decimal atTime)
        {
            Loans.Add(new EffItem { Amount = (double)amount, Time = (double)atTime, ExactAmount = amount, ExactTime = atTime });
            return this;
        }

        public EffectiveInterestRateCalculation WithPayment(decimal amount, decimal atTime)
        {
            Payments.Add(new EffItem { Amount = (double)amount, Time = (double)atTime, ExactAmount = amount, ExactTime = atTime });
            return this;
        }

        public EffectiveInterestRateCalculation WithPayments(IEnumerable<Tuple<decimal, decimal>> paymentsAtTimes)
        {
            foreach (var p in paymentsAtTimes)
            {
                WithPayment(p.Item1, p.Item2);
            }
            return this;
        }

        public decimal? Calculate(int? effectiveInterstRateRoundToDigits, Action<List<Tuple<decimal, decimal>>, List<Tuple<decimal, decimal>>, decimal> observeLoansAndPaymentsAndExactRate = null)
        {
            //Special case, no fees at all and one payment
            if (Loans.Count == 1 && Payments.Count == 1 && Loans.Single().Amount == Payments.Single().Amount)
            {
                //This really has not rate as the customer is actually making money here so if anything it should be negative but zero 
                //seems way less likely to cause issues in practice.
                return 0m;
            }

            //See: https://sv.wikipedia.org/wiki/Effektiv_r%C3%A4nta
            Func<double, double> f = x =>
            {
                var sum = 0d;
                foreach (var loan in Loans)
                {
                    sum += loan.Amount * Math.Pow(1 + x, -loan.Time);
                }
                foreach (var payment in Payments)
                {
                    sum -= payment.Amount * Math.Pow(1 + x, -payment.Time);
                }

                return sum;
            };

            const double tolerance = 1e-8;

            if (Math.Abs(f(0d)) < tolerance)
            {
                //Finding exactly 0 is difficult for the algo so we check for that explicitly
                //This happens for instance when the interest is constrained down to zero for a mortgage loan and there are no fees
                return 0m;
            }

            double root;
            var hasRoot = TryFindRootUsingBrentsMethod(f, 0d, 100000d, tolerance, out root);

            if (!hasRoot)
            {
                return null;
            }
            else
            {
                var exactRate = (decimal)root * 100m;
                if (observeLoansAndPaymentsAndExactRate != null)
                {
                    observeLoansAndPaymentsAndExactRate.Invoke(
                        Loans.Select(x => Tuple.Create(x.ExactAmount, x.ExactTime)).ToList(),
                        Payments.Select(x => Tuple.Create(x.ExactAmount, x.ExactTime)).ToList(),
                        exactRate);
                }
                if (effectiveInterstRateRoundToDigits.HasValue)
                    return Math.Round(exactRate, effectiveInterstRateRoundToDigits.Value);
                else
                    return exactRate;
            }
        }

        private static bool TryFindRootUsingBrentsMethod
                       (
                           Func<double, double> f,
                           double left,
                           double right,
                           double tolerance,
                           out double root
                       )
        {
            int _; double errorEstimate;
            var rootCandidate = FindRootUsingBrentsMethod(f, left, right, tolerance, out _, out errorEstimate);
            if (!rootCandidate.HasValue)
            {
                root = 0;
                return false;
            }
            else
            {
                root = rootCandidate.Value;
                return true;
            }
        }

        private static double? FindRootUsingBrentsMethod
        (
            Func<double, double> f,
            double left,
            double right,
            double tolerance,
            out int iterationsUsed,
            out double errorEstimate
        )
        {
            const int maxIterations = 75;

            if (tolerance <= 0.0)
            {
                string msg = string.Format("Tolerance must be positive. Recieved {0}.", tolerance);
                throw new ArgumentOutOfRangeException(msg);
            }

            errorEstimate = double.MaxValue;

            // Implementation and notation based on Chapter 4 in
            // "Algorithms for Minimization without Derivatives"
            // by Richard Brent.

            double c, d, e, fa, fb, fc, tol, m, p, q, r, s;

            // set up aliases to match Brent's notation
            double a = left; double b = right; double t = tolerance;
            iterationsUsed = 0;

            fa = f(a);
            fb = f(b);

            if (fa * fb > 0.0)
            {
                return null;
            }

        label_int:
            c = a; fc = fa; d = e = b - a;
        label_ext:
            if (Math.Abs(fc) < Math.Abs(fb))
            {
                a = b; b = c; c = a;
                fa = fb; fb = fc; fc = fa;
            }

            iterationsUsed++;

            tol = 2.0 * t * Math.Abs(b) + t;
            errorEstimate = m = 0.5 * (c - b);
            if (Math.Abs(m) > tol && fb != 0.0) // exact comparison with 0 is OK here
            {
                // See if bisection is forced
                if (Math.Abs(e) < tol || Math.Abs(fa) <= Math.Abs(fb))
                {
                    d = e = m;
                }
                else
                {
                    s = fb / fa;
                    if (a == c)
                    {
                        // linear interpolation
                        p = 2.0 * m * s; q = 1.0 - s;
                    }
                    else
                    {
                        // Inverse quadratic interpolation
                        q = fa / fc; r = fb / fc;
                        p = s * (2.0 * m * q * (q - r) - (b - a) * (r - 1.0));
                        q = (q - 1.0) * (r - 1.0) * (s - 1.0);
                    }
                    if (p > 0.0)
                        q = -q;
                    else
                        p = -p;
                    s = e; e = d;
                    if (2.0 * p < 3.0 * m * q - Math.Abs(tol * q) && p < Math.Abs(0.5 * s * q))
                        d = p / q;
                    else
                        d = e = m;
                }
                a = b; fa = fb;
                if (Math.Abs(d) > tol)
                    b += d;
                else if (m > 0.0)
                    b += tol;
                else
                    b -= tol;
                if (iterationsUsed == maxIterations)
                    return null;

                fb = f(b);
                if ((fb > 0.0 && fc > 0.0) || (fb <= 0.0 && fc <= 0.0))
                    goto label_int;
                else
                    goto label_ext;
            }
            else
                return b;
        }
    }
}