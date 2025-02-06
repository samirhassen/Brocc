using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.WebserviceMethods.MortgageLoan
{
    public class FetchCollateralsMethod : TypedWebserviceMethod<FetchCollateralsMethod.Request, FetchCollateralsMethod.Response>
    {
        public override string Path => "MortgageLoans/Fetch-Collaterals";

        public override bool IsEnabled => NEnv.IsStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            using (var context = new CreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                IQueryable<CollateralHeader> collaterals = context
                    .CollateralHeaders
                    .Where(x => x.CollateralType == "MortgageLoanProperty");

                var hasFilter = false;

                if (request.CollateralIds != null && request.CollateralIds.Count > 0)
                {
                    hasFilter = true;
                    collaterals = collaterals.Where(x => request.CollateralIds.Contains(x.Id));
                }

                if (request.CustomerIds != null && request.CustomerIds.Count > 0)
                {
                    hasFilter = true;
                    var listNames = new[] { "mortgageLoanApplicant", "mortgageLoanConsentingParty", "mortgageLoanPropertyOwner" };
                    collaterals = collaterals
                        .Where(x => x.Credits.Any(y => y.CustomerListMembers.Any(z => request.CustomerIds.Contains(z.CustomerId) && listNames.Contains(z.ListName))));
                }

                if (request.CreditNrs != null && request.CreditNrs.Count > 0)
                {
                    hasFilter = true;
                    collaterals = collaterals.Where(x => x.Credits.Any(y => request.CreditNrs.Contains(y.CreditNr)));
                }

                if (!hasFilter)
                    return new Response
                    {
                        Collaterals = new List<Response.CollateralModel>(),
                        Credits = new List<Response.CreditModel>()
                    };

                var allItems = CollateralItem.GetLatestCollateralItems(context.CollateralItems);

                var collateralHeaders = collaterals.Select(x => new
                {
                    CollateralId = x.Id,
                    CreditNrs = x.Credits.Select(y => y.CreditNr)
                })
                .ToList();
                var collateralIds = collateralHeaders.Select(x => x.CollateralId).ToList();
                var itemsByCollateralId = allItems
                    .Where(x => collateralIds.Contains(x.CollateralHeaderId))
                    .Select(x => new
                    {
                        x.CollateralHeaderId,
                        x.ItemName,
                        x.StringValue,
                        x.DateValue,
                        x.NumericValue
                    })
                    .GroupBy(x => x.CollateralHeaderId)
                    .ToDictionary(x => x.Key, x => x.ToDictionary(y => y.ItemName, y => new Response.CollateralItemModel
                    {
                        Name = y.ItemName,
                        StringValue = y.StringValue,
                        DateValue = y.DateValue,
                        NumericValue = y.NumericValue
                    }));

                var collateralsResult = collateralHeaders.Select(x => new Response.CollateralModel
                {
                    CollateralId = x.CollateralId,
                    CreditNrs = x.CreditNrs.ToList(),
                    CollateralItems = itemsByCollateralId[x.CollateralId]
                }).ToList();

                var creditsResult = context
                    .CreditHeaders
                    .Where(x => x.CollateralHeaderId.HasValue && collateralIds.Contains(x.CollateralHeaderId.Value))
                    .Select(x => new
                    {
                        x.CreditNr,
                        x.CollateralHeaderId,
                        CreditCustomers = x.CreditCustomers.Select(y => new { y.ApplicantNr, y.CustomerId }),
                        LisCustomers = x.CustomerListMembers.Select(y => new { y.ListName, y.CustomerId }),
                        CurrentCapitalBalance = x
                            .Transactions
                            .Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                            .Sum(y => y.Amount)
                    })
                    .ToList()
                    .Select(x =>
                    {
                        var customerData = new Dictionary<int, Response.CustomerModel>();

                        foreach (var cs in x.CreditCustomers)
                        {
                            if (!customerData.ContainsKey(cs.CustomerId))
                                customerData[cs.CustomerId] = new Response.CustomerModel
                                {
                                    ApplicantNr = cs.ApplicantNr,
                                    CustomerId = cs.CustomerId,
                                    ListNames = new List<string>()
                                };
                            else
                                customerData[cs.CustomerId].ApplicantNr = cs.ApplicantNr;
                        }
                        foreach (var cl in x.LisCustomers)
                        {
                            if (!customerData.ContainsKey(cl.CustomerId))
                                customerData[cl.CustomerId] = new Response.CustomerModel
                                {
                                    CustomerId = cl.CustomerId,
                                    ListNames = new List<string> { cl.ListName }
                                };
                            else
                                customerData[cl.CustomerId].ListNames.Add(cl.ListName);
                        }

                        return new Response.CreditModel
                        {
                            CreditNr = x.CreditNr,
                            CollateralId = x.CollateralHeaderId.Value,
                            CurrentCapitalBalance = x.CurrentCapitalBalance,
                            Customers = customerData.Values.OrderBy(y => y.ApplicantNr ?? 99).ToList()
                        };
                    })
                    .ToList();

                return new Response
                {
                    Collaterals = collateralsResult,
                    Credits = creditsResult
                };
            }
        }

        public class Request
        {
            public List<int> CollateralIds { get; set; }
            public List<int> CustomerIds { get; set; }
            public List<string> CreditNrs { get; set; }
        }

        public class Response
        {
            public List<CollateralModel> Collaterals { get; set; }
            public List<CreditModel> Credits { get; set; }
            public class CollateralModel
            {
                public int CollateralId { get; set; }
                public List<string> CreditNrs { get; set; }
                public Dictionary<string, CollateralItemModel> CollateralItems { get; set; }
            }
            public class CollateralItemModel
            {
                public string Name { get; set; }
                public string StringValue { get; set; }
                public decimal? NumericValue { get; set; }
                public DateTime? DateValue { get; set; }
            }
            public class CreditModel
            {
                public string CreditNr { get; set; }
                public int CollateralId { get; set; }
                public decimal CurrentCapitalBalance { get; set; }
                public List<CustomerModel> Customers { get; set; }
            }
            public class CustomerModel
            {
                public int CustomerId { get; set; }
                public int? ApplicantNr { get; set; }
                public List<string> ListNames { get; set; }
            }
        }
    }
}