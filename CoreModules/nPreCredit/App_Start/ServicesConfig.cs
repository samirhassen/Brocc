using Autofac;
using nPreCredit.Code.Datasources;
using nPreCredit.Code.Services.LegacyUnsecuredLoans;
using nPreCredit.Code.Services.MortgageLoans;
using nPreCredit.Code.Services.NewUnsecuredLoans;
using nPreCredit.Code.Services.SharedStandard;
using nPreCredit.DbModel.Repository;
using nPreCredit.WebserviceMethods.UnsecuredLoansStandard;
using NTech;
using NTech.Core;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Core.PreCredit.Services;
using NTech.Core.PreCredit.Shared;
using NTech.Core.PreCredit.Shared.Services;
using NTech.Core.PreCredit.Shared.Services.UlLegacy;
using NTech.Core.PreCredit.Shared.Services.UlStandard;
using NTech.Core.PreCredit.Shared.Services.Utilities;
using NTech.Legacy.Module.Shared;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Legacy.Module.Shared.Services;
using System;

namespace nPreCredit.Code.Services
{
    public static class ServicesConfig
    {
        private static void R<TConcrete, TInterface>(ContainerBuilder builder) where TConcrete : TInterface
        {
            builder
                .RegisterType<TConcrete>()
                .InstancePerRequest();

            builder
                .RegisterType<TConcrete>()
                .As<TInterface>()
                .InstancePerRequest();
        }

        private static void RIf<TConcrete>(ContainerBuilder builder, bool isEnabled)
        {
            if (!isEnabled)
                return;

            builder
                .RegisterType<TConcrete>()
                .InstancePerRequest();
        }

