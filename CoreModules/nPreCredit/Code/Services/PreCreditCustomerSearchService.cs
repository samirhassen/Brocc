using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    /*
     NOTE: This does not support finding by customer name, email and such since the customer module handles that directly.
           Those applications will still be returned from GetEntities
     */
    public class PreCreditCustomerSearchService : ICustomerSearchSourceService
    {
        public ISet<int> FindCustomers(string searchQuery)
        {
            using (var context = new PreCreditContext())
            {
                //We only support searching by application nr here
                //Searching by customer name and such will work in practice since nCustomer is also consulted by the FindCustomers loop
                //so those ids will still get sent to GetCustomerEntities and get picked up that way
                return GetCustomerIdsQueryable(context)
                    .Where(x => x.ApplicationNr == searchQuery)
                    .ToList()
                    .SelectMany(x => x.ListMemberCustomerIds.Concat(x.ApplicationItemCustomerIds.Select(y => int.Parse(y))))
                    .ToHashSet();
            }
        }
        public List<CustomerSearchEntity> GetCustomerEntities(int customerId)
        {
            void AddCustomerWithRole(Dictionary<int, HashSet<string>> localCustomersWithRoles, int localCustomerId, string localRoleName)
            {
                if (!localCustomersWithRoles.ContainsKey(localCustomerId))
                    localCustomersWithRoles.Add(localCustomerId, new HashSet<string> { localRoleName });
                else
                    localCustomersWithRoles[localCustomerId].Add(localRoleName);
            }

            var isMlStandard = NEnv.IsStandardMortgageLoansEnabled;

            using (var context = new PreCreditContext())
            {
                var customerIdStr = customerId.ToString();
                var applicationsByApplicationNr = GetCustomerIdsQueryable(context)
                    .Where(x => x.ListMemberCustomerIds.Contains(customerId) || x.ApplicationItemCustomerIds.Contains(customerIdStr))
                    .ToList()
                    .ToDictionary(x => x.ApplicationNr, x => x);

                var applicationNrs = applicationsByApplicationNr.Keys;

                var pre = context
                    .CreditApplicationHeaders
                    .Where(x => applicationNrs.Contains(x.ApplicationNr));

                var result = pre
                    .Select(x => new
                    {
                        x.ApplicationNr,
                        x.ApplicationType,
                        x.ApplicationDate,
                        x.IsActive,
                        x.IsFinalDecisionMade,
                        x.IsCancelled,
                        x.IsRejected,
                        x.RejectedDate,
                        x.CancelledDate,
                        x.FinalDecisionDate,
                        x.ChangedDate,
                        x.ArchivedDate,
                        CreditNrLegacy = x
                            .Items.Where(y => y.GroupName == "application" && y.Name == "creditnr")
                            .Select(y => y.Value)
                            .FirstOrDefault(),
                        NonMlCreditNrStandard = x
                            .ComplexApplicationListItems
                            .Where(y => y.ListName == "Application" && y.Nr == 1 && y.ItemName == "creditNr")
                            .Select(y => y.ItemValue)
                            .FirstOrDefault()
                    })
                    .ToList()
                    .Select(x =>
                    {
                        var application = applicationsByApplicationNr[x.ApplicationNr];
                        var customersWithRoles = new Dictionary<int, HashSet<string>>();
                        foreach (var applicationItemCustomerId in application.ApplicationItemCustomerIds)
                        {
                            //Legacy application customer ids. Always applicants
                            AddCustomerWithRole(customersWithRoles, int.Parse(applicationItemCustomerId), "Applicant");
                        }
                        foreach (var listMemberCustomer in application.ListMembers)
                        {
                            AddCustomerWithRole(customersWithRoles, listMemberCustomer.CustomerId, listMemberCustomer.ListName);
                        }

                        string statusCode;
                        string statusText;
                        if (x.ArchivedDate.HasValue)
                        {   //NOTE: We filter these out of search currently but in case someone changes this we will leave these in
                            statusCode = "Archived";
                            statusText = $"Archived {x.ArchivedDate?.ToString("yyyy-MM-dd")}";
                        }
                        else if (x.IsActive)
                        {
                            statusCode = "Active";
                            statusText = "Active";
                        }
                        else if (x.IsFinalDecisionMade)
                        {
                            IEnumerable<string> creditNrs;
                            if (x.CreditNrLegacy != null)
                                creditNrs = Enumerables.Singleton(x.CreditNrLegacy);
                            else if (x.NonMlCreditNrStandard != null)
                                creditNrs = Enumerables.Singleton(x.NonMlCreditNrStandard);
                            else if (isMlStandard)
                            {
                                //TODO: implement this part when we reach that step on ml standard.
                                //      This is the whole reason for not just one credit nr ... ml will allow several to be created at once
                                creditNrs = Enumerables.Singleton("TODO loanNrs");
                            }
                            else
                                creditNrs = Enumerable.Empty<string>();

                            statusCode = "LoanCreated";
                            statusText = $"Loan(s) created {string.Join(", ", creditNrs)}";
                        }
                        else if (x.IsCancelled)
                        {
                            statusCode = "Cancelled";
                            statusText = $"Cancelled {x.CancelledDate?.ToString("yyyy-MM-dd")}";
                        }
                        else if (x.IsRejected)
                        {
                            statusCode = "Rejected";
                            statusText = $"Rejected {x.RejectedDate?.ToString("yyyy-MM-dd")}";
                        }
                        else
                        {
                            statusCode = "Inactive";
                            statusText = "Inactive";
                        }
                        return new CustomerSearchEntity
                        {
                            IsActive = x.IsActive,
                            CreationDate = x.ApplicationDate.DateTime,
                            CurrentBalance = null,
                            EndDate = x.IsActive ? (DateTime?)null : MaxOfDatesWithFallback(x.ChangedDate, x.RejectedDate, x.CancelledDate, x.FinalDecisionDate),
                            EntityId = x.ApplicationNr,
                            EntityType = x.ApplicationType ?? CreditApplicationTypeCode.unsecuredLoan.ToString(),
                            Source = "nPreCredit",
                            StatusCode = statusCode,
                            StatusDisplayText = statusText,
                            Customers = customersWithRoles
                                .Keys
                                .Select(y => new CustomerSearchEntityCustomer { CustomerId = y, Roles = customersWithRoles[y].ToList() })
                                .ToList()
                        };
                    })
                    .ToList();


                return result;
            }
        }

        private DateTime MaxOfDatesWithFallback(DateTimeOffset fallbackDate, params DateTimeOffset?[] dates)
        {
            var currentMaxDate = DateTimeOffset.MinValue;
            foreach (var date in dates)
            {
                if (date.HasValue && date.Value > currentMaxDate)
                    currentMaxDate = date.Value;
            }
            var result = (currentMaxDate > DateTimeOffset.MinValue) ? currentMaxDate : fallbackDate;
            return result.DateTime;
        }

        private static List<string> CustomerIdApplicationItemNames = new List<string>
        {
            "companyCustomerId", //Legacy company loan
            "applicantCustomerId", //Legacy company loan
            "customerId" //Legacy unsecured loan
        };

        private IQueryable<ApplicationWithAllCustomerIds> GetCustomerIdsQueryable(PreCreditContext context)
        {
            if (NEnv.IsMortgageLoansEnabled && !NEnv.IsStandardMortgageLoansEnabled)
                throw new Exception("Legacy mortgage loans not supported");

            return context
                .CreditApplicationHeaders
                .Where(x => !x.ArchivedDate.HasValue)
                .Select(x => new ApplicationWithAllCustomerIds
                {
                    ApplicationNr = x.ApplicationNr,
                    ApplicationItemCustomerIds = x.Items.Where(y => CustomerIdApplicationItemNames.Contains(y.Name)).Select(y => y.Value),
                    ListMemberCustomerIds = x.CustomerListMemberships.Select(y => y.CustomerId),
                    ListMembers = x.CustomerListMemberships
                });
        }

        private class ApplicationWithAllCustomerIds
        {
            public string ApplicationNr { get; set; }
            public IEnumerable<string> ApplicationItemCustomerIds { get; set; }
            public IEnumerable<int> ListMemberCustomerIds { get; set; }
            public List<CreditApplicationCustomerListMember> ListMembers { get; set; }
        }
    }
}