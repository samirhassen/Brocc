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
        private static void Reg<TConcrete, TInterface>(ContainerBuilder builder) where TConcrete : TInterface
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
            Reg<PartialCreditApplicationModelService, IPartialCreditApplicationModelService>(builder);
            builder.RegisterType<ApplicationInfoService>().InstancePerRequest();
            Reg<MortgageLoanApplicationCreditCheckService, IMortgageLoanApplicationCreditCheckService>(builder);
            Reg<HttpContextUrlService, IHttpContextUrlService>(builder);
            Reg<ApplicationCommentService, IApplicationCommentService>(builder);
            Reg<ApplicationCommentService, IApplicationCommentServiceComposable>(builder);
            Reg<KeyValueStoreService, IKeyValueStoreService>(builder);
            Reg<MortgageLoanApplicationValuationService, IMortgageLoanApplicationValuationService>(builder);
            Reg<MortgageLoanCurrentLoansService, IMortgageLoanCurrentLoansService>(builder);
            Reg<MortgageLoanApplicationDirectDebitCheckService, IMortgageLoanApplicationDirectDebitCheckService>(builder);
            Reg<MortgageLoanObjectService, IMortgageLoanObjectService>(builder);
            Reg<PublishEventService, IPublishEventService>(builder);
            Reg<CustomerInfoService, ICustomerInfoService>(builder);
            Reg<MortgageLoanWorkListService, IMortgageLoanWorkListService>(builder);
            Reg<MortgageLoanApplicationBasisService, IMortgageLoanApplicationBasisService>(builder);
            Reg<CustomerOfficialDataService, ICustomerOfficialDataService>(builder);
            builder.RegisterType<ApplicationCheckpointService>().InstancePerRequest();
            Reg<FraudModelService, IFraudModelService>(builder);
            Reg<ApplicationWaitingForAdditionalInformationService, IApplicationWaitingForAdditionalInformationService>(builder);
            Reg<ApplicationCancellationService, IApplicationCancellationService>(builder);
            Reg<ProviderInfoService, IProviderInfoService>(builder);
            Reg<MortgageApplicationRejectionService, IMortgageApplicationRejectionService>(builder);
            Reg<MortgageLoanApplicationInitialCreditCheckService, IMortgageLoanApplicationInitialCreditCheckService>(builder);
            Reg<EncryptedTemporaryStorageService, IEncryptedTemporaryStorageService>(builder);
            Reg<OtherApplicationsService, IOtherApplicationsService>(builder);
            Reg<MortgageLoanApplicationCreationService, IMortgageLoanApplicationCreationService>(builder);
            Reg<MortgageLoanApplicationAlterationService, IMortgageLoanApplicationAlterationService>(builder);
            Reg<ShowInfoOnNextPageLoadService, IShowInfoOnNextPageLoadService>(builder);
            Reg<ReferenceInterestRateService, IReferenceInterestRateService>(builder);
            Reg<ApplicationArchiveService, IApplicationArchiveService>(builder);
            Reg<CompanyLoanApplicationSearchService, ICompanyLoanApplicationSearchService>(builder);
            builder.RegisterType<SharedCreateApplicationService>().InstancePerRequest();
            Reg<CompanyLoans.CompanyLoanCreditCheckService, CompanyLoans.ICompanyLoanCreditCheckService>(builder);
            Reg<CompanyLoans.CompanyLoanCustomerCardUpdateService, CompanyLoans.ICompanyLoanCustomerCardUpdateService>(builder);
            builder.RegisterType<CreditApplicationListService>().InstancePerRequest();
            builder.RegisterType<CreditApplicationCustomerListService>().InstancePerRequest();
            Reg<CreditApplicationCustomerListService, ICreditApplicationCustomerListService>(builder);
            Reg<CompanyLoans.CompanyLoanApplicationApprovalService, CompanyLoans.ICompanyLoanApplicationApprovalService>(builder);
            Reg<CompanyLoans.CompanyLoanAgreementService, CompanyLoans.ICompanyLoanAgreementService>(builder);
            Reg<CompanyLoans.CompanyLoanAgreementSignatureService, CompanyLoans.ICompanyLoanAgreementSignatureService>(builder);
            Reg<LockedAgreementService, ILockedAgreementService>(builder);
            Reg<ComplexApplicationListService, IComplexApplicationListService>(builder);
            Reg<ComplexApplicationListReadOnlyService, IComplexApplicationListReadOnlyService>(builder);
            Reg<PreCreditContextFactoryService, IPreCreditContextFactoryService>(builder);
            Reg<WorkListService, IWorkListService>(builder);
            Reg<MortgageLoanLeadsWorkListService, IMortgageLoanLeadsWorkListService>(builder);
            Reg<ApplicationAssignedHandlerService, IApplicationAssignedHandlerService>(builder);
            Reg<AbTestingService, IAbTestingService>(builder);
            Reg<BankAccountDataShareService, IBankAccountDataShareService>(builder);
            Reg<LtlDataTables, ILtlDataTables>(builder);
            builder.RegisterType<NTechEnvironmentLegacy>().As<INTechEnvironment>().SingleInstance();

            //Shared for all standard products
            if (NEnv.IsStandardMortgageLoansEnabled || NEnv.IsStandardUnsecuredLoansEnabled)
            {
                builder.RegisterType<LoanStandardApplicationSearchService>().InstancePerRequest();
            }

            //Product specific
            if (NEnv.IsMortgageLoansEnabled && !NEnv.IsStandardMortgageLoansEnabled)
            {
                Reg<MortgageLoanWorkflowService, IMortgageLoanWorkflowService>(builder);
                Reg<MortgageLoanWorkflowService, ISharedWorkflowService>(builder);
                Reg<MortgageLoanWorkflowService, IMinimalSharedWorkflowService>(builder);
                Reg<MortgageLoans.MortgageLoanDualAgreementService, MortgageLoans.IMortgageLoanDualAgreementService>(builder);
                Reg<MortgageLoans.MortgageLoanDualApplicationAndPoaService, MortgageLoans.IMortgageLoanDualApplicationAndPoaService>(builder);
            }
            else if (NEnv.IsCompanyLoansEnabled)
            {
                Reg<CompanyLoans.CompanyLoanWorkflowService, CompanyLoans.ICompanyLoanWorkflowService>(builder);
                Reg<CompanyLoans.CompanyLoanWorkflowService, ISharedWorkflowService>(builder);
                Reg<CompanyLoans.CompanyLoanWorkflowService, IMinimalSharedWorkflowService>(builder);
            }
            else if (NEnv.IsStandardUnsecuredLoansEnabled)
            {
                builder.RegisterType<UnsecuredLoanStandardWorkflowService>().InstancePerRequest();
                Reg<UnsecuredLoanStandardWorkflowService, ISharedWorkflowService>(builder);
                Reg<UnsecuredLoanStandardWorkflowService, IMinimalSharedWorkflowService>(builder);
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
                Reg<MortgageLoanStandardWorkflowService, IMortgageLoanStandardWorkflowService>(builder);
                Reg<MortgageLoanStandardWorkflowService, ISharedWorkflowService>(builder);
                Reg<MortgageLoanStandardWorkflowService, IMinimalSharedWorkflowService>(builder);
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

                Reg<ThrowExceptionMinimalSharedWorkflowService, IMinimalSharedWorkflowService>(builder);
            }
            else
            {
                Reg<ThrowExceptionMinimalSharedWorkflowService, IMinimalSharedWorkflowService>(builder);
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
            Reg<CampaignCodeService, ICampaignCodeService>(builder);
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

            Reg<CreditApplicationKeySequenceGenerator, ICreditApplicationKeySequenceGenerator>(builder);
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
            Reg<LoanStandardCustomerRelationService, ILoanStandardCustomerRelationService>(builder);
            Reg<LinqKitQueryExpander, ILinqQueryExpander>(builder);

            builder.RegisterType<UpdateCreditApplicationRepository>().InstancePerRequest();
            builder.RegisterType<HandlerLimitEngine>().InstancePerRequest();
            builder.RegisterType<LoanStandardEmailTemplateService>().InstancePerRequest();
            Reg<MarkdownTemplateRenderingServiceLegacyPreCredit, IMarkdownTemplateRenderingService>(builder);
        }
    }
}