        public static void RegisterServices(ContainerBuilder builder)
        {
            R<PartialCreditApplicationModelService, IPartialCreditApplicationModelService>(builder);
            builder.RegisterType<ApplicationInfoService>().InstancePerRequest();
            R<MortgageLoanApplicationCreditCheckService, IMortgageLoanApplicationCreditCheckService>(builder);
            R<HttpContextUrlService, IHttpContextUrlService>(builder);
            R<ApplicationCommentService, IApplicationCommentService>(builder);
            R<ApplicationCommentService, IApplicationCommentServiceComposable>(builder);
            R<KeyValueStoreService, IKeyValueStoreService>(builder);
            R<MortgageLoanApplicationValuationService, IMortgageLoanApplicationValuationService>(builder);
            R<MortgageLoanCurrentLoansService, IMortgageLoanCurrentLoansService>(builder);
            R<MortgageLoanApplicationDirectDebitCheckService, IMortgageLoanApplicationDirectDebitCheckService>(builder);
            R<MortgageLoanObjectService, IMortgageLoanObjectService>(builder);
            R<PublishEventService, IPublishEventService>(builder);
            R<CustomerInfoService, ICustomerInfoService>(builder);
            R<MortgageLoanWorkListService, IMortgageLoanWorkListService>(builder);
            R<MortgageLoanApplicationBasisService, IMortgageLoanApplicationBasisService>(builder);
            R<CustomerOfficialDataService, ICustomerOfficialDataService>(builder);
            builder.RegisterType<ApplicationCheckpointService>().InstancePerRequest();
            R<FraudModelService, IFraudModelService>(builder);
            R<ApplicationWaitingForAdditionalInformationService, IApplicationWaitingForAdditionalInformationService>(builder);
            R<ApplicationCancellationService, IApplicationCancellationService>(builder);
            R<ProviderInfoService, IProviderInfoService>(builder);
            R<MortgageApplicationRejectionService, IMortgageApplicationRejectionService>(builder);
            R<MortgageLoanApplicationInitialCreditCheckService, IMortgageLoanApplicationInitialCreditCheckService>(builder);
            R<EncryptedTemporaryStorageService, IEncryptedTemporaryStorageService>(builder);
            R<OtherApplicationsService, IOtherApplicationsService>(builder);
            R<MortgageLoanApplicationCreationService, IMortgageLoanApplicationCreationService>(builder);
            R<MortgageLoanApplicationAlterationService, IMortgageLoanApplicationAlterationService>(builder);
            R<ShowInfoOnNextPageLoadService, IShowInfoOnNextPageLoadService>(builder);
            R<ReferenceInterestRateService, IReferenceInterestRateService>(builder);
            R<ApplicationArchiveService, IApplicationArchiveService>(builder);
            R<CompanyLoanApplicationSearchService, ICompanyLoanApplicationSearchService>(builder);
            builder.RegisterType<SharedCreateApplicationService>().InstancePerRequest();
            R<CompanyLoans.CompanyLoanCreditCheckService, CompanyLoans.ICompanyLoanCreditCheckService>(builder);
            R<CompanyLoans.CompanyLoanCustomerCardUpdateService, CompanyLoans.ICompanyLoanCustomerCardUpdateService>(builder);
            builder.RegisterType<CreditApplicationListService>().InstancePerRequest();
            builder.RegisterType<CreditApplicationCustomerListService>().InstancePerRequest();
            R<CreditApplicationCustomerListService, ICreditApplicationCustomerListService>(builder);
            R<CompanyLoans.CompanyLoanApplicationApprovalService, CompanyLoans.ICompanyLoanApplicationApprovalService>(builder);
            R<CompanyLoans.CompanyLoanAgreementService, CompanyLoans.ICompanyLoanAgreementService>(builder);
            R<CompanyLoans.CompanyLoanAgreementSignatureService, CompanyLoans.ICompanyLoanAgreementSignatureService>(builder);
            R<LockedAgreementService, ILockedAgreementService>(builder);
            R<ComplexApplicationListService, IComplexApplicationListService>(builder);
            R<ComplexApplicationListReadOnlyService, IComplexApplicationListReadOnlyService>(builder);
            R<PreCreditContextFactoryService, IPreCreditContextFactoryService>(builder);
            R<WorkListService, IWorkListService>(builder);
            R<MortgageLoanLeadsWorkListService, IMortgageLoanLeadsWorkListService>(builder);
            R<ApplicationAssignedHandlerService, IApplicationAssignedHandlerService>(builder);
            R<AbTestingService, IAbTestingService>(builder);
            R<BankAccountDataShareService, IBankAccountDataShareService>(builder);
            R<LtlDataTables, ILtlDataTables>(builder);
            builder.RegisterType<NTechEnvironmentLegacy>().As<INTechEnvironment>().SingleInstance();

            //Shared for all standard products
            if (NEnv.IsStandardMortgageLoansEnabled || NEnv.IsStandardUnsecuredLoansEnabled)
            {
                builder.RegisterType<LoanStandardApplicationSearchService>().InstancePerRequest();
            }

            //Product specific
            if (NEnv.IsMortgageLoansEnabled && !NEnv.IsStandardMortgageLoansEnabled)
            {
                R<MortgageLoanWorkflowService, IMortgageLoanWorkflowService>(builder);
                R<MortgageLoanWorkflowService, ISharedWorkflowService>(builder);
                R<MortgageLoanWorkflowService, IMinimalSharedWorkflowService>(builder);
                R<MortgageLoans.MortgageLoanDualAgreementService, MortgageLoans.IMortgageLoanDualAgreementService>(builder);
                R<MortgageLoans.MortgageLoanDualApplicationAndPoaService, MortgageLoans.IMortgageLoanDualApplicationAndPoaService>(builder);
            }
            else if (NEnv.IsCompanyLoansEnabled)
            {
                R<CompanyLoans.CompanyLoanWorkflowService, CompanyLoans.ICompanyLoanWorkflowService>(builder);
                R<CompanyLoans.CompanyLoanWorkflowService, ISharedWorkflowService>(builder);
                R<CompanyLoans.CompanyLoanWorkflowService, IMinimalSharedWorkflowService>(builder);
            }
            else if (NEnv.IsStandardUnsecuredLoansEnabled)
            {
                builder.RegisterType<UnsecuredLoanStandardWorkflowService>().InstancePerRequest();
                R<UnsecuredLoanStandardWorkflowService, ISharedWorkflowService>(builder);
                R<UnsecuredLoanStandardWorkflowService, IMinimalSharedWorkflowService>(builder);
                builder.RegisterType<CreditRecommendationUlStandardService>().InstancePerRequest();
                builder.RegisterType<UnsecuredLoanStandardWorkflowService>().InstancePerRequest();
                builder.RegisterType<UnsecuredLoanStandardAgreementService>().InstancePerRequest();
                builder.RegisterType<UnsecuredLoans.UnsecuredLoanLtlAndDbrService>().InstancePerRequest();
                builder.RegisterType<LoanApplicationCreditReportService>().InstancePerRequest();
                builder.RegisterType<StandardPolicyFilters.DataSources.UnsecuredLoanStandardApplicationPolicyFilterDataSourceFactory>().InstancePerRequest();
                builder.RegisterType<CreateLoanFromApplicationUlStandardService>().InstancePerRequest();
                
            }
            else if (NEnv.IsStandardMortgageLoansEnabled)
            {
                R<MortgageLoanStandardWorkflowService, IMortgageLoanStandardWorkflowService>(builder);
                R<MortgageLoanStandardWorkflowService, ISharedWorkflowService>(builder);
                R<MortgageLoanStandardWorkflowService, IMinimalSharedWorkflowService>(builder);
                builder.RegisterType<LoanApplicationCreditReportService>().InstancePerRequest();
                builder.RegisterType<StandardPolicyFilters.DataSources.MortgageLoanStandardApplicationPolicyFilterDataSourceFactory>().InstancePerRequest();
                builder.RegisterType<MortgageLoanStandardCreditCheckService>().InstancePerRequest();
            }
            else if (!NEnv.IsStandardUnsecuredLoansEnabled && NEnv.IsUnsecuredLoansEnabled) //Legacy UL
            {
                builder
                    .RegisterType<LegacyUnsecuredLoansRejectionService>()
                    .InstancePerRequest();

                builder.RegisterType<PetrusOnlyCreditCheckService>().InstancePerRequest();
                builder.Register(x => Clients.SignicatSigningClientFactory.CreateClient()).As<Clients.ISignicatSigningClientReadOnly>();
                builder.RegisterType<UlLegacyAdditionalQuestionsService>().InstancePerRequest();

                R<ThrowExceptionMinimalSharedWorkflowService, IMinimalSharedWorkflowService>(builder);
            }
            else
            {
                R<ThrowExceptionMinimalSharedWorkflowService, IMinimalSharedWorkflowService>(builder);
            }

            RIf<PolicyFilterService>(builder, PolicyFilterService.IsEnabled(NEnv.EnvSettings));
            RIf<CreateApplicationUlStandardService>(builder, CreateApplicationUlStandardService.IsEnabled(NEnv.EnvSettings));
            RIf<CreateApplicationWithScoringUlStandardService>(builder, CreateApplicationUlStandardService.IsEnabled(NEnv.EnvSettings));
            RIf<NewCreditCheckUlStandardService>(builder, NEnv.IsStandardUnsecuredLoansEnabled);
            builder
                .Register(p => new CreditApplicationCustomEditableFieldsService(() => NEnv.CreditApplicationCustomEditableFieldsFile, new Lazy<int>(() => 2)))
                .As<ICreditApplicationCustomEditableFieldsService>()
                .SingleInstance();

            builder
                .Register(p => new ApplicationDocumentService(
                    LegacyServiceClientFactory.CreateDocumentClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry),
                    NEnv.IsMortgageLoansEnabled, //TODO: Make this injectable
                    (t, d) => NTech.Services.Infrastructure.Eventing.NTechEventHandler.PublishEvent(t.ToString(), d), //TODO: Make this injectable
                    p.Resolve<IPreCreditContextFactoryService>()
                    ))
                .As<IApplicationDocumentService>()
                .InstancePerRequest();

