using nCustomer.DbModel;
using NTech.Core;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer
{
    public abstract class CustomerWriteRepositoryBase : CustomerRepositorySimple
    {
        private readonly ICustomerContext db;
        private INTechCurrentUserMetadata currentUser;
        private CustomerDataNormalizer normalizer;
        private ICoreClock clock;
        private readonly EncryptionService encryptionService;

        public CustomerWriteRepositoryBase(
            ICustomerContext db,
            INTechCurrentUserMetadata currentUser,
            ICoreClock clock, EncryptionService encryptionService,
            IClientConfigurationCore clientConfiguration) : base(db, encryptionService) //http://security.stackexchange.com/questions/59580/how-to-safely-store-sensitive-data-like-a-social-security-number
        {
            this.db = db;
            this.currentUser = currentUser;
            normalizer = new CustomerDataNormalizer(clientConfiguration.Country.BaseCountry);
            this.clock = clock;
            this.encryptionService = encryptionService;
        }

        #region "Address hash"

        public static string ComputeAddressHash(IList<CustomerPropertyModel> items)
        {
            return NTech.Core.Customer.Shared.Services.CustomerServiceBase.ComputeAddressHash(items);
        }

        public static bool HasAllAddressHashFields(IList<CustomerPropertyModel> items)
        {
            return CustomerPropertyModel.AdressHashFieldNames.All(x => items.Any(y => y.Name == x));
        }

        public void UpdateAdressHashes(List<CustomerPropertyModel> items)
        {
            db.EnsureCurrentTransaction();

            bool isUpdatingAnyAdressField = items.Select(x => x.Name)
                           .Intersect(CustomerPropertyModel.AdressHashFieldNames)
                           .Any();

            foreach (var customer in items.GroupBy(x => x.CustomerId))
            {
                var customerId = customer.Key;
                var customerItems = customer.ToList();

                if (isUpdatingAnyAdressField && !customerItems.Any(x => x.Name == CustomerProperty.Codes.addressHash.ToString()))
                {
                    HashSet<string> names = new HashSet<string>(CustomerPropertyModel.AdressHashFieldNames);
                    var addressProps = BulkFetch(new HashSet<int>() { customerId }, propertyNames: names);
                    string addressHash = ComputeAddressHash(addressProps[customerId]);
                    var hashProp = new List<CustomerPropertyModel>() { new CustomerPropertyModel
                    {
                        Name = CustomerProperty.Codes.addressHash.ToString(),
                        Group = CustomerProperty.Groups.insensitive.ToString(),
                        CustomerId = customerId,
                        Value = addressHash
                    } };

                    UpdateProperties(hashProp, true);
                    db.SaveChanges();
                }
            }
        }

        #endregion "Address hash"

        public void UpdateProperties(IList<CustomerPropertyModel> customerProperties, bool force, string businessEventCode = null, bool updateEvenIfNotChanged = false)
        {
            db.EnsureCurrentTransaction();
            Set(customerProperties, force, businessEventCode: businessEventCode, updateEvenIfNotChanged: updateEvenIfNotChanged);
        }

        protected abstract void OnCustomerPropertiesAdded(params SearchTermUpdateItem[] items);
        protected abstract List<string> TranslateSearchTermValue(string term, string value);

        private void Set(IList<CustomerPropertyModel> customerProperties, bool forceUpdate, string businessEventCode = null, bool updateEvenIfNotChanged = false)
        {
            var updatedProperties = SetInternal(customerProperties, forceUpdate, businessEventCode: businessEventCode, updateEvenIfNotChanged: updateEvenIfNotChanged);
            db.SaveChanges();
            UpdateAdressHashes(updatedProperties);
        }

        private List<CustomerPropertyModel> SetInternal(IList<CustomerPropertyModel> customerPropertiesIn, bool forceUpdateIn, string businessEventCode = null, bool updateEvenIfNotChanged = false)
        {
            if (customerPropertiesIn.Any(x => x.CustomerId == 0))
                throw new Exception("Missing customerid");

            var updatedProperties = new List<CustomerPropertyModel>();
            var evt = new Lazy<BusinessEvent>(() => new BusinessEvent
            {
                EventDate = clock.Now,
                TransactionDate = clock.Now.Date,
                EventType = businessEventCode ?? BusinessEventCode.Generic.ToString(),
                UserId = currentUser.UserId
            });

            var customerIds = customerPropertiesIn.Select(x => x.CustomerId).Distinct().ToList();
            var allNames = customerPropertiesIn.Select(x => x.Name).Distinct().ToList();
            var currentItems = db
                .CustomerPropertiesQueryable
                .Where(x => x.IsCurrentData && customerIds.Contains(x.CustomerId) && allNames.Contains(x.Name)) //NOTE: Will overfetch but the exact detailed query isnt possible to translate using linq to entities. Will be good if the same properties are updated for all customers which seems likely.
                .ToList()
                .Where(x => customerPropertiesIn.Any(y => x.CustomerId == y.CustomerId && x.Name == y.Name))
                .ToList();

            var encryptedItems = currentItems.Where(x => x.IsEncrypted).ToList();
            IDictionary<long, string> decryptedValues;
            if (encryptedItems.Any())
            {
                decryptedValues = encryptionService.DecryptEncryptedValues(db,
                    encryptedItems.Select(x => long.Parse(x.Value)).ToArray());
            }
            else
            {
                decryptedValues = new Dictionary<long, string>();
            }

            var addedEncryptedProperties = new List<CustomerProperty>();

            var specialPropertyNames = new List<string> { CustomerProperty.Codes.addressHash.ToString() };
            var standardProperties = customerPropertiesIn.Where(x => !specialPropertyNames.Contains(x.Name)).ToList();
            var specialProperties = customerPropertiesIn.Where(x => specialPropertyNames.Contains(x.Name)).ToList();

            Action<IList<CustomerPropertyModel>, bool> handleProperties = (customerProperties, forceUpdatePre) =>
            {
                foreach (var customerProperty in customerProperties)
                {
                    var forceUpdate = forceUpdatePre || customerProperty.ForceUpdate.GetValueOrDefault();

                    CustomerProperty replacesCustomerProperty = null;
                    var customerPropertyToReplace = currentItems
                        .SingleOrDefault(x => x.CustomerId == customerProperty.CustomerId && x.Name == customerProperty.Name && x.IsCurrentData);

                    var currentValue = customerPropertyToReplace == null
                        ? null
                        : (customerPropertyToReplace.IsEncrypted
                            ? decryptedValues[long.Parse(customerPropertyToReplace.Value)]
                            : customerPropertyToReplace.Value);

                    if (customerPropertyToReplace != null)
                    {
                        if ((currentValue == customerProperty.Value && !updateEvenIfNotChanged) || CustomerPropertyModel.IsCustomerIdProperty(customerProperty.Name))
                        {
                            continue; //only update if value is actually changed. Never EVER update customerid fields.
                        }
                        else if (string.IsNullOrWhiteSpace(customerProperty.Value)) //We could allow this by just setting the current one to not current but would have to filter which fields we allow it on.
                            throw new Exception($"Attempted to delete property '{customerProperty.Name}'. This is not supported.");
                        else if (this.normalizer.NormalizeToSameValue(customerProperty.Name, currentValue, customerProperty.Value))
                        {
                            //Treat an update that normalizes to the same value as the current value as if nothing had changed
                            continue;
                        }
                    }
                    else if (string.IsNullOrWhiteSpace(customerProperty.Value))
                    {
                        continue; //Trying to remove a value that already doesnt exist is a null operation
                    }

                    if (customerPropertyToReplace != null)
                    {
                        replacesCustomerProperty = customerPropertyToReplace;
                        customerPropertyToReplace.IsCurrentData = false;
                    }
                    var property = new CustomerProperty
                    {
                        ChangedDate = clock.Now,
                        ChangedById = currentUser.UserId,
                        CustomerId = customerProperty.CustomerId,
                        InformationMetaData = currentUser.InformationMetadata,
                        Value = customerProperty.Value,
                        Name = customerProperty.Name,
                        Group = customerProperty.Group,
                        IsSensitive = customerProperty.IsSensitive,
                        ReplacesCustomerProperty = replacesCustomerProperty,
                        IsCurrentData = true,
                        IsEncrypted = customerProperty.IsSensitive,
                        CreatedByEvent = evt.Value
                    };
                    db.AddCustomerProperties(property);
                    updatedProperties.Add(customerProperty);
                    OnCustomerPropertiesAdded(new SearchTermUpdateItem { PropertyName = property.Name, CustomerId = property.CustomerId, ClearTextValue = property.Value });
                    if (property.IsEncrypted)
                        addedEncryptedProperties.Add(property);
                }
            };

            handleProperties(standardProperties, forceUpdateIn);

            foreach (var customerProperty in specialProperties)
            {
                if (customerProperty.Name == CustomerProperty.Codes.addressHash.ToString())
                {
                    handleProperties(new List<CustomerPropertyModel> { customerProperty }, true);
                }
                else
                    throw new NotImplementedException();
            }

            if (evt.IsValueCreated)
            {
                db.AddBusinessEvents(evt.Value);
            }

            if (addedEncryptedProperties.Any())
            {
                encryptionService.SaveEncryptItems(addedEncryptedProperties.ToArray(), x => x.Value, (x, y) => x.Value = y.ToString(), db);
            }

            return updatedProperties;
        }

        public class CustomerDataNormalizer
        {
            private readonly ConcurrentDictionary<string, Func<string, string>> normalizers;

            public CustomerDataNormalizer(string clientCountryIsoCode)
            {
                normalizers = new ConcurrentDictionary<string, Func<string, string>>(StringComparer.OrdinalIgnoreCase);

                //Phonenr
                normalizers["phone"] = nr => PhoneNumberHandler.GetInstance(clientCountryIsoCode).TryNormalizeToInternationalFormat(nr);
            }

            public bool NormalizeToSameValue(string itemName, string value1, string value2)
            {
                if (this.normalizers.ContainsKey(itemName))
                {
                    var n = this.normalizers[itemName];
                    return n(value1) == n(value2);
                }
                else
                    return false;
            }

            public string Normalize(string itemName, string rawValue)
            {
                if (this.normalizers.ContainsKey(itemName))
                    return this.normalizers[itemName](rawValue);
                else
                    return rawValue;
            }

            public bool HasNormalizer(string itemName)
            {
                return this.normalizers.ContainsKey(itemName);
            }
        }
    }
}