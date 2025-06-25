using System;
using System.IO;
using System.Xml.Linq;
using ICSharpCode.SharpZipLib.Zip;
using nCredit.Code.Email;
using nCredit.DbModel;
using nCredit.DbModel.BusinessEvents;
using nCredit.DbModel.DomainModel;
using nCredit.DbModel.Repository;
using NTech.Banking.BookKeeping;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Legacy.Module.Shared;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Legacy.Module.Shared.Services;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;

namespace nCredit.Code.Services
{
    public class ControllerServiceFactory
    {
        private readonly Func<string, string> getUserDisplayNameByUserId;
        private readonly ICombinedClock clock;
        private readonly Lazy<INTechWsUrlService> wsUrlService;
        private readonly Lazy<INTechCurrentUserMetadata> user;

        public ControllerServiceFactory(
            Func<string, string> getUserDisplayNameByUserId,
            ICombinedClock clock,
            Lazy<INTechWsUrlService> wsUrlService,
            Lazy<INTechCurrentUserMetadata> user)
        {
            this.getUserDisplayNameByUserId = getUserDisplayNameByUserId;
            this.clock = clock;
            this.wsUrlService = wsUrlService;
            this.user = user;
        }

        public CustomerCreditHistoryRepository CustomerCreditHistory =>
            new CustomerCreditHistoryRepository(ContextFactory);

        public ICreditDocumentsService CreditDocuments =>
            new CreditDocumentsService(DocumentClientHttpContext, ServiceRegistry, ContextFactory);

        public ICreditSecurityService CreditSecurity => new CreditSecurityService(ContextFactory);

        public ISnailMailLoanDeliveryService UnsecuredLoansDelivery =>
            new ZippedPdfsLoanDeliveryService(
                DocumentClientHttpContext, ContextFactory,
                NEnv.EnvSettings, NEnv.ClientCfgCore);

        public EInvoiceFiBusinessEventManager EInvoiceFi =>
            new EInvoiceFiBusinessEventManager(user.Value, clock, NEnv.ClientCfgCore, GetEncryptionService(user.Value),
                ContextFactory, NEnv.EnvSettings);

        public CreditContextFactory ContextFactory => new CreditContextFactory(() => CreateCreditContext(user.Value));

        public DirectDebitNotificationDeliveryService DirectDebitNotificationDeliveryService =>
            new DirectDebitNotificationDeliveryService(
                LegacyServiceClientFactory.CreateDocumentClient(LegacyHttpServiceHttpContextUser.SharedInstance,
                    NEnv.ServiceRegistry),
                PaymentAccount, ContextFactory, NEnv.EnvSettings, PaymentOrder, NEnv.ClientCfgCore);

        public IDirectDebitService GetDirectDebitService(INTechCurrentUserMetadata _)
        {
            var directDebitBusinessEventManager = new DirectDebitBusinessEventManager(user.Value, clock,
                NEnv.ClientCfgCore, ContextFactory, NEnv.EnvSettings);
            return new DirectDebitService(clock, BankAccountValidation, directDebitBusinessEventManager,
                UserDisplayName, PaymentAccount, ContextFactory, NEnv.EnvSettings,
                NEnv.ClientCfgCore, CustomerClientHttpContext, DocumentClientHttpContext);
        }

        public IBankAccountValidationService BankAccountValidation =>
            new BankAccountValidationService(NEnv.ClientCfgCore);

        public CreditTerminationLettersInactivationBusinessEventManager
            CreditTerminationLettersInactivationBusinessEventManager =>
            new CreditTerminationLettersInactivationBusinessEventManager(
                user.Value, clock, NEnv.ClientCfgCore, ContextFactory, TerminationLetterCandidateService);

        public IUcCreditRegistryService UcCreditRegistryService
        {
            get
            {
                return new UcCreditRegistryService(() => NEnv.UcCreditRegistrySettings, clock,
                    NEnv.IsMortgageLoansEnabled, () => new CreditCustomerClient(),
                    () => NEnv.BaseCivicRegNumberParser, ContextFactory);
            }
        }