            builder
                .Register(p => new MortgageLoanAmortizationService(
                    p.Resolve<IKeyValueStoreService>(),
                    p.Resolve<IClock>(),
                    p.Resolve<IMortgageLoanApplicationValuationService>(),
                    p.Resolve<IPartialCreditApplicationModelService>(),
                    NEnv.MortgageLoanDefaultAmortizationFreeMonths,
                    NEnv.MortgageLoanClientMinimumAmortizationPercent,
                    p.Resolve<IMortgageLoanWorkflowService>()))
                .As<IMortgageLoanAmortizationService>()
                .InstancePerRequest();

            builder
               .RegisterType<MortgageLoanDualApplicationAndPoaService>()
               .As<IMortgageLoanDualApplicationAndPoaService>()
               .InstancePerRequest();

            builder.RegisterType<ServiceRegistryUrlService>().As<IServiceRegistryUrlService>();
            builder
                .Register(x => LegacyServiceClientFactory.CreateUserClient(LegacyHttpServiceHttpContextUser.SharedInstance, NEnv.ServiceRegistry))
                .As<NTech.Core.Module.Shared.Clients.IUserClient>()
                .InstancePerRequest();

            builder.Register(x => new ServiceRegistryLegacy(NEnv.ServiceRegistry)).As<INTechServiceRegistry>();
            builder.Register(x => SerilogLoggingService.SharedInstance).As<ILoggingService>();

