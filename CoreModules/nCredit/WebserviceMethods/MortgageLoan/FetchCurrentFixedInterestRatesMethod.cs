using nCredit.DbModel.BusinessEvents;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.WebserviceMethods
{
    public class FetchCurrentFixedInterestRatesMethod : TypedWebserviceMethod<FetchCurrentFixedInterestRatesMethod.Request, FetchCurrentFixedInterestRatesMethod.Response>
    {
        public override string Path => "MortgageLoans/FixedInterest/Fetch-All-Current";

        public override bool IsEnabled => NEnv.IsStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            var mgr = new MortgageLoanFixedInterestChangeEventManager(requestContext.CurrentUserMetadata(), requestContext.Service().ContextFactory, NEnv.EnvSettings,
                CoreClock.SharedInstance, NEnv.ClientCfgCore);
            var result = mgr.GetCurrent();

            var currentRates = result.CurrentRates.Select(x => new Response.RateModel
            {
                MonthCount = x.MonthCount,
                RatePercent = x.RatePercent
            }).ToList();

            var response = new Response
            {
                CurrentRates = currentRates
            };

            if (result.PendingChange != null)
            {
                response.PendingChange = new Response.PendingChangeModel
                {
                    IsCommitAllowed = result.IsPendingChangeCommitAllowed,
                    InitiatedByUserId = result.PendingChange.InitiatedByUserId,
                    InitiatedByUserDisplayName = requestContext.Service().UserDisplayName.GetUserDisplayNameByUserId(result.PendingChange.InitiatedByUserId.ToString()),
                    InitiatedDate = result.PendingChange.InitiatedDate,
                    NewRateByMonthCount = result.PendingChange.NewRateByMonthCount
                };
            }

            return response;
        }

        public class Request
        {

        }

        public class Response
        {
            public List<RateModel> CurrentRates { get; set; }
            public PendingChangeModel PendingChange { get; set; }
            public class RateModel
            {
                public int MonthCount { get; set; }
                public decimal RatePercent { get; set; }
            }
            public class PendingChangeModel
            {
                public bool IsCommitAllowed { get; set; }
                public Dictionary<int, decimal> NewRateByMonthCount { get; set; }
                public int InitiatedByUserId { get; set; }
                public string InitiatedByUserDisplayName { get; set; }
                public DateTime InitiatedDate { get; set; }
            }
        }
    }
}