        public KycService Kyc => new KycService(new CreditCustomerClient());

        public INTechWsUrlService WsUrl => wsUrlService.Value;

        public IKeyValueStoreService KeyValueStore =>
            new KeyValueStoreService(() => new CreditContextExtended(user.Value, CoreClock.SharedInstance));

        public IOutgoingPaymentsService OutgoingPayments => new OutgoingPaymentsService(ContextFactory);

        public ICustomerRelationsMergeService CustomerRelationsMerge =>
            new CustomerRelationsMergeService(new CreditCustomerClient(), ContextFactory);

        public IReferenceInterestChangeService ReferenceInterestChange =>
            new ReferenceInterestChangeService(getUserDisplayNameByUserId, KeyValueStore,
                clock, LegalInterestCeiling, NEnv.EnvSettings,
                NEnv.ClientCfgCore, ContextFactory);

        public IUserDisplayNameService UserDisplayName => new UserDisplayNameService(new UserClient());

        public LegalInterestCeilingService LegalInterestCeiling => new LegalInterestCeilingService(NEnv.EnvSettings);

        public PdfTemplateReader PdfTemplateReader => PdfTemplateReaderLegacy.GetReader(x =>
        {
            var fs = new FastZip();
            using (var ms = new MemoryStream())
            {
                fs.CreateZip(ms, x, true, null, null);
                return ms.ToArray();
            }
        });

        public byte[] GetPdfTemplate(string templateName) => PdfTemplateReader.GetPdfTemplate(templateName,
            NEnv.ClientCfgCore.Country.BaseCountry, NEnv.IsTemplateCacheDisabled);


        public DocumentRenderer GetDocumentRenderer(bool useDelayedDocuments) => new DocumentRenderer(
            DocumentClientHttpContext,
            useDelayedDocuments, NEnv.EnvSettings, LoggingService, PdfTemplateReader, NEnv.ClientCfgCore);

        public NotificationService GetNotificationService(bool useDelayedDocuments)
        {
            return new NotificationService(clock, UnsecuredLoansDelivery, PaymentAccount, ContextFactory,
                LoggingService, NEnv.EnvSettings, NEnv.ClientCfgCore,
                NEnv.NotificationProcessSettings, CustomerClientHttpContext,
                new NotificationDocumentRenderer(CreateDocumentRenderer), AlternatePaymentPlan, user.Value,
                PaymentOrder);

            IDocumentRenderer CreateDocumentRenderer() => GetDocumentRenderer(useDelayedDocuments);
        }

        public AlternatePaymentPlanService AlternatePaymentPlan => new AlternatePaymentPlanService(ContextFactory,
            NEnv.NotificationProcessSettings,
            NEnv.EnvSettings, NEnv.ClientCfgCore, CachedSettings, CustomerClientHttpContext, user.Value,
            CoreClock.SharedInstance,
            NustacheTemplateService.SharedInstance, PaymentOrder);

        public AlternatePaymentPlanSecureMessagesService AlternatePaymentPlanSecureMessages =>
            new AlternatePaymentPlanSecureMessagesService(ContextFactory, NEnv.NotificationProcessSettings,
                NEnv.EnvSettings, NEnv.ClientCfgCore, CachedSettings, CustomerClientHttpContext, AlternatePaymentPlan,
                user.Value,
                CoreClock.SharedInstance, NustacheTemplateService.SharedInstance);

        public ReminderService CreateReminderService(bool useDelayedDocuments)
        {
            var mgr = CreateNewCreditRemindersBusinessEventManager(useDelayedDocuments);
            return new ReminderService(
                DocumentClientHttpContext, mgr, LoggingService, NEnv.NotificationProcessSettings,
                CustomerClientHttpContext, NEnv.EnvSettings);
        }

