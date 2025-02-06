using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.Code.Services
{
    public class CreditSearchService
    {
        private readonly ICustomerClient customerClient;
        private readonly Func<ICreditContextExtended> createContext;
        private Lazy<OcrNumberParser> ocrNumberParser;

        public CreditSearchService(ICustomerClient customerClient, IClientConfigurationCore clientConfiguration, CreditContextFactory creditContextFactory, ICreditEnvSettings envSettings)
        {
            this.customerClient = customerClient;
            this.createContext = () => creditContextFactory.CreateContext();
            ocrNumberParser = new Lazy<OcrNumberParser>(() => new OcrNumberParser(clientConfiguration.Country.BaseCountry));
        }

        public List<SearchCreditHit> Search(SearchCreditRequest request)
        {
            var searchQuery = (request?.OmnisearchValue ?? "").Trim();
            if (searchQuery.Length == 0)
                return new List<SearchCreditHit>();

            var allHits = new List<SearchCreditHit>();

            using (var context = createContext())
            {
                if (ocrNumberParser.Value.TryParse(searchQuery, out var ocrNr, out _))
                {
                    var ocrParsed = ocrNr.NormalForm;
                    var query = context
                        .CreditHeadersQueryable
                        .Select(x => new
                        {
                            Credit = x,
                            CreditOcrNr = x
                                        .DatedCreditStrings
                                        .Where(y => y.Name == DatedCreditStringCode.OcrPaymentReference.ToString())
                                        .OrderByDescending(y => y.TransactionDate)
                                        .ThenByDescending(y => y.Timestamp)
                                        .Select(y => y.Value)
                                        .FirstOrDefault(),
                            SharedOcrPaymentReference = x
                                        .DatedCreditStrings
                                        .Where(y => y.Name == DatedCreditStringCode.SharedOcrPaymentReference.ToString())
                                        .OrderByDescending(y => y.TransactionDate)
                                        .ThenByDescending(y => y.Timestamp)
                                        .Select(y => y.Value)
                                        .FirstOrDefault() ?? "[NONE]",
                            NotificationOcrs = x.Notifications.Select(y => y.OcrPaymentReference)
                        })
                        .Where(x => x.CreditOcrNr == ocrParsed || x.NotificationOcrs.Contains(ocrParsed) || x.SharedOcrPaymentReference == ocrParsed)
                        .Select(x => x.Credit);

                    allHits.AddRange(SearchByCreditsQuery(query));
                }

                if (!searchQuery.Contains(' '))
                {
                    IQueryable<CreditHeader> queryBase = context
                        .CreditHeadersQueryable
                        .Where(x => 
                            x.CreditNr == searchQuery 
                            || x.DatedCreditStrings.Any(y => y.Name == DatedCreditStringCode.MortgageLoanAgreementNr.ToString() && y.Value == searchQuery)
                        );
                    allHits.AddRange(SearchByCreditsQuery(queryBase));
                }

                if (!request.SkipCustomerSearch)
                {
                    var customerIds = customerClient.FindCustomerIdsOmni(searchQuery);

                    allHits.AddRange(SearchByCustomerIds(context, customerIds));
                }
            }

            //Remove duplicates
            allHits = allHits
                .GroupBy(x => x.CreditNr)
                .Select(x => x.First())
                .OrderByDescending(x => x.StartDate)
                .ToList();

            return allHits;
        }

        private List<SearchCreditHit> SearchByCustomerIds(ICreditContextExtended context, List<int> customerIds)
        {
            var q = SearchByCreditsQuery(context
                .CreditHeadersQueryable
                .Where(x => x.CreditCustomers.Any(y => customerIds.Contains(y.CustomerId)) || x.CustomerListMembers.Any(y => customerIds.Contains(y.CustomerId))));

            return q.ToList()
                .OrderByDescending(x => x.StartDate)
                .ToList();
        }

        private List<SearchCreditHit> SearchByCreditsQuery(IQueryable<CreditHeader> query)
        {
            var result = query
                .Select(x => new
                {
                    PreResult = new SearchCreditHit
                    {
                        CreditNr = x.CreditNr,
                        Status = x.Status,
                        StartDate = x.StartDate
                    },
                    CreditCustomerIds = x.CreditCustomers.Select(y => y.CustomerId),
                    ListCustomerIds = x.CustomerListMembers.Select(y => y.CustomerId)
                })
                .ToList();
            //NOTE: This is a hack since Concat for CreditCustomers and CustomerListMembers does not work in ef core. Retest this in future versions to see if it sucks less then.
            foreach (var item in result)
            {
                item.PreResult.ConnectedCustomerIds = item.CreditCustomerIds.Concat(item.ListCustomerIds).Distinct().ToList();
            }
            return result.Select(x => x.PreResult).ToList();
        }
    }

    public class SearchCreditRequest
    {
        public string OmnisearchValue { get; set; }
        public bool SkipCustomerSearch { get; set; }
    }

    public class SearchCreditHit
    {
        public string CreditNr { get; set; }
        public string Status { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public List<int> ConnectedCustomerIds { get; set; }
    }
}