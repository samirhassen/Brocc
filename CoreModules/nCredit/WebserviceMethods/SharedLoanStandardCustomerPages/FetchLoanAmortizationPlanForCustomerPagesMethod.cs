using nCredit.Controllers;
using Newtonsoft.Json;
using NTech.Core.Credit.Shared.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCredit.WebserviceMethods.SharedLoanStandardCustomerPages
{
    public class FetchLoanAmortizationPlanForCustomerPagesMethod : TypedWebserviceMethod<FetchLoanAmortizationPlanForCustomerPagesMethod.Request, FetchLoanAmortizationPlanForCustomerPagesMethod.Response>
    {
        public override string Path => "LoanStandard/CustomerPages/Fetch-Loan-AmortizationPlan";

        public override bool IsEnabled => (NEnv.IsStandardUnsecuredLoansEnabled || NEnv.IsStandardMortgageLoansEnabled) && NEnv.ClientCfg.Country.BaseCountry == "SE";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var customerId = request.CustomerId;

            var resolver = requestContext.Service();

            var (plan, model) = AmortizationPlanService.GetAmortizationPlanAndModelShared(
                request.CreditNr, customerId, resolver.ContextFactory, NEnv.EnvSettings, 
                NEnv.NotificationProcessSettings, NEnv.ClientCfgCore);

            GetSeAmortizationBasisResponse amortizationBasis;
            using (var context = resolver.ContextFactory.CreateContext())
            {
                amortizationBasis = resolver.MortgageLoanCollateral.GetSeMortageLoanAmortizationBasis(context, new GetSeAmortizationBasisRequest
                {
                    CreditNr = request.CreditNr,
                    UseUpdatedBalance = request?.UseUpdatedBalance ?? true
                });
            }

            var response = new Response
            {
                NrOfRemainingPayments = plan.NrOfRemainingPayments,
                AmortizationPlanItems = plan.Items,
                TotalInterestAmount = plan.Items.Sum(x => x.InterestTransaction ?? 0m),
                UsesAnnuities = model.AmortizationModel.UsesAnnuities,
                AnnuityAmount = model.AmortizationModel.UsesAnnuities
                    ? (decimal?)model.AmortizationModel.GetActualAnnuityOrException()
                    : null,
                FixedMonthlyPaymentAmount = model.AmortizationModel.UsesAnnuities
                    ? new decimal?()
                    : model.AmortizationModel.GetActualFixedMonthlyPaymentOrException(),
                AmortizationBasis = amortizationBasis,
                SinglePaymentLoanRepaymentDays = plan.SinglePaymentLoanRepaymentDays
            };

            return response;
        }

        public class Request
        {
            [Required]
            public int? CustomerId { get; set; }

            [Required]
            public string CreditNr { get; set; }

            public bool? UseUpdatedBalance { get; set; }
        }

        public class Response
        {
            public int NrOfRemainingPayments { get; set; }
            public List<AmortizationPlan.Item> AmortizationPlanItems { get; set; }
            public decimal TotalInterestAmount { get; set; }

            public decimal? AnnuityAmount { get; set; }

            public GetSeAmortizationBasisResponse AmortizationBasis { get; set; }
            public decimal? FixedMonthlyPaymentAmount { get; set; }
            public bool UsesAnnuities { get; set; }
            public int? SinglePaymentLoanRepaymentDays { get; set; }
        }
    }
}