            builder.Register(x => new UserDisplayNameService(x.Resolve<IUserClient>(), FewItemsCache.SharedInstance))
                .As<IUserDisplayNameService>()
                .InstancePerRequest();

            builder.RegisterType<UcbvService>().InstancePerRequest();
            builder.RegisterType<TestUcbvService>().InstancePerRequest();
            builder.RegisterType<Plugins.PluginCompanyLoanApplicationRequestTranslator>();
            builder.RegisterType<Plugins.PluginMortgageLoanApplicationRequestTranslator>();
            builder.RegisterType<Plugins.PluginMortgageLoanSubmitAdditionalQuestionsRequestTranslator>();
            R<CampaignCodeService, ICampaignCodeService>(builder);
            builder.RegisterType<SwedishDirectDebitConsentDocumentService>().InstancePerRequest();

            builder
                .Register<IUcbvService>(p =>
                {
                    if (NEnv.UseUcBvTestData)
                        return p.Resolve<TestUcbvService>();
                    else
                        return p.Resolve<UcbvService>();
                });

            //Data source
            builder.RegisterType<CreditApplicationItemDataSource>();
            builder.RegisterType<CreditApplicationInfoSource>();
            builder.RegisterType<BankAccountTypeAndNrCreditApplicationItemDataSource>();
            builder.RegisterType<ComplexApplicationListDataSource>();
            builder.RegisterType<CustomerCardItemDataSource>();
            builder.RegisterType<CurrentCreditDecisionItemsDataSource>();

            builder
                .Register(x => ApplicationDataSourceService.Create(
                    x.Resolve<ICreditApplicationCustomEditableFieldsService>(), x.Resolve<IPreCreditContextFactoryService>(),
                    x.Resolve<EncryptionService>(), x.Resolve<ApplicationInfoService>(), x.Resolve<ICustomerClient>()))
                .InstancePerRequest();

            R<CreditApplicationKeySequenceGenerator, ICreditApplicationKeySequenceGenerator>(builder);
            builder.Register(x => new EncryptionService(
                NEnv.EncryptionKeys.CurrentKeyName, NEnv.EncryptionKeys.AsDictionary(),
                x.Resolve<ICoreClock>(), x.Resolve<INTechCurrentUserMetadata>())).InstancePerRequest();
            builder
                .Register(x =>
                {
                    var user = x.Resolve<INTechCurrentUserMetadata>();
                    var clock = x.Resolve<ICombinedClock>();
                    return new PreCreditContextFactory(() => new PreCreditContextExtended(user, clock));
                }).InstancePerRequest();

            builder.Register(x => LegacyServiceClientFactory.CreateDocumentClient(LegacyHttpServiceHttpContextUser.SharedInstance, NEnv.ServiceRegistry)).InstancePerRequest();
            R<LoanStandardCustomerRelationService, ILoanStandardCustomerRelationService>(builder);
            R<LinqKitQueryExpander, ILinqQueryExpander>(builder);

            builder.RegisterType<UpdateCreditApplicationRepository>().InstancePerRequest();
            builder.RegisterType<HandlerLimitEngine>().InstancePerRequest();
            builder.RegisterType<LoanStandardEmailTemplateService>().InstancePerRequest();
            R<MarkdownTemplateRenderingServiceLegacyPreCredit, IMarkdownTemplateRenderingService>(builder);
        }
    }
}