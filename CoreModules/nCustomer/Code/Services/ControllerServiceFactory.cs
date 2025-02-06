using nCustomer.Code.Email;
using nCustomer.Code.Services.Kyc;
using nCustomer.Code.Services.Settings;
using nCustomer.DbModel;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Customer.Shared.Services;
using NTech.Core.Customer.Shared.Settings;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Services;
using NTech.Legacy.Module.Shared;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Legacy.Module.Shared.Services;
using NTech.Services.Infrastructure.Email;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace nCustomer.Code.Services
{
    public class ControllerServiceFactory
    {
        private readonly UrlHelper urlHelper;
        private readonly Func<string, string> getUserDisplayNameByUserId;
        private readonly ICombinedClock clock;
        private readonly Func<CustomersContext> createCustomersContext;
        private readonly Func<CustomersContext, NtechCurrentUserMetadata, CustomerWriteRepository> createCustomerRepository;
        private readonly Func<Dictionary<string, string>> getUserDisplayNamesByUserId;
        private readonly Lazy<KycScreeningProviderServiceFactory> kycServiceFactory;
        private readonly NtechCurrentUserMetadata currentUser;

        public ControllerServiceFactory(
            UrlHelper urlHelper,
            Func<string, string> getUserDisplayNameByUserId,
            ICombinedClock clock,
            Func<Dictionary<string, string>> getUserDisplayNamesByUserId,
            Lazy<KycScreeningProviderServiceFactory> kycServiceFactory,
            NtechCurrentUserMetadata currentUser)
        {
            this.urlHelper = urlHelper;
            this.getUserDisplayNameByUserId = getUserDisplayNameByUserId;
            this.clock = clock;
            this.createCustomersContext = () => new CustomersContext();
            this.createCustomerRepository = (db, u) => new CustomerWriteRepository(db, u.CoreUser, clock, EncryptionService, NEnv.ClientCfgCore);
            this.getUserDisplayNamesByUserId = getUserDisplayNamesByUserId;
            this.kycServiceFactory = kycServiceFactory;
            this.currentUser = currentUser;
        }

        public INTechEmailService Email
        {
            get
            {
                return Code.Email.EmailServiceFactory.CreateEmailService();
            }
        }

        public ICompanyLoanNameSearchService CompanyLoanNameSearch
        {
            get
            {
                return new CompanyLoanNameSearchService(clock);
            }
        }

        public IKycManagementService KycManagement
        {
            get
            {
                return new KycManagementService(CustomerContextFactory,
                    (db) => new CustomerWriteRepository((CustomersContext)db, currentUser.CoreUser, clock, EncryptionService, NEnv.ClientCfgCore),
                    Url, NEnv.ClientCfgCore, KycAnswersUpdate);
            }
        }

        public CustomerContextFactory CustomerContextFactory => new CustomerContextFactory(() => new CustomersContextExtended(currentUser.CoreUser));

        public IKeyValueStoreService KeyValueStore
        {
            get
            {
                return new KeyValueStoreService(createCustomersContext, CoreClock.SharedInstance);
            }
        }

        public IKycScreeningManagementService KycScreeningManagement
        {
            get
            {
                return new KycScreeningManagementService(CustomerContextFactory, EncryptionService);
            }
        }

        public IUrlService Url
        {
            get
            {
                return new UrlService(new ServiceRegistryLegacy(NEnv.ServiceRegistry));
            }
        }

        public ICustomerCommentService CustomerComment
        {
            get
            {
                return new CustomerCommentService(this.getUserDisplayNameByUserId, this.createCustomersContext, this.clock, this.Url);
            }
        }

        public IUserService User
        {
            get
            {
                return new UserService(this.getUserDisplayNameByUserId, this.getUserDisplayNamesByUserId);
            }
        }

        public ICustomerService Customer
        {
            get
            {
                return new CustomerService(createCustomersContext, createCustomerRepository);
            }
        }

        public ICompanyCustomerService CompanyCustomer
        {
            get
            {
                return new CompanyCustomerService(createCustomersContext, createCustomerRepository);
            }
        }

        public PersonCustomerService PersonCustomer
        {
            get
            {
                return new PersonCustomerService(CustomerContextFactory, EncryptionService, NEnv.ClientCfgCore);
            }
        }

        public ICustomerMessageService CustomerMessage
        {
            get
            {
                return new CustomerMessageService(createCustomersContext, (x, _) => new CustomerSearchRepository(x, EncryptionService, NEnv.ClientCfgCore),
                    new Lazy<ICompanyLoanNameSearchService>(() => CompanyLoanNameSearch), EmailServiceFactory,
                    KeyValueStore, ReadonlySettings, NEnv.ClientCfgCore, Logging, ServiceRegistry, EncryptionService);
            }
        }

        public ILoggingService Logging => new SerilogLoggingService();

        public INTechServiceRegistry ServiceRegistry => new ServiceRegistryLegacy(NEnv.ServiceRegistry);
        public INTechEmailServiceFactory EmailServiceFactory => new EmailServiceFactory.ServiceFactoryImpl();

        public IKycScreeningService KycScreening =>
            new KycScreeningService(
                (x, y) => new CustomerWriteRepository(x, y,
                    CoreClock.SharedInstance, EncryptionService, NEnv.ClientCfgCore), clock, new Lazy<IKycScreeningProviderServiceFactory>(() => kycServiceFactory.Value),
                KycManagement, currentUser.CoreUser, CustomerContextFactory, EncryptionService, NEnv.ClientCfgCore, NEnv.EnvSettings);

        public ReadonlySettingsService ReadonlySettings
        {
            get
            {
                return new ReadonlySettingsService(SettingsModelSource.GetSharedSettingsModelSource(NEnv.ClientCfgCore), KeyValueStore, NEnv.ClientCfgCore);
            }
        }

        public EncryptionService EncryptionService
        {
            get
            {
                var c = NEnv.EncryptionKeys;
                return new EncryptionService(c.CurrentKeyName, c.AsDictionary(), clock, currentUser.CoreUser);
            }
        }
        public KycQuestionsPeriodicUpdateService KycQuestionsUpdate
        {
            get
            {
                return new KycQuestionsPeriodicUpdateService(CustomerContextFactory, CoreClock.SharedInstance,
                    new CustomerMessageSendingService(CustomerMessage, currentUser.CoreUser, Logging), EmailServiceFactory,
                    NEnv.EnvSettings, Settings, EncryptionService, KycAnswersUpdate);
            }
        }

        public KycAnswersUpdateService KycAnswersUpdate =>
            new KycAnswersUpdateService(CustomerContextFactory, currentUser.CoreUser, CoreClock.SharedInstance, KycQuestionsTemplate, Settings, EncryptionService);

        public KycQuestionsTemplateService KycQuestionsTemplate
        {
            get
            {
                return new KycQuestionsTemplateService(CustomerContextFactory, NEnv.EnvSettings, NEnv.ClientCfgCore);
            }
        }

        public CachedSettingsService Settings =>
            new CachedSettingsService(LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry));
    }
}