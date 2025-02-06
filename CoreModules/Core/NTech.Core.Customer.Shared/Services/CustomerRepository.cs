using NTech.Core;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System.Collections.Generic;

namespace nCustomer
{
    public class CustomerWriteRepository : CustomerWriteRepositoryBase
    {
        private readonly ICustomerContext db;
        private readonly INTechCurrentUserMetadata currentUser;
        private readonly ICoreClock clock;
        private readonly IClientConfigurationCore clientConfiguration;

        public CustomerWriteRepository(
            ICustomerContext db,
            INTechCurrentUserMetadata currentUser,
            ICoreClock clock, EncryptionService encryptionService,
            IClientConfigurationCore clientConfiguration) : base(db, currentUser, clock, encryptionService, clientConfiguration)
        {
            this.db = db;
            this.currentUser = currentUser;
            this.clock = clock;
            this.clientConfiguration = clientConfiguration;
        }

        //TODO: When CustomerSearchTermRepository is moved to core, merge CustomerRepositoryBase back into this and delete CustomerRepositoryBase
        protected override void OnCustomerPropertiesAdded(params SearchTermUpdateItem[] items)
        {
            CustomerSearchTerms.OnCustomerPropertiesAddedShared(db, currentUser, clock, clientConfiguration, items);
        }

        //TODO: When CustomerSearchTermRepository is moved to core, merge CustomerRepositoryBase back into this and delete CustomerRepositoryBase
        protected override List<string> TranslateSearchTermValue(string term, string value)
        {
            return CustomerSearchTerms.TranslateSearchTermValue(term, value, clientConfiguration);
        }
    }

    public class CustomerSearchRepository : CustomerSearchRepositoryBase
    {
        private readonly IClientConfigurationCore clientConfiguration;

        public CustomerSearchRepository(
            ICustomerContext db,
            EncryptionService encryptionService,
            IClientConfigurationCore clientConfiguration) : base(db, encryptionService)
        {
            this.clientConfiguration = clientConfiguration;
        }

        //TODO: When CustomerSearchTermRepository is moved to core, merge CustomerRepositoryBase back into this and delete CustomerRepositoryBase
        protected override List<string> TranslateSearchTermValue(string term, string value)
        {
            return CustomerSearchTerms.TranslateSearchTermValue(term, value, clientConfiguration);
        }
    }
}