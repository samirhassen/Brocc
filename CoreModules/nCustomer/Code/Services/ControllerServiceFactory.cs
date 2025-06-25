using System;
using System.Collections.Generic;
using System.Web.Mvc;
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

namespace nCustomer.Code.Services
{
    public class ControllerServiceFactory
    {
        private readonly UrlHelper _urlHelper;
        private readonly Func<string, string> _getUserDisplayNameByUserId;
        private readonly ICombinedClock _clock;
        private readonly Func<CustomersContext> _createCustomersContext;

        private readonly Func<CustomersContext, NtechCurrentUserMetadata, CustomerWriteRepository>
            _createCustomerRepository;

        private readonly Func<Dictionary<string, string>> _getUserDisplayNamesByUserId;
        private readonly Lazy<KycScreeningProviderServiceFactory> _kycServiceFactory;
        private readonly NtechCurrentUserMetadata _currentUser;

        public ControllerServiceFactory(
            UrlHelper urlHelper,
            Func<string, string> getUserDisplayNameByUserId,
            ICombinedClock clock,
            Func<Dictionary<string, string>> getUserDisplayNamesByUserId,
            Lazy<KycScreeningProviderServiceFactory> kycServiceFactory,
            NtechCurrentUserMetadata currentUser)
        {
            _urlHelper = urlHelper;
            _getUserDisplayNameByUserId = getUserDisplayNameByUserId;
            _clock = clock;
            _createCustomersContext = () => new CustomersContext();
            _createCustomerRepository = (db, u) =>
                new CustomerWriteRepository(db, u.CoreUser, clock, EncryptionService, NEnv.ClientCfgCore);
            _getUserDisplayNamesByUserId = getUserDisplayNamesByUserId;
            _kycServiceFactory = kycServiceFactory;
            _currentUser = currentUser;
        }

        public INTechEmailService Email => Code.Email.EmailServiceFactory.CreateEmailService();

        public ICompanyLoanNameSearchService CompanyLoanNameSearch => new CompanyLoanNameSearchService(_clock);

        public IKycManagementService KycManagement
        {
            get
            {
                return new KycManagementService(CustomerContextFactory,
                    (db) => new CustomerWriteRepository((CustomersContext)db, _currentUser.CoreUser, _clock,
                        EncryptionService, NEnv.ClientCfgCore),
                    Url, NEnv.ClientCfgCore, KycAnswersUpdate);
            }
        }

        public CustomerContextFactory CustomerContextFactory =>
            new CustomerContextFactory(() => new CustomersContextExtended(_currentUser.CoreUser));

        public IKeyValueStoreService KeyValueStore =>
            new KeyValueStoreService(_createCustomersContext, CoreClock.SharedInstance);

        public IKycScreeningManagementService KycScreeningManagement =>
            new KycScreeningManagementService(CustomerContextFactory, EncryptionService);

        public IUrlService Url => new UrlService(new ServiceRegistryLegacy(NEnv.ServiceRegistry));

        public ICustomerCommentService CustomerComment =>
            new CustomerCommentService(_getUserDisplayNameByUserId, _createCustomersContext, _clock, Url);

        public IUserService User => new UserService(_getUserDisplayNameByUserId, _getUserDisplayNamesByUserId);

        public ICustomerService Customer => new CustomerService(_createCustomersContext, _createCustomerRepository);

        public ICompanyCustomerService CompanyCustomer =>
            new CompanyCustomerService(_createCustomersContext, _createCustomerRepository);

        public PersonCustomerService PersonCustomer =>
            new PersonCustomerService(CustomerContextFactory, EncryptionService, NEnv.ClientCfgCore);

        public ICustomerMessageService CustomerMessage
        {
            get
            {
                return new CustomerMessageService(_createCustomersContext,
                    (x, _) => new CustomerSearchRepository(x, EncryptionService, NEnv.ClientCfgCore),
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
                    CoreClock.SharedInstance, EncryptionService, NEnv.ClientCfgCore), _clock,
                new Lazy<IKycScreeningProviderServiceFactory>(() => _kycServiceFactory.Value),
                KycManagement, _currentUser.CoreUser, CustomerContextFactory, EncryptionService, NEnv.ClientCfgCore,
                NEnv.EnvSettings);

        public ReadonlySettingsService ReadonlySettings =>
            new ReadonlySettingsService(SettingsModelSource.GetSharedSettingsModelSource(NEnv.ClientCfgCore),
                KeyValueStore, NEnv.ClientCfgCore);

        public EncryptionService EncryptionService
        {
            get
            {
                var c = NEnv.EncryptionKeys;
                return new EncryptionService(c.CurrentKeyName, c.AsDictionary(), _clock, _currentUser.CoreUser);
            }
        }

        public KycQuestionsPeriodicUpdateService KycQuestionsUpdate =>
            new KycQuestionsPeriodicUpdateService(CustomerContextFactory, CoreClock.SharedInstance,
                new CustomerMessageSendingService(CustomerMessage, _currentUser.CoreUser, Logging),
                EmailServiceFactory,
                NEnv.EnvSettings, Settings, EncryptionService, KycAnswersUpdate);

        public KycAnswersUpdateService KycAnswersUpdate =>
            new KycAnswersUpdateService(CustomerContextFactory, _currentUser.CoreUser, CoreClock.SharedInstance,
                KycQuestionsTemplate, Settings, EncryptionService);

        public KycQuestionsTemplateService KycQuestionsTemplate =>
            new KycQuestionsTemplateService(CustomerContextFactory, NEnv.EnvSettings, NEnv.ClientCfgCore);

        public CachedSettingsService Settings =>
            new CachedSettingsService(
                LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance,
                    NEnv.ServiceRegistry));
    }
}