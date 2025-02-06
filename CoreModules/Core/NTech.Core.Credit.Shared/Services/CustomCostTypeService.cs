using nCredit.Code.Services;
using Newtonsoft.Json;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NTech.Core.Credit.Shared.Services
{
    public class CustomCostTypeService
    {
        private const string ModelKeySpace = "NotificationCostModel";
        private const string CurrentKeyKeySpace = "CurrentNotificationCostModelKey";        

        private readonly CreditContextFactory contextFactory;
        private readonly PaymentOrderAndCostTypeCache cache;

        public CustomCostTypeService(CreditContextFactory contextFactory, PaymentOrderAndCostTypeCache cache)
        {
            this.contextFactory = contextFactory;
            this.cache = cache;
        }

        public HashSet<string> GetDefinedCodes()
        {
            return GetCustomCosts().Select(x => x.Code).ToHashSetShared();
        }

        public void SetCosts(List<CustomCost> costs)
        {
            using (var context = contextFactory.CreateContext())
            {
                if(costs.Count != costs.Select(x => x.Code.ToLower()).Distinct().Count())
                    throw new NTechCoreWebserviceException("Duplicate codes found") { ErrorHttpStatusCode = 400, IsUserFacing = true };
                if(costs.Any(x => !Regex.IsMatch(x.Code, @"^[a-zA-Z]+$")))
                    throw new NTechCoreWebserviceException("Codes must contain only the letters a-z upper or lower case") { ErrorHttpStatusCode = 400, IsUserFacing = true };
                if(costs.Any(x => x.Code.Length < 3 || x.Code.Length > 128))
                    throw new NTechCoreWebserviceException("Codes must be 3-128 in length") { ErrorHttpStatusCode = 400, IsUserFacing = true };
                if (costs.Any(x => x.Text == null || (x.Text.Length < 1 || x.Text.Length > 128)))
                    throw new NTechCoreWebserviceException("Text must be 1-128 in length") { ErrorHttpStatusCode = 400, IsUserFacing = true };
                var key = Guid.NewGuid().ToString();

                KeyValueStoreService.SetValueComposable(context, CurrentKeyKeySpace, CurrentKeyKeySpace, key);
                KeyValueStoreService.SetValueComposable(context, key, ModelKeySpace, JsonConvert.SerializeObject(new CustomCostsStorageModel
                {
                    SchemaVersion = 1,
                    TransactionDate = context.CoreClock.Now,
                    Costs = costs
                }));

                context.SaveChanges();

                cache.SignalCostsChanged(costs);
            }            
        }

        public List<CustomCost> GetCustomCosts()
        {
            List<CustomCost> GetCosts()
            {
                using (var context = contextFactory.CreateContext())
                {
                    var key = Guid.NewGuid().ToString();

                    var keyValueStore = new KeyValueStoreService(() => contextFactory.CreateContext());
                    var currentKey = keyValueStore.GetValue(CurrentKeyKeySpace, CurrentKeyKeySpace);
                    if (currentKey == null)
                        return new List<CustomCost>();

                    var currentValue = keyValueStore.GetValue(currentKey, ModelKeySpace);
                    var storedModel = JsonConvert.DeserializeObject<CustomCostsStorageModel>(currentValue);
                    return storedModel.Costs;
                }
            }

            return cache.GetCustomCosts(GetCosts);
        }

        public Func<string, string> GetCustomCostTextSource()
        {
            var costs = new Lazy<List<CustomCost>>(() => GetCustomCosts());
            return x => costs.Value.Single(y => y.Code == x).Text;
        }

        public CustomCost GetCost(string code)
        {
            var allCosts = GetCustomCosts();
            var cost = allCosts.SingleOrDefault(x => x.Code == code);
            if (cost == null)
                throw new NTechCoreWebserviceException("No such custom cost code exists") { ErrorCode = "noSuchCustomCostCodeExists" };
            return cost;
        }

        private class CustomCostsStorageModel
        {
            public int SchemaVersion { get; set; }
            public DateTimeOffset TransactionDate { get; set; }
            public List<CustomCost> Costs { get; set; }
        }
    }

    public class CustomCost
    {
        public string Code { get; set; }
        public string Text { get; set; }
    }
}