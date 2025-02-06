using nCredit.DbModel.BusinessEvents;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;

namespace nCredit.WebserviceMethods
{
    public class HandleFixedInterestRatesChangeMethod : TypedWebserviceMethod<HandleFixedInterestRatesChangeMethod.Request, HandleFixedInterestRatesChangeMethod.Response>
    {
        public override string Path => "MortgageLoans/FixedInterest/Handle-Change";

        public override bool IsEnabled => NEnv.IsStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            var mgr = new MortgageLoanFixedInterestChangeEventManager(requestContext.CurrentUserMetadata(), requestContext.Service().ContextFactory, NEnv.EnvSettings,
                CoreClock.SharedInstance, NEnv.ClientCfgCore);

            mgr.HandleChange(
                request.IsCancel.GetValueOrDefault(),
                request.IsCommit.GetValueOrDefault(),
                request.NewRateByMonthCount,
                request.OverrideDualityCommitRequirement.GetValueOrDefault());

            return new Response();
        }


        public class Request
        {
            public Dictionary<int, decimal> NewRateByMonthCount { get; set; }

            public bool? IsCancel { get; set; }
            public bool? IsCommit { get; set; }

            //NOTE: This only works in test
            public bool? OverrideDualityCommitRequirement { get; set; }
        }

        public class Response
        {

        }
    }
}