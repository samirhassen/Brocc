using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace NTech.Banking.LoanModel
{
    public class PaymentPlanCalculation
    {
        private readonly LoanTerms terms;
        private readonly Settings settings;
        private readonly Action<List<Tuple<decimal, decimal>>, List<Tuple<decimal, decimal>>, decimal> effectiveInterestRateCalculationObserveLoansAndPaymentsAndExactRate;

        private decimal? annuityAmount;
        private decimal? fixedMonthlyCapitalAmount;
        private bool hasEffectiveInterestRatePercentBeenComputed = false;
        private decimal? effectiveInterestRatePercent;
        private IList<MonthlyPayment> payments;

        private PaymentPlanCalculation(LoanTerms terms, Settings settings, Action<List<Tuple<decimal, decimal>>, List<Tuple<decimal, decimal>>, decimal> effectiveInterestRateCalculationObserveLoansAndPaymentsAndExactRate)
        {
            this.terms = terms;
            this.settings = settings;
            this.effectiveInterestRateCalculationObserveLoansAndPaymentsAndExactRate = effectiveInterestRateCalculationObserveLoansAndPaymentsAndExactRate;
        }

        public static Settings DefaultSettings
        {
            get
            {
                return new Settings();
            }
        }

        public static PaymentPlanCalculation CaclculateSinglePaymentWithRepaymentTimeInDays(decimal loanAmount, int repaymentTimeInDays, decimal yearlyInterestRateInPercent, decimal? initialFeeOnNotification = null, decimal? notificationFee = null)
        {
            var capitalOnNotification = loanAmount;

            //Why +1? Since a loan with 0 days repayment time would still have to pay interest for the day when they both got the loan and paid
            var interestOnNotification = Math.Round(loanAmount * yearlyInterestRateInPercent / 365.25m / 100m * (decimal)(repaymentTimeInDays + 1), 2);

            var terms = new FixedMonthlyCapitalAmountLoanTerms
            {
                FixedMonthlyCapitalAmount = capitalOnNotification,
                GetOverrideAmortizationAmountForMonth = null,
                InitialFeeDrawnFromLoanAmount = 0m,
                LoanAmount = loanAmount,
                YearlyInterestRateAsPercent = yearlyInterestRateInPercent,
                CompensateFor360DayYear = false,
                InitialFeeCapitalized = 0m,
                InitialFeePaidOnFirstNotification = initialFeeOnNotification ?? 0m,
                MonthCountCapEvenIfNotFullyPaid = null,
                MonthlyFee = notificationFee ?? 0m,
                RepaymentTimeInMonths = 1
            };
            PaymentPlanCalculation c = new PaymentPlanCalculation(terms, DefaultSettings, effectiveInterestRateCalculationObserveLoansAndPaymentsAndExactRate: null)
            {
                payments = new List<MonthlyPayment>
                {
                    new MonthlyPayment
                    {
                        Capital = capitalOnNotification,
                        InitialFee = initialFeeOnNotification ?? 0m,
                        MonthlyFee = notificationFee ?? 0m,
                        Interest = interestOnNotification,
                        NonStandardPaymentDays = repaymentTimeInDays
                    }
                },
                effectiveInterestRatePercent = null
            };
            c.effectiveInterestRatePercent = EffectiveInterestRateCalculation.ComputeSingleNotificationEffectiveInterestRate(loanAmount, c.Payments[0].TotalAmount, repaymentTimeInDays);
            c.hasEffectiveInterestRatePercentBeenComputed = true;
            return c;
        }

        public static PaymentPlanCalculationBuilder BeginCreateWithRepaymentTime(decimal loanAmount, int repaymentTimeInMonths, decimal yearlyInterestRateInPercent, bool useAnnuities, Func<int, decimal?> getOverrideAmortizationAmountForMonth, bool compensateFor360DayYear)
        {
            var m = new PaymentPlanCalculationBuilder();
            var settings = DefaultSettings;

            if (useAnnuities)
                m.Terms = new AnnuityLoanTerms
                {
                    LoanAmount = loanAmount,
                    RepaymentTimeInMonths = repaymentTimeInMonths,
                    YearlyInterestRateAsPercent = yearlyInterestRateInPercent,
                    GetOverrideAmortizationAmountForMonth = getOverrideAmortizationAmountForMonth,
                    CompensateFor360DayYear = compensateFor360DayYear
                };
            else
                m.Terms = new FixedMonthlyCapitalAmountLoanTerms
                {
                    LoanAmount = loanAmount,
                    RepaymentTimeInMonths = repaymentTimeInMonths,
                    YearlyInterestRateAsPercent = yearlyInterestRateInPercent,
                    GetOverrideAmortizationAmountForMonth = getOverrideAmortizationAmountForMonth
                };

            m.Settings = settings;

            return m;
        }

        public static Func<int, decimal?> CreateGetOverrideAmortizationAmountForMonth(CreditAmortizationModel amortizationModel, DateTime firstNotificationDate, int dueDayOfMonth)
        {
            return monthNr =>
            {
                if (monthNr < 1)
                    throw new Exception("Monthnr should start at 1");
                var notificationDate = firstNotificationDate.AddMonths(monthNr - 1);
                var dueDate = new DateTime(notificationDate.Year, notificationDate.Month, dueDayOfMonth);

                return amortizationModel.GetAmortizationOverrideAmount(dueDate);
            };
        }

        public static PaymentPlanCalculationBuilder BeginCreateWithAmortizationModel(decimal loanAmount, CreditAmortizationModel amortizationModel, int? monthCountCap, decimal yearlyInterestRateInPercent, DateTime firstNotificationDate, int dueDayOfMonth, bool compensateFor360DayYear)
        {
            var getOverrideAmortizationAmountForMonth = CreateGetOverrideAmortizationAmountForMonth(amortizationModel, firstNotificationDate, dueDayOfMonth);

            return amortizationModel.UsingActualAnnuityOrFixedMonthlyCapital(
                a => BeginCreateWithAnnuity(loanAmount, a, yearlyInterestRateInPercent, getOverrideAmortizationAmountForMonth, compensateFor360DayYear),
                m => BeginCreateWithFixedMonthlyCapitalAmount(loanAmount, m, yearlyInterestRateInPercent, monthCountCap, getOverrideAmortizationAmountForMonth, compensateFor360DayYear));
        }

        public static PaymentPlanCalculationBuilder BeginCreateWithAnnuity(decimal loanAmount, decimal annuityAmount, decimal yearlyInterestRateInPercent, Func<int, decimal?> getOverrideAmortizationAmountForMonth, bool compensateFor360DayYear)
        {
            var m = new PaymentPlanCalculationBuilder();

            m.Terms = new AnnuityLoanTerms
            {
                LoanAmount = loanAmount,
                AnnuityAmount = annuityAmount,
                YearlyInterestRateAsPercent = yearlyInterestRateInPercent,
                GetOverrideAmortizationAmountForMonth = getOverrideAmortizationAmountForMonth,
                CompensateFor360DayYear = compensateFor360DayYear
            };

            m.Settings = DefaultSettings;

            return m;
        }

        public static PaymentPlanCalculationBuilder BeginCreateWithFixedMonthlyCapitalAmount(decimal loanAmount, decimal monthlyCapitalAmount, decimal yearlyInterestRateInPercent, int? monthCountCapEvenIfNotFullyPaid, Func<int, decimal?> getOverrideAmortizationAmountForMonth, bool compensateFor360DayYear)
        {
            var m = new PaymentPlanCalculationBuilder();

            m.Terms = new FixedMonthlyCapitalAmountLoanTerms
            {
                LoanAmount = loanAmount,
                FixedMonthlyCapitalAmount = monthlyCapitalAmount,
                YearlyInterestRateAsPercent = yearlyInterestRateInPercent,
                MonthCountCapEvenIfNotFullyPaid = monthCountCapEvenIfNotFullyPaid,
                GetOverrideAmortizationAmountForMonth = getOverrideAmortizationAmountForMonth,
                CompensateFor360DayYear = compensateFor360DayYear
            };

            m.Settings = DefaultSettings;

            return m;
        }

        public bool UsesAnnuities
        {
            get
            {
                return terms is AnnuityLoanTerms;
            }
        }

        public decimal AnnuityAmount
        {
            get
            {
                if (!(terms is AnnuityLoanTerms))
                    throw new Exception("Annuity is not defined for this amortization model");

                var t = terms as AnnuityLoanTerms;

                if (t.AnnuityAmount.HasValue)
                    return t.AnnuityAmount.Value;
                if (!annuityAmount.HasValue)
                    annuityAmount = ComputeAnnuity(t);
                return annuityAmount.Value;
            }
        }

        public decimal FixedMonthlyCapitalAmount
        {
            get
            {
                if (!(terms is FixedMonthlyCapitalAmountLoanTerms))
                    throw new Exception("FixedMonthlyCapitalAmount is not defined for this amortization model");

                var t = terms as FixedMonthlyCapitalAmountLoanTerms;

                if (t.FixedMonthlyCapitalAmount.HasValue)
                    return t.FixedMonthlyCapitalAmount.Value;
                if (!fixedMonthlyCapitalAmount.HasValue)
                    fixedMonthlyCapitalAmount = Math.Round((InitialCapitalDebtAmount / ((decimal)t.RepaymentTimeInMonths.Value)), this.settings.AnnuityRoundToDigits);

                return fixedMonthlyCapitalAmount.Value;
            }
        }

        public decimal? EffectiveInterestRatePercent
        {
            get
            {
                if (!hasEffectiveInterestRatePercentBeenComputed)
                {
                    hasEffectiveInterestRatePercentBeenComputed = true;
                    effectiveInterestRatePercent = ComputeEffRate();
                }
                return effectiveInterestRatePercent;
            }
        }

        public IList<MonthlyPayment> Payments
        {
            get
            {
                if (payments == null)
                {
                    string failedMessage;
                    if (!TryPrefetchPayments(out failedMessage))
                        throw new PaymentPlanCalculationException(failedMessage);
                }
                return payments;
            }
        }

        public bool TryPrefetchPayments(out string failedMessage)
        {
            IList<MonthlyPayment> p;
            if (TryComputeMonthlyPayments(out p, out failedMessage))
            {
                payments = p;
                return true;
            }
            else
            {
                payments = null;
                return false;
            }
        }

        public decimal InitialPaidToCustomerAmount
        {
            get
            {
                return terms.LoanAmount - terms.InitialFeeDrawnFromLoanAmount;
            }
        }

        public decimal InitialCapitalDebtAmount
        {
            get
            {
                return terms.LoanAmount + terms.InitialFeeCapitalized;
            }
        }

        public decimal TotalPaidAmount
        {
            get
            {
                return Payments.Sum(x => x.TotalAmount);
            }
        }

        private decimal? ComputeEffRate()
        {
            return EffectiveInterestRateCalculation
                .WithInitialLoan(InitialPaidToCustomerAmount)
                .WithPayments(Payments.Select((x, i) => Tuple.Create(x.TotalAmount, (i + 1m) / 12m)))
                .Calculate(settings.EffectiveInterestRateRoundToDigits, observeLoansAndPaymentsAndExactRate: effectiveInterestRateCalculationObserveLoansAndPaymentsAndExactRate);
        }

        public abstract class LoanTerms
        {
            public decimal LoanAmount { get; set; }
            public int? RepaymentTimeInMonths { get; set; }
            public decimal YearlyInterestRateAsPercent { get; set; }
            public decimal MonthlyFee { get; set; }
            public decimal InitialFeePaidOnFirstNotification { get; set; }
            public decimal InitialFeeCapitalized { get; set; }
            public decimal InitialFeeDrawnFromLoanAmount { get; set; }
            public Func<int, decimal?> GetOverrideAmortizationAmountForMonth { get; set; }

            /// <summary>
            /// When using an Actual/360 interest model the difference between this and the annuity value will be to large
            /// without compensation so in this case the interest is multiplied by 365/360 when computing the annuity to
            /// handle this.
            /// </summary>
            public bool CompensateFor360DayYear { get; set; }
        }

        public class FixedMonthlyCapitalAmountLoanTerms : LoanTerms
        {
            public decimal? FixedMonthlyCapitalAmount { get; set; }
            public int? MonthCountCapEvenIfNotFullyPaid { get; set; }
        }

        public class AnnuityLoanTerms : LoanTerms
        {
            public decimal? AnnuityAmount { get; set; }
        }

        public class Settings
        {
            public int AnnuityRoundToDigits { get; set; } = 2;
            public int EffectiveInterestRateRoundToDigits { get; set; } = 2;
            public int PaymentRoundToDigits { get; set; } = 2;
            public decimal? LastMonthCarryOverStrategyPercentOfAnnuity { get; set; } = 10;
            public decimal? LastMonthCarryOverStrategyPercentOfFixedMonthlyCapitalAmount { get; set; } = 10;
            public decimal? LastMonthCarryOverStrategyFixedAmount { get; set; }

            public bool ShouldCarryOverRemainingCapitalAmount(decimal remainingCapitalAmount, decimal? annuityAmount, decimal? fixedMonthlyCapitalAmount)
            {
                if (remainingCapitalAmount == 0)
                    return false;

                if (annuityAmount.HasValue && fixedMonthlyCapitalAmount.HasValue)
                    throw new NotImplementedException();

                if (annuityAmount.HasValue)
                {
                    if (LastMonthCarryOverStrategyFixedAmount.HasValue || LastMonthCarryOverStrategyPercentOfAnnuity.HasValue)
                    {
                        if (LastMonthCarryOverStrategyFixedAmount.HasValue && LastMonthCarryOverStrategyPercentOfAnnuity.HasValue)
                            throw new NotImplementedException();

                        var lastMonthCarryOverLimit = LastMonthCarryOverStrategyPercentOfAnnuity.HasValue
                            ? (LastMonthCarryOverStrategyPercentOfAnnuity.Value / 100m * annuityAmount.Value)
                            : LastMonthCarryOverStrategyFixedAmount.Value;

                        if (remainingCapitalAmount < lastMonthCarryOverLimit)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (fixedMonthlyCapitalAmount.HasValue)
                {
                    if (LastMonthCarryOverStrategyFixedAmount.HasValue || LastMonthCarryOverStrategyPercentOfFixedMonthlyCapitalAmount.HasValue)
                    {
                        if (LastMonthCarryOverStrategyFixedAmount.HasValue && LastMonthCarryOverStrategyPercentOfFixedMonthlyCapitalAmount.HasValue)
                            throw new NotImplementedException();

                        var lastMonthCarryOverLimit = LastMonthCarryOverStrategyPercentOfFixedMonthlyCapitalAmount.HasValue
                            ? (LastMonthCarryOverStrategyPercentOfFixedMonthlyCapitalAmount.Value / 100m * fixedMonthlyCapitalAmount.Value)
                            : LastMonthCarryOverStrategyFixedAmount.Value;

                        if (remainingCapitalAmount < lastMonthCarryOverLimit)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                    throw new NotImplementedException();
            }
        }

        public class MonthlyPayment
        {
            public decimal MonthlyFee { get; set; }
            public decimal InitialFee { get; set; }
            public decimal Interest { get; set; }
            public decimal Capital { get; set; }
            public int? NonStandardPaymentDays { get; set; }

            public decimal TotalAmount
            {
                get
                {
                    return MonthlyFee + InitialFee + Interest + Capital;
                }
            }
        }

        public class PaymentPlanCalculationBuilder
        {
            public PaymentPlanCalculation.LoanTerms Terms { get; set; }
            public PaymentPlanCalculation.Settings Settings { get; set; }
            public Action<List<Tuple<decimal, decimal>>, List<Tuple<decimal, decimal>>, decimal> EffectiveInterestRateCalculationObserveLoansAndPaymentsAndExactRate { get; set; }

            public PaymentPlanCalculationBuilder WithMonthlyFee(decimal fee)
            {
                Terms.MonthlyFee = fee;
                return this;
            }

            public PaymentPlanCalculationBuilder WithInitialFeePaidOnFirstNotification(decimal fee)
            {
                Terms.InitialFeePaidOnFirstNotification = fee;
                return this;
            }

            public PaymentPlanCalculationBuilder WithEffectiveInterestCalculationDetailsObserver(Action<List<Tuple<decimal, decimal>>, List<Tuple<decimal, decimal>>, decimal> observeLoansAndPaymentsAndExactRate)
            {
                EffectiveInterestRateCalculationObserveLoansAndPaymentsAndExactRate = observeLoansAndPaymentsAndExactRate;
                return this;
            }

            public PaymentPlanCalculationBuilder WithInitialFeeCapitalized(decimal fee)
            {
                Terms.InitialFeeCapitalized = fee;
                return this;
            }

            public PaymentPlanCalculationBuilder WithInitialFeeDrawnFromLoanAmount(decimal fee)
            {
                Terms.InitialFeeDrawnFromLoanAmount = fee;
                return this;
            }

            public PaymentPlanCalculationBuilder WithCustom(Func<PaymentPlanCalculationBuilder, PaymentPlanCalculationBuilder> f)
            {
                return f(this);
            }

            public PaymentPlanCalculationBuilder ConfigureRounding(int? annuityRoundToDigits = null, int? effectiveInterestRateRoundToDigits = null, int? paymentRoundToDigits = null)
            {
                this.Settings.AnnuityRoundToDigits = annuityRoundToDigits ?? this.Settings.AnnuityRoundToDigits;
                this.Settings.EffectiveInterestRateRoundToDigits = effectiveInterestRateRoundToDigits ?? this.Settings.EffectiveInterestRateRoundToDigits;
                this.Settings.PaymentRoundToDigits = paymentRoundToDigits ?? this.Settings.PaymentRoundToDigits;
                return this;
            }

            public PaymentPlanCalculationBuilder WithLastMonthStrategyPercentOfAnnuity(decimal percent)
            {
                this.Settings.LastMonthCarryOverStrategyPercentOfAnnuity = percent;
                this.Settings.LastMonthCarryOverStrategyFixedAmount = null;
                return this;
            }

            public PaymentPlanCalculationBuilder WithLastMonthStrategyFixedAmount(decimal amount)
            {
                this.Settings.LastMonthCarryOverStrategyPercentOfAnnuity = null;
                this.Settings.LastMonthCarryOverStrategyFixedAmount = amount;
                return this;
            }

            public PaymentPlanCalculation EndCreate()
            {
                return new PaymentPlanCalculation(this.Terms, this.Settings, this.EffectiveInterestRateCalculationObserveLoansAndPaymentsAndExactRate);
            }
        }

        private decimal CompensationFactor360DayYear
        {
            get
            {
                var t = terms as AnnuityLoanTerms;
                if (t == null)
                    return 1m;
                else
                    return t.CompensateFor360DayYear ? 365m / 360m : 1m;
            }
        }

        private decimal ComputeAnnuity(AnnuityLoanTerms terms)
        {
            if (terms.YearlyInterestRateAsPercent == 0m)
            {
                return Math.Round(InitialCapitalDebtAmount / terms.RepaymentTimeInMonths.Value, this.settings.AnnuityRoundToDigits);
            }

            var r = (double)(CompensationFactor360DayYear * terms.YearlyInterestRateAsPercent / 100m / 12m);
            var pv = (double)InitialCapitalDebtAmount;
            var n = (double)terms.RepaymentTimeInMonths;

            var result = (decimal)(r * pv / (1 - Math.Pow(1 + r, -n)));

            return Math.Round(result, this.settings.AnnuityRoundToDigits);
        }

        private decimal GetActualAmortizationAmountForMonth(int monthNr, decimal standardAmortizationAmount, decimal remainingCapitalAmount)
        {
            var overrideAmortizationAmountForMonth = this.terms?.GetOverrideAmortizationAmountForMonth?.Invoke(monthNr);
            var amt = overrideAmortizationAmountForMonth ?? standardAmortizationAmount;
            return Math.Min(amt, remainingCapitalAmount);
        }

        private bool TryComputeMonthlyPayments(out IList<MonthlyPayment> monthlyPayments, out string failedMessage)
        {
            if (terms.LoanAmount - Math.Round(terms.LoanAmount, this.settings.PaymentRoundToDigits) != 0m)
                throw new Exception("The loanAmount cannot have more decimals than the supplied number of roundingdigits");

            var paymentPlan = new List<MonthlyPayment>();

            var first = true;
            var remainingAmount = InitialCapitalDebtAmount;
            int monthNr = 0;
            var monthCountCapEvenIfNotFullyPaid = (terms as FixedMonthlyCapitalAmountLoanTerms)?.MonthCountCapEvenIfNotFullyPaid;

            while (remainingAmount > 0m)
            {
                monthNr++;

                var interest = Math.Round(remainingAmount * CompensationFactor360DayYear * terms.YearlyInterestRateAsPercent / 100m / 12m, this.settings.PaymentRoundToDigits);

                decimal amortization;
                decimal? annuityAmount = null;
                decimal? fixedMonthlyCapitalAmount = null;

                if (terms is FixedMonthlyCapitalAmountLoanTerms)
                {
                    fixedMonthlyCapitalAmount = FixedMonthlyCapitalAmount;
                    amortization = GetActualAmortizationAmountForMonth(monthNr, FixedMonthlyCapitalAmount, remainingAmount);
                }
                else if (terms is AnnuityLoanTerms)
                {
                    annuityAmount = AnnuityAmount;
                    amortization = GetActualAmortizationAmountForMonth(monthNr, AnnuityAmount - interest, remainingAmount);
                }
                else
                    throw new NotImplementedException();

                if (amortization < 0m)
                {
                    //Simplest possible fallback. Should probably be options here. Like minimum X% of total capital or something since this may loop forever
                    amortization = 0m;
                }

                remainingAmount -= amortization;
                var shouldCarryOverAmount = this.settings.ShouldCarryOverRemainingCapitalAmount(remainingAmount, annuityAmount, fixedMonthlyCapitalAmount);
                if (shouldCarryOverAmount || (monthCountCapEvenIfNotFullyPaid.HasValue && monthCountCapEvenIfNotFullyPaid.Value == monthNr))
                {
                    amortization += remainingAmount;
                    remainingAmount = 0m;
                }

                paymentPlan.Add(new MonthlyPayment
                {
                    Capital = amortization,
                    Interest = interest,
                    InitialFee = first ? terms.InitialFeePaidOnFirstNotification : 0m,
                    MonthlyFee = terms.MonthlyFee
                });
                first = false;

                if (monthNr == 500 * 12)
                {
                    failedMessage = "Will never be paid with current terms";
                    monthlyPayments = null;
                    return false;
                }
            }

            failedMessage = null;
            monthlyPayments = paymentPlan;
            return true;
        }
    }
}

public class PaymentPlanCalculationException : Exception
{
    public PaymentPlanCalculationException()
    {
    }

    public PaymentPlanCalculationException(string message) : base(message)
    {
    }

    public PaymentPlanCalculationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected PaymentPlanCalculationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}