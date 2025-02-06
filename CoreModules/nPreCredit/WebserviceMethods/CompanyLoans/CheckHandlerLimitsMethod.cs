using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.Services.SharedStandard;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class CheckHandlerLimitsMethod : TypedWebserviceMethod<CheckHandlerLimitsMethod.Request, CheckHandlerLimitsMethod.Response>
    {
        public override string Path => "CompanyLoan/CheckHandlerLimits";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            return CheckApprovedLoan(request.HandlerUserId, request.LoanAmount);
        }

        public class Request
        {
            [Required]
            public int HandlerUserId { get; set; }
            public decimal LoanAmount { get; set; }
        }

        public class Response
        {

            public bool Approved { get; set; }
        }

        public static Response CheckApprovedLoan(int handlerUserId, decimal loanAmount)
        {
            bool isOverHandlerLimit;
            bool? isAllowedToOverrideHandlerLimit;
            var handlerLimitEngine = DependancyInjection.Services.Resolve<HandlerLimitEngine>();

            handlerLimitEngine.CheckHandlerLimits(0, loanAmount, handlerUserId, out isOverHandlerLimit, out isAllowedToOverrideHandlerLimit);

            var approved = false;
            if (isAllowedToOverrideHandlerLimit == true)
                approved = true;
            if (isOverHandlerLimit == false)
                approved = true;

            return new Response
            {
                Approved = approved
            };

        }
    }
}