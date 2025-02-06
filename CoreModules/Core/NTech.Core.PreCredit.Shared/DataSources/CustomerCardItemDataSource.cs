using nPreCredit.Code.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Datasources
{
    public class CustomerCardItemDataSource : IApplicationDataSource
    {
        public const string DataSourceNameShared = "CustomerCardItem";
        private readonly ICustomerClient customerClient;
        private readonly IPreCreditContextFactoryService preCreditContextFactoryService;

        public CustomerCardItemDataSource(ICustomerClient customerClient, IPreCreditContextFactoryService preCreditContextFactoryService)
        {
            this.customerClient = customerClient;
            this.preCreditContextFactoryService = preCreditContextFactoryService;
        }

        public string DataSourceName => DataSourceNameShared;

        public bool IsSetDataSupported => false;

        public Dictionary<string, string> GetItems(string applicationNr, ISet<string> names, ApplicationDataSourceMissingItemStrategy missingItemStrategy, Action<string> observeMissingItems = null, Func<string, string> getDefaultValue = null, Action<string> observeChangedItems = null)
        {
            var items = new List<QueryItem>();
            foreach (var name in names)
            {
                if (!TryParseName(name, out var queryItem))
                    throw new NTechCoreWebserviceException($"Invalid CustomerCardItem: {name}")
                    {
                        IsUserFacing = true,
                        ErrorCode = "invalidCustomerCardItemName"
                    };
                items.Add(queryItem);
            }

            var applicantNrItems = items.Where(x => x.ApplicantNr.HasValue).ToList();
            if (applicantNrItems.Count > 0)
            {
                using (var context = preCreditContextFactoryService.CreateExtended())
                {
                    var customerIdByApplicantTag = context
                        .CreditApplicationItemsQueryable
                        .Where(x =>
                            x.ApplicationNr == applicationNr
                            && x.GroupName.StartsWith("applicant")
                            && x.Name == "customerId")
                        .ToList()
                        .ToDictionary(x => x.GroupName, x => int.Parse(x.Value));
                    foreach (var i in applicantNrItems)
                    {
                        var tag = $"applicant{i.ApplicantNr.Value}";
                        i.CustomerId = customerIdByApplicantTag.ContainsKey(tag) ? customerIdByApplicantTag[tag] : new int?();
                        if (!i.CustomerId.HasValue)
                            i.IsMissing = true; //No such applicant exists
                    }
                }
            }

            var customerIds = new HashSet<int>();
            var propertyNames = new HashSet<string>();

            foreach (var i in items.Where(x => !x.IsMissing))
            {
                customerIds.Add(i.CustomerId.Value);
                propertyNames.Add(i.CustomerPropertyName);
            }

            var result = customerClient.BulkFetchPropertiesByCustomerIdsD(customerIds, propertyNames.ToArray());

            foreach (var i in items.Where(x => !x.IsMissing))
            {
                if (!result.ContainsKey(i.CustomerId.Value))
                    i.IsMissing = false;
                else
                {
                    var c = result[i.CustomerId.Value];
                    if (c.ContainsKey(i.CustomerPropertyName))
                        i.Value = c[i.CustomerPropertyName];
                    else
                        i.IsMissing = false;
                }
            }
            foreach (var i in items.Where(x => x.IsMissing))
            {
                observeMissingItems?.Invoke(i.ExternalName);
                if (missingItemStrategy == ApplicationDataSourceMissingItemStrategy.ThrowException)
                    throw new NTechCoreWebserviceException($"No such property '{i.ExternalName}'.");
                else if (missingItemStrategy == ApplicationDataSourceMissingItemStrategy.UseDefaultValue)
                {
                    i.Value = getDefaultValue(i.ExternalName);
                    i.IsMissing = false;
                }
            }
            return items.Where(x => !x.IsMissing).ToDictionary(x => x.ExternalName, x => x.Value);
        }

        private class QueryItem
        {
            public string ExternalName { get; set; }
            public string CustomerPropertyName { get; set; }
            public int? ApplicantNr { get; set; }
            public int? CustomerId { get; set; }
            public bool IsMissing { get; set; }
            public string Value { get; set; }
        }

        private static bool TryParseName(string name, out QueryItem queryItem)
        {
            queryItem = new QueryItem { ExternalName = name };
            try
            {
                var i = name.IndexOf('.');
                queryItem.CustomerPropertyName = name.Substring(i + 1);
                var id = int.Parse(name.Substring(0, i).Substring(1));
                if (name.StartsWith("a"))
                {
                    //a<applicantNr>.<propertyName>
                    queryItem.ApplicantNr = id;
                    return true;
                }
                else if (name.StartsWith("c"))
                {
                    //c<customerId>.<propertyName>
                    queryItem.CustomerId = id;
                    return true;
                }

                queryItem = null;
                return false;
            }
            catch
            {
                queryItem = null;
                return false;
            }
        }

        public int? SetData(string applicationNr, string compoundItemName, bool isDelete, bool isMissingCurrentValue, string currentValue, string newValue, INTechCurrentUserMetadata currentUser)
        {
            throw new NotImplementedException();
        }
    }
}