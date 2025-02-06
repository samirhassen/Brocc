using nCustomer.DbModel;
using NTech.Banking.CivicRegNumbers;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Customer.Shared.Services;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer.Code.Services
{
    public class PersonCustomerService : CustomerServiceBase
    {
        private readonly CustomerContextFactory contextFactory;
        private readonly EncryptionService encryptionService;
        private readonly IClientConfigurationCore clientConfiguration;

        public PersonCustomerService(
            CustomerContextFactory contextFactory,
            EncryptionService encryptionService,
            IClientConfigurationCore clientConfiguration)
        {
            this.contextFactory = contextFactory;
            this.encryptionService = encryptionService;
            this.clientConfiguration = clientConfiguration;
        }

        public int CreateOrUpdatePerson(ICivicRegNumber civicRegNr, IDictionary<string, string> properties, ISet<string> additionalSensitiveProperties = null, int? expectedCustomerId = null, string externalEventCode = null, ISet<string> forceUpdateProperties = null)
        {
            CheckForBannedProperties(properties);

            int actualCustomerId;
            using (var context = contextFactory.CreateContext())
            {
                actualCustomerId = CustomerIdSourceCore.GetCustomerIdByCivicRegNr(civicRegNr, context);
            }

            if (expectedCustomerId.HasValue && expectedCustomerId.Value != actualCustomerId)
                throw new Exception($"Expected customerid to be {expectedCustomerId.Value} but it was instead {actualCustomerId}");

            var items = new List<CustomerPropertyModel>();

            Action<string, string> add = (name, value) =>
                {
                    items.Add(CustomerPropertyModel.Create(
                        actualCustomerId,
                        name,
                        value,
                        false,
                        forceUpdate: (forceUpdateProperties?.Contains(name) ?? false),
                        forceSensetiveIfNoTemplate: additionalSensitiveProperties != null && additionalSensitiveProperties.Contains(name)));
                };

            add(CustomerProperty.Codes.civicRegNr.ToString(), civicRegNr.NormalizedValue);

            foreach (var p in properties)
                add(p.Key, p.Value);

            if (!items.Any(x => x.Name == CustomerProperty.Codes.birthDate.ToString()) && civicRegNr.BirthDate.HasValue)
                add(CustomerProperty.Codes.birthDate.ToString(), civicRegNr.BirthDate.Value.ToString("yyyy-MM-dd"));

            using (var db = contextFactory.CreateContext())
            {
                db.BeginTransaction();
                try
                {
                    var repository = new CustomerWriteRepository(db, db.CurrentUser, db.CoreClock, encryptionService, clientConfiguration);
                    repository.UpdateProperties(items, false, businessEventCode: externalEventCode == null ? null : $"E_{externalEventCode}");
                    db.SaveChanges();
                    db.CommitTransaction();
                }
                catch
                {
                    db.RollbackTransaction();
                    throw;
                }
            }

            return actualCustomerId;
        }
    }
}