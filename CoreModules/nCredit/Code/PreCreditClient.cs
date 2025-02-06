using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using static nCredit.Controllers.ReportsController;

namespace nCredit.Code
{
    public class PreCreditClient : AbstractServiceClient
    {
        protected override string ServiceName => "nPreCredit";

        public IDictionary<string, string> GetApplicationNrsByCreditNrs(ISet<string> creditNrs)
        {
            var rr = Begin().PostJson("api/GetApplicationNrByCreditNr", new
            {
                creditNrs = creditNrs.ToList()
            }).ParseJsonAs<GetApplicationNrByCreditNrResult>();
            return (rr.Hits ?? new List<GetApplicationNrByCreditNrResult.Item>()).ToDictionary(x => x.CreditNr, x => x.ApplicationNr);
        }



        public List<CreditApplicationCustomerIdsResult> GetCreditApplicationCustomerIdsByCreditNrs(ISet<string> creditNrs)
        {
            var rr = Begin().PostJson("api/GetCreditApplicationCustomerIdsByCreditNrs", new
            {
                creditNrs = creditNrs.ToList()
            }).ParseJsonAs<List<CreditApplicationCustomerIdsResult>>();
            return rr;
        }

        public class CreditApplicationCustomerIdsResult
        {
            public int CustomerId { get; set; }
            public int ApplicantNr { get; set; }
        }


        public class KycQuestionAnswerSet
        {
            public int CustomerId { get; set; }
            public string CivicRegNr { get; set; }
            public string CreditNr { get; set; }
            public DateTime AnswerDate { get; set; }

            public class Item
            {
                public string Q { get; set; }
                public string A { get; set; }
            }

            public List<Item> QuestionAnswerCodes { get; set; }
        }

        private class AmlMonitorResult
        {
            public List<KycQuestionAnswerSet> Items { get; set; }
            public string NewLatestSeenTimestamp { get; set; }
        }

        public Tuple<byte[], List<KycQuestionAnswerSet>> FetchAmlMonitoringKycQuestions(byte[] latestSeenTimestamp, List<string> questionNames, IList<int> customerIds)
        {
            var rr = Begin()
                .PostJson("api/FetchAmlMonitoringKycQuestions", new
                {
                    latestSeenTimestamp = latestSeenTimestamp == null ? null : Convert.ToBase64String(latestSeenTimestamp),
                    questionNames = questionNames,
                    customerIds = customerIds
                })
                .ParseJsonAs<AmlMonitorResult>();
            return Tuple.Create(
                rr.NewLatestSeenTimestamp == null ? (byte[])null : Convert.FromBase64String(rr.NewLatestSeenTimestamp),
                rr.Items ?? new List<KycQuestionAnswerSet>());
        }

        public Dictionary<int, List<string>> FetchCustomerIdsByApplicationNrs(ISet<string> applicationNrs)
        {
            return Begin()
                .PostJson("api/Reporting/Fetch-CustomerIds-By-ApplicationNrs", new { ApplicationNrs = applicationNrs?.ToList() })
                .ParseJsonAsAnonymousType(new { Customers = new[] { new { CustomerId = (int?)null, ApplicationNrs = (List<string>)null } } })
                ?.Customers
                ?.ToDictionary(x => x.CustomerId.Value, x => x.ApplicationNrs);
        }

        private class GetApplicationNrByCreditNrResult
        {
            public int CustomerId { get; set; }

            public class Item
            {
                public string CreditNr { get; set; }
                public string ApplicationNr { get; set; }
            }

            public List<Item> Hits { get; set; }
        }

        public List<long> AddAffiliateReportingLoanPaidOutEvents(List<LoanPaidOutEventModel> loanPaidOutEvents, NTechSelfRefreshingBearerToken token = null)
        {
            return Begin(bearerToken: token?.GetToken())
                .PostJson("api/AffiliateReporting/Events/AddLoanPaidOut", new
                {
                    Events = loanPaidOutEvents
                })
            .ParseJsonAsAnonymousType(new { Ids = (List<long>)null })
            ?.Ids;
        }

        public class LoanPaidOutEventModel
        {
            public string ApplicationNr { get; set; }
            public string ProviderName { get; set; }
            public string CreditNr { get; set; }
            public string ProviderApplicationId { get; set; }
            public decimal PaymentAmount { get; set; }
            public DateTime PaymentDate { get; set; }
        }

        public (List<string> ProviderNames, List<DateTime> ApplicationMonths) GetCommonReportParameters()
        {
            var result = Begin().PostJson("api/Reports/Fetch-Common-Parameters", new { })
                .ParseJsonAsAnonymousType(new
                {
                    ApplicationMonths = (List<DateTime>)null,
                    ProviderNames = (List<string>)null
                });
            return (result?.ProviderNames, result?.ApplicationMonths);
        }

        public List<AbTestExperimentModel> GetAbTestExperiments()
        {
            return Begin().PostJson("api/GetAllAbTestExperiments", new { })
                    .ParseJsonAs<List<AbTestExperimentModel>>();
        }
    }
}