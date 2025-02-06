using Newtonsoft.Json;
using nPreCredit.Code.Services;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Datasources
{
    public class CurrentCreditDecisionItemsDataSource : IApplicationDataSource
    {
        public const string DataSourceNameShared = "CurrentCreditDecisionItems";
        private readonly IPreCreditContextFactoryService preCreditContextFactoryService;

        public CurrentCreditDecisionItemsDataSource(IPreCreditContextFactoryService preCreditContextFactoryService)
        {
            this.preCreditContextFactoryService = preCreditContextFactoryService;
        }

        public string DataSourceName => DataSourceNameShared;

        public bool IsSetDataSupported => false;

        public Dictionary<string, string> GetItems(string applicationNr, ISet<string> names, ApplicationDataSourceMissingItemStrategy missingItemStrategy, Action<string> observeMissingItems = null, Func<string, string> getDefaultValue = null, Action<string> observeChangedItems = null)
        {
            var result = new Dictionary<string, string>();
            using (var context = preCreditContextFactoryService.CreateExtended())
            {
                var q = context.CreditDecisionItemsQueryable.Where(x =>
                    x.Decision.CreditApplication.ApplicationNr == applicationNr
                    && x.Decision.CreditApplication.CurrentCreditDecisionId == x.CreditDecisionId);

                if (!names.Contains("*"))
                {
                    q = q.Where(x => names.Contains(x.ItemName));
                }

                var items = q.Select(x => new
                {
                    x.ItemName,
                    x.IsRepeatable,
                    x.Value
                }).ToList();

                foreach (var i in items.Where(x => !x.IsRepeatable))
                {
                    result[i.ItemName] = i.Value;
                }

                foreach (var i in items.Where(x => x.IsRepeatable).GroupBy(x => x.ItemName))
                {
                    result[i.Key] = JsonConvert.SerializeObject(i.Select(x => x.Value).ToList());
                }

                if (missingItemStrategy == ApplicationDataSourceMissingItemStrategy.ThrowException || missingItemStrategy == ApplicationDataSourceMissingItemStrategy.UseDefaultValue)
                {
                    foreach (var n in names.Where(x => x != "*"))
                    {
                        if (!result.ContainsKey(n))
                        {
                            if (missingItemStrategy == ApplicationDataSourceMissingItemStrategy.ThrowException)
                                throw new Exception($"Application {applicationNr} is missing item {n} from data source {DataSourceNameShared}");
                            else
                            {
                                observeMissingItems?.Invoke(n);
                                result[n] = getDefaultValue(n);
                            }
                        }
                    }
                }

                return result;
            }
        }

        public int? SetData(string applicationNr, string compoundItemName,
            bool isDelete, bool isMissingCurrentValue, string currentValue, string newValue,
            INTechCurrentUserMetadata currentUser)
        {
            throw new NotImplementedException();
        }
    }
}