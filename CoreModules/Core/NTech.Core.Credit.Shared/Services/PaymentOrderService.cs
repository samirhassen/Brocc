using nCredit.Code.Services;
using nCredit.DomainModel;
using Newtonsoft.Json;
using NTech.Banking.Conversion;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services
{
    public class PaymentOrderService
    {
        private const string ModelKeySpace = "PaymentOrderModel";
        private const string CurrentKeyKeySpace = "CurrentPaymentOrderModelKey";

        private readonly CreditContextFactory contextFactory;
        private readonly CustomCostTypeService costTypeService;
        private readonly PaymentOrderAndCostTypeCache cache;
        private readonly IClientConfigurationCore clientConfiguration;
        private static readonly Lazy<List<PaymentOrderItem>> DefaultBuiltinPaymentOrder = new Lazy<List<PaymentOrderItem>>(() => new CreditDomainModel.AmountType[]
        {
                    CreditDomainModel.AmountType.NotificationFee,
                    CreditDomainModel.AmountType.Interest,
                    CreditDomainModel.AmountType.Capital,
                    CreditDomainModel.AmountType.ReminderFee
        }.Select(x => new PaymentOrderItem
        {
            Code = x.ToString(),
            IsBuiltin = true
        }).ToList());

        public static List<PaymentOrderItem> GetDefaultBuiltinPaymentOrder() => DefaultBuiltinPaymentOrder.Value;

        public PaymentOrderService(CreditContextFactory contextFactory, CustomCostTypeService costTypeService, PaymentOrderAndCostTypeCache cache, IClientConfigurationCore clientConfiguration)
        {
            this.contextFactory = contextFactory;
            this.costTypeService = costTypeService;
            this.cache = cache;
            this.clientConfiguration = clientConfiguration;
        }

        public void SetOrder(List<PaymentOrderItem> paymentOrderItems)
        {
            var definedBuiltInCodes = Enums.GetAllValues<CreditDomainModel.AmountType>().Select(x => x.ToString()).ToHashSetShared();
            var definedCustomCodes = new Lazy<HashSet<string>>(() => costTypeService.GetDefinedCodes());

            var builtInItemCodes = paymentOrderItems.Where(x => x.IsBuiltin).Select(x => x.Code).ToList();
            if (builtInItemCodes.Count != builtInItemCodes.Distinct().Count())
                throw new NTechCoreWebserviceException("Duplicate builtin codes found") { ErrorHttpStatusCode = 400, IsUserFacing = true };
            if (!builtInItemCodes.ToHashSetShared().SetEquals(definedBuiltInCodes))
                throw new NTechCoreWebserviceException("All builtin codes must be present") { ErrorHttpStatusCode = 400, IsUserFacing = true };

            var customItemCodes = paymentOrderItems.Where(x => !x.IsBuiltin).Select(x => x.Code).ToList();
            if (customItemCodes.Count != customItemCodes.Distinct().Count())
                throw new NTechCoreWebserviceException("Duplicate custom codes found") { ErrorHttpStatusCode = 400, IsUserFacing = true };
            if (!customItemCodes.ToHashSetShared().SetEquals(definedCustomCodes.Value))
                throw new NTechCoreWebserviceException("All custom codes must be present") { ErrorHttpStatusCode = 400, IsUserFacing = true };

            using (var context = contextFactory.CreateContext())
            {
                var key = Guid.NewGuid().ToString();

                KeyValueStoreService.SetValueComposable(context, CurrentKeyKeySpace, CurrentKeyKeySpace, key);
                KeyValueStoreService.SetValueComposable(context, key, ModelKeySpace, JsonConvert.SerializeObject(new PaymentOrderStorageModel
                {
                    SchemaVersion = 1,
                    TransactionDate = context.CoreClock.Now,
                    Items = paymentOrderItems
                }));

                context.SaveChanges();

                cache.SignalOrderItemsChanged(paymentOrderItems);
            }
        }

        public List<PaymentOrderItem> GetPaymentOrderItems()
        {
            List<PaymentOrderItem> GetStoredItems()
            {
                using (var context = contextFactory.CreateContext())
                {
                    var key = Guid.NewGuid().ToString();

                    var keyValueStore = new KeyValueStoreService(() => contextFactory.CreateContext());
                    var currentKey = keyValueStore.GetValue(CurrentKeyKeySpace, CurrentKeyKeySpace);
                    if (currentKey == null)
                        return DefaultBuiltinPaymentOrder.Value;

                    var currentValue = keyValueStore.GetValue(currentKey, ModelKeySpace);
                    var storedModel = JsonConvert.DeserializeObject<PaymentOrderStorageModel>(currentValue);
                    return storedModel.Items;
                }
            }

            List<PaymentOrderItem> GetItems()
            {
                var storedItems = GetStoredItems();

                var customCosts = costTypeService.GetCustomCosts();
                var definedCustomCostCodes = customCosts.Select(x => x.Code).ToHashSetShared();

                var returendItems = new List<PaymentOrderItem>();

                //Custom costs that have been removed are filtered out
                foreach (var item in storedItems)
                {
                    if (item.IsBuiltin || definedCustomCostCodes.Contains(item.Code))
                        returendItems.Add(item);
                }

                //Custom costs that have been added are included last
                foreach(var customCost in customCosts)
                {
                    if (!returendItems.Any(x => !x.IsBuiltin && x.Code == customCost.Code))
                        returendItems.Add(new PaymentOrderItem { IsBuiltin = false, Code = customCost.Code });
                }

                return returendItems;
            }

            return cache.GetPaymentOrderItems(GetItems);
        }

        public List<PaymentOrderUiItem> GetPaymentOrderUiItems()
        {
            var customTextSource = costTypeService.GetCustomCostTextSource();
            var lang = clientConfiguration.Country.GetBaseLanguage();
            Dictionary<string, string> localizedNames = null;
            if (lang == "sv")
                localizedNames = localizedNamesSv;

            return GetPaymentOrderItems().Select(x => new PaymentOrderUiItem
            {
                UniqueId = x.GetUniqueId(),
                OrderItem = x,
                Text = x.IsBuiltin ? localizedNames.Opt(x.GetUniqueId()) ?? x.Code : customTextSource(x.Code)
            }).ToList();
        }
        
        public PaymentOrderItem GetCustomCostPaymentOrderItem(string code)
        {
            var allItems = GetPaymentOrderItems();
            var item = allItems.SingleOrDefault(x => !x.IsBuiltin && x.Code == code);
            if (item == null)
                throw new NTechCoreWebserviceException("No such payment order item exists") { ErrorCode = "noSuchPaymentOrderItemExists" };
            return item;
        }

        public bool HasCustomCosts() => GetPaymentOrderItems().Any(x => !x.IsBuiltin);
        public bool IsValidUniqueId(string uniqueId) => GetItemByUniqueId(uniqueId) != null;
        public PaymentOrderItem GetItemByUniqueId(string uniqueId, bool allowRse = false)
        {
            if (allowRse && uniqueId == PaymentOrderItem.FromSwedishRse().GetUniqueId())
                return PaymentOrderItem.FromSwedishRse();

            return GetPaymentOrderItems().Where(x => x.GetUniqueId() == uniqueId).FirstOrDefault();
        }

        private static Dictionary<string, string> localizedNamesSv = new Dictionary<string, string>
        {
            { "b_Capital", "Kapital" },
            { "b_Interest", "Ränta" },
            { "b_ReminderFee", "Påminnelseavgift" },
            { "b_NotificationFee", "Aviavgift" },
            { "b_SwedishRseAmount", "RSE" },
        };

        private class PaymentOrderStorageModel
        {
            public int SchemaVersion { get; set; }
            public DateTimeOffset TransactionDate { get; set; }
            public List<PaymentOrderItem> Items { get; set; }
        }
    }

    public class PaymentOrderAndCostTypeCache
    {
        private List<PaymentOrderItem> cachedOrderItems = null;
        private List<CustomCost> cachedCosts = null;

        public void SignalCostsChanged(List<CustomCost> newCosts)
        {
            cachedCosts = newCosts;
            cachedOrderItems = null;            
        }

        public void SignalOrderItemsChanged(List<PaymentOrderItem> newOrderItems)
        {
            cachedOrderItems = newOrderItems;
        }

        public List<PaymentOrderItem> GetPaymentOrderItems(Func<List<PaymentOrderItem>> readFromDb)
        {
            if (cachedOrderItems != null) 
                return cachedOrderItems;

            var result = readFromDb();
            cachedOrderItems = result;
            return result;
        }

        public List<CustomCost> GetCustomCosts(Func<List<CustomCost>> readFromDb)
        {
            if (cachedCosts != null)
                return cachedCosts;

            var result = readFromDb();
            cachedCosts = result;
            return result;
        }
    }

    public class PaymentOrderItem : IEquatable<PaymentOrderItem>
    {
        public string Code { get; set; }
        public bool IsBuiltin { get; set; }
        public CreditDomainModel.AmountType GetBuiltinAmountType() => Enums.ParseReq<CreditDomainModel.AmountType>(Code);
        public bool IsCreditDomainModelAmountType(CreditDomainModel.AmountType type) => IsBuiltin && Code == type.ToString();
        public string GetDebugText() => GetUniqueId();
        public string GetUniqueId() => $"{(IsBuiltin ? "b" : "c")}_{Code}";
        public static string GetUniqueId(CreditDomainModel.AmountType type) => $"b_{type.ToString()}";

        //TODO: How do we integrate this with everything else?
        public static PaymentOrderItem FromSwedishRse() => new PaymentOrderItem { Code = "SwedishRseAmount", IsBuiltin = true };
        public static PaymentOrderItem FromCustomCostCode(string code) => new PaymentOrderItem { Code = code, IsBuiltin = false };
        public static PaymentOrderItem FromAmountType(CreditDomainModel.AmountType type) => new PaymentOrderItem { Code = type.ToString(), IsBuiltin = true };

        public bool Equals(PaymentOrderItem other)
        {
            if (other == null) return false;
            return GetUniqueId() == other.GetUniqueId();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals(obj as PaymentOrderItem);
        }

        public override int GetHashCode() => GetUniqueId().GetHashCode();
    }

    public class PaymentOrderUiItem
    {
        public string UniqueId { get; set; }
        public string Text { get; set; }
        public PaymentOrderItem OrderItem { get; set; }
    }
}