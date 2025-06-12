using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;

namespace nSavings.Code.Services
{
    public class SavingsAccountSearchService
    {
        private readonly ICustomerClient customerClient;

        public SavingsAccountSearchService(ICustomerClient customerClient)
        {
            this.customerClient = customerClient;
        }

        public List<SavingsAccountSearchHit> Search(SavingsAccountSearchRequest request)
        {
            var searchQuery = (request?.OmnisearchValue ?? "").Trim();
            if (searchQuery.Length == 0)
                return new List<SavingsAccountSearchHit>();

            var allHits = new List<SavingsAccountSearchHit>();

            using (var context = new DbModel.SavingsContext())
            {
                if (NEnv.BaseOcrNumberParser.TryParse(searchQuery, out var ocrNr, out _))
                {
                    var ocrParsed = ocrNr.NormalForm;
                    var hits = CreateResultFromAccountsQueryable(context
                        .SavingsAccountHeaders
                        .Select(x => new
                        {
                            Header = x,
                            OcrNr = x
                                .DatedStrings
                                .Where(y => y.Name == DatedSavingsAccountStringCode.OcrDepositReference.ToString())
                                .OrderByDescending(y => y.BusinessEventId)
                                .Select(y => y.Value)
                                .FirstOrDefault()
                        })
                        .Where(x => x.OcrNr == ocrParsed)
                        .Select(x => x.Header));
                    allHits.AddRange(hits);
                }

                if (!searchQuery.Contains(' '))
                {
                    allHits.AddRange(CreateResultFromAccountsQueryable(context.SavingsAccountHeaders.Where(x => x.SavingsAccountNr == searchQuery)));
                }

                if (!request.SkipCustomerSearch)
                {
                    var customerIds = customerClient.FindCustomerIdsOmni(searchQuery);
                    var hits = CreateResultFromAccountsQueryable(context.SavingsAccountHeaders.Where(x => customerIds.Contains(x.MainCustomerId)));
                    allHits.AddRange(hits);
                }
            }

            //Remove duplicates
            allHits = allHits
                .GroupBy(x => x.SavingsAccountNr)
                .Select(x => x.First())
                .OrderByDescending(x => x.StartDate)
                .ToList();

            return allHits;
        }

        private List<SavingsAccountSearchHit> CreateResultFromAccountsQueryable(IQueryable<SavingsAccountHeader> query)
        {
            return query
                .OrderByDescending(x => x.CreatedByBusinessEventId)
                .Select(x => new SavingsAccountSearchHit
                {
                    SavingsAccountNr = x.SavingsAccountNr,
                    Status = x.Status,
                    StartDate = x.CreatedByEvent.TransactionDate,
                    MainCustomerId = x.MainCustomerId
                })
                .ToList();
        }
    }

    public class SavingsAccountSearchHit
    {
        public string SavingsAccountNr { get; set; }
        public int MainCustomerId { get; set; }
        public string Status { get; set; }
        public DateTime StartDate { get; set; }
    }

    public class SavingsAccountSearchRequest
    {
        public string OmnisearchValue { get; set; }
        public bool SkipCustomerSearch { get; set; }
    }
}