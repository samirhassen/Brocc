using NTech.Banking.LoanModel;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods
{
    public class CalculatePaymentPlanMethod : TypedWebserviceMethod<CalculatePaymentPlanMethod.Request, CalculatePaymentPlanMethod.Response>
    {
        public override string Path => "PaymentPlan/Calculate";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);
            if (request.RepaymentTimeInMonths.HasValue == request.AnnuityOrFlatAmortizationAmount.HasValue)
            {
                return Error("Exactly one of RepaymentTimeInMonths or AnnuityOrFlatAmortizationAmount must be included");
            }
            if (request.RepaymentTimeInMonths.HasValue && request.RepaymentTimeInMonths.Value <= 0)
            {
                return Error("Invalid repayment time");
            }

            var useAnnuities = !(request.IsFlatAmortization ?? false);

            var p = request.RepaymentTimeInMonths.HasValue
                ? PaymentPlanCalculation.BeginCreateWithRepaymentTime(request.LoanAmount.Value, request.RepaymentTimeInMonths.Value, request.TotalInterestRatePercent.Value, useAnnuities, null, NEnv.CreditsUse360DayInterestYear)
                : (useAnnuities
                    ? PaymentPlanCalculation.BeginCreateWithAnnuity(request.LoanAmount.Value, request.AnnuityOrFlatAmortizationAmount.Value, request.TotalInterestRatePercent.Value, null, NEnv.CreditsUse360DayInterestYear)
                    : PaymentPlanCalculation.BeginCreateWithFixedMonthlyCapitalAmount(request.LoanAmount.Value, request.AnnuityOrFlatAmortizationAmount.Value, request.TotalInterestRatePercent.Value, request.MonthCountCapEvenIfNotFullyPaid, null, NEnv.CreditsUse360DayInterestYear)
                    );

            var pp = p.WithMonthlyFee(request.MonthlyFeeAmount ?? 0m)
                .WithInitialFeeCapitalized(request.CapitalizedInitialFeeAmount ?? 0m)
                .WithInitialFeeDrawnFromLoanAmount(request.DrawnFromInitialPaymentInitialFeeAmount ?? 0m)
                .WithInitialFeePaidOnFirstNotification(request.PaidOnFirstNotificationInitialFeeAmount ?? 0m)
                .EndCreate();

            return new Response
            {
                InitialCapitalDebtAmount = pp.InitialCapitalDebtAmount,
                AnnuityAmount = useAnnuities ? new decimal?(pp.AnnuityAmount) : new decimal?(),
                FlatAmortizationAmount = useAnnuities ? new decimal?() : new decimal?(pp.FixedMonthlyCapitalAmount),
                EffectiveInterestRatePercent = pp.EffectiveInterestRatePercent,
                TotalPaidAmount = pp.TotalPaidAmount,
                Payments = pp.Payments.Select(x => new Response.PaymentModel
                {
                    CapitalAmount = x.Capital,
                    InterestAmount = x.Interest,
                    MonthlyFeeAmount = x.MonthlyFee,
                    InitialFeeAmount = x.InitialFee,
                    TotalAmount = x.TotalAmount,
                }).ToList()
            };
        }

        public class Request
        {
            [Required]
            public decimal? LoanAmount { get; set; }

            [Required]
            public decimal? TotalInterestRatePercent { get; set; }

            //One of these two is needed and both cannot be included
            public int? RepaymentTimeInMonths { get; set; }

            public decimal? AnnuityOrFlatAmortizationAmount { get; set; }

            [Required]
            public bool? IsFlatAmortization { get; set; }

            public decimal? MonthlyFeeAmount { get; set; }
            public decimal? CapitalizedInitialFeeAmount { get; set; }
            public decimal? DrawnFromInitialPaymentInitialFeeAmount { get; set; }
            public decimal? PaidOnFirstNotificationInitialFeeAmount { get; set; }

            public int? MonthCountCapEvenIfNotFullyPaid { get; set; }

            public bool? IncludePayments { get; set; }
        }

        public class Response
        {
            public decimal TotalPaidAmount { get; set; }
            public decimal? EffectiveInterestRatePercent { get; set; }
            public decimal? AnnuityAmount { get; set; }
            public decimal? FlatAmortizationAmount { get; set; }
            public decimal InitialCapitalDebtAmount { get; set; }
            public List<PaymentModel> Payments { get; set; }

            public class PaymentModel
            {
                public decimal CapitalAmount { get; set; }
                public decimal InterestAmount { get; set; }
                public decimal MonthlyFeeAmount { get; set; }
                public decimal InitialFeeAmount { get; set; }
                public decimal TotalAmount { get; set; }
            }
        }
    }
}