        public MortgageLoansUpdateChangeTermsService CreateMortgageLoansChangeTermsService(
            INTechCurrentUserMetadata currentUser)
        {
            var customerClient =
                LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance,
                    NEnv.ServiceRegistry);
            var mgr = new MortgageLoansCreditTermsChangeBusinessEventManager(
                currentUser,
                new LegalInterestCeilingService(NEnv.EnvSettings), CoreClock.SharedInstance, null,
                ContextFactory, NEnv.EnvSettings, customerClient, LoggingService,
                new ServiceRegistryLegacy(NEnv.ServiceRegistry),
                EmailServiceFactory.SharedInstance, CachedSettings);
            return new MortgageLoansUpdateChangeTermsService(
                mgr, LoggingService, NEnv.NotificationProcessSettings,
                CustomerClientHttpContext, NEnv.EnvSettings);
        }

        public ICreditCustomerListServiceComposable CreditCustomerListService =>
            new CreditCustomerListServiceComposable();

        public EncryptionService GetEncryptionService(INTechCurrentUserMetadata currentUser)
        {
            var encryptionKeys = NEnv.EncryptionKeys;
            return new EncryptionService(encryptionKeys.CurrentKeyName, encryptionKeys.AsDictionary(), new CoreClock(),
                currentUser);
        }

        public ICreditContextExtended CreateCreditContext(INTechCurrentUserMetadata currentUser) =>
            new CreditContextExtended(currentUser, clock);

        public OcrPaymentReferenceGenerator CreateOcrPaymentReferenceGenerator(INTechCurrentUserMetadata currentUser) =>
            new OcrPaymentReferenceGenerator(NEnv.ClientCfg.Country.BaseCountry,
                () => CreateCreditContext(currentUser));

        public CachedSettingsService CachedSettings => new CachedSettingsService(CustomerClientHttpContext);

        public PaymentAccountService PaymentAccount =>
            new PaymentAccountService(CachedSettings, NEnv.EnvSettings, NEnv.ClientCfgCore);

        public IDocumentClient DocumentClientHttpContext =>
            LegacyServiceClientFactory.CreateDocumentClient(LegacyHttpServiceHttpContextUser.SharedInstance,
                NEnv.ServiceRegistry);

