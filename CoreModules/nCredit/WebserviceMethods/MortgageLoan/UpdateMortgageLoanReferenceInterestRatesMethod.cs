using nCredit.DbModel.BusinessEvents;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
namespace nCredit.WebserviceMethods
{
    public class UpdateMortgageLoanReferenceInterestRatesMethod : TypedWebserviceMethod<UpdateMortgageLoanReferenceInterestRatesMethod.Request, UpdateMortgageLoanReferenceInterestRatesMethod.Response>
    {
        public override string Path => "MortgageLoans/Update-BindingExpired-ReferenceInterestRates";

        public override bool IsEnabled => NEnv.IsMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);
            var u = requestContext.CurrentUserMetadata();

            var mgr = new ReferenceInterestRateChangeBusinessEventManager(u, requestContext.Service().LegalInterestCeiling, NEnv.EnvSettings,
                CoreClock.SharedInstance, NEnv.ClientCfgCore);

            int? successBusinessEventId = null;
            var errors = new List<string>();

            using (var context = new CreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                if (mgr.TryUpdateReferenceInterestWhereBindingHasExpired(context, out var evt, out var failedMessage))
                {
                    context.SaveChanges();
                    successBusinessEventId = evt.Id;
                }
                else
                    errors.Add(failedMessage);
            }

            return new Response
            {
                SuccessBusinessEventId = successBusinessEventId,
                Errors = errors
            };
        }

        public class Request
        {

        }

        public class Response : NTechWebserviceSchedulerResponseBase
        {
            public int? SuccessBusinessEventId { get; set; }
        }
    }
}