        public ICustomerClient CustomerClientHttpContext =>
            LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceHttpContextUser.SharedInstance,
                NEnv.ServiceRegistry);

        public INTechServiceRegistry ServiceRegistry => new ServiceRegistryLegacy(NEnv.ServiceRegistry);

        public CalendarDateService CalendarDateService =>
            new CalendarDateService(CalendarDateService.EarliestCalendarDateProduction, ContextFactory);

        public ILoggingService LoggingService { get; } = new SerilogLoggingService();

        public NewCreditRemindersBusinessEventManager CreateNewCreditRemindersBusinessEventManager(
            bool useDelayedDocuments)
        {
            return new NewCreditRemindersBusinessEventManager(user.Value, PaymentAccount, CoreClock.SharedInstance,
                NEnv.ClientCfgCore, NEnv.NotificationProcessSettings, NEnv.EnvSettings, ContextFactory, LoggingService,
                CreateDocumentRenderer,
                PaymentOrder);

            IDocumentRenderer CreateDocumentRenderer() => GetDocumentRenderer(useDelayedDocuments);
        }

        public DebtCollectionCandidateService DebtCollectionCandidate =>
            new DebtCollectionCandidateService(clock, ContextFactory, CustomerClientHttpContext, NEnv.ClientCfgCore);

        public CreditTermsChangeBusinessEventManager CreditTermsChangeBusinessEventManager =>
            new CreditTermsChangeBusinessEventManager(user.Value,
                LegalInterestCeiling, clock, NEnv.ClientCfgCore, ContextFactory,
                NEnv.EnvSettings, EmailServiceFactory.SharedInstance,
                LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance,
                    NEnv.ServiceRegistry),
                LoggingService, ServiceRegistry, x => NEnv.GetAffiliateModel(x));

        public CreditDebtCollectionBusinessEventManager CreditDebtCollectionBusinessEventManager =>
            new CreditDebtCollectionBusinessEventManager(
                user.Value, DocumentClientHttpContext, clock, NEnv.ClientCfgCore, NEnv.EnvSettings,
                CreditTermsChangeBusinessEventManager,
                CustomerClientHttpContext, DebtCollectionCandidate, PaymentOrder);

        public NewCreditTerminationLettersBusinessEventManager NewCreditTerminationLettersBusinessEventManager =>
            new NewCreditTerminationLettersBusinessEventManager(
                user.Value, PaymentAccount, clock, NEnv.ClientCfgCore, NEnv.NotificationProcessSettings, ContextFactory,
                NEnv.EnvSettings,
                TerminationLetterCandidateService, LoggingService, PaymentOrder);

        public TerminationLetterCandidateService TerminationLetterCandidateService =>
            new TerminationLetterCandidateService(
                clock, DebtCollectionCandidate, NEnv.NotificationProcessSettings, NEnv.EnvSettings, ContextFactory,
                CustomerClientHttpContext, NEnv.ClientCfgCore);


        public TerminationLetterService GetTerminationLetterService(bool useDelayedDocuments)
        {
            return new TerminationLetterService(CreateDocumentRenderer, NewCreditTerminationLettersBusinessEventManager,
                NEnv.NotificationProcessSettings, CustomerClientHttpContext,
                NEnv.ClientCfgCore, LoggingService, DocumentClientHttpContext);

            IDocumentRenderer CreateDocumentRenderer() => GetDocumentRenderer(useDelayedDocuments);
        }

        public MortgageLoanCollateralService MortgageLoanCollateral =>
            new MortgageLoanCollateralService(user.Value, clock, NEnv.ClientCfgCore);

        public SwedishMortgageLoanRseService RseService => new SwedishMortgageLoanRseService(
            ContextFactory, NEnv.NotificationProcessSettings, CoreClock.SharedInstance, NEnv.ClientCfgCore,
            NEnv.EnvSettings);

        public LoanStandardAnnualSummaryService LoanStandardAnnualSummary => new LoanStandardAnnualSummaryService(
            ContextFactory, NEnv.ClientCfgCore,
            DocumentClientHttpContext, CustomerClientHttpContext, GetPdfTemplate, NEnv.EnvSettings);

        public BookKeepingFileManager BookKeeping => new BookKeepingFileManager(user.Value, NEnv.ClientCfgCore,
            CoreClock.SharedInstance,
            NEnv.EnvSettings, ContextFactory,
            () => NtechBookKeepingRuleFile.Parse(XDocuments.Load(NEnv.BookKeepingRuleFileName)));

        public NewOutgoingPaymentFileBusinessEventManager NewOutgoingPaymentFile =>
            new NewOutgoingPaymentFileBusinessEventManager(user.Value, DocumentClientHttpContext,
                NEnv.ClientCfgCore, clock, GetEncryptionService(user.Value), NEnv.EnvSettings);

        public PaymentOrderAndCostTypeCache PaymentOrderAndCostCache => new PaymentOrderAndCostTypeCache();

        public CustomCostTypeService CustomCostType =>
            new CustomCostTypeService(ContextFactory, PaymentOrderAndCostCache);

        public PaymentOrderService PaymentOrder => new PaymentOrderService(ContextFactory, CustomCostType,
            PaymentOrderAndCostCache, NEnv.ClientCfgCore);

        public NewMortgageLoanBusinessEventManager NewMortgageLoanManager => new NewMortgageLoanBusinessEventManager(
            user.Value, CreditCustomerListService, CreateOcrPaymentReferenceGenerator(user.Value),
            CoreClock.SharedInstance, NEnv.ClientCfgCore, NEnv.EnvSettings, NEnv.NotificationProcessSettings,
            CustomCostType);

        public CreditAttentionStatusService CreditAttentionStatus =>
            new CreditAttentionStatusService(ContextFactory, new CurrentNotificationStateServiceLegacy(),
                NEnv.EnvSettings, NEnv.ClientCfgCore);
    }
}