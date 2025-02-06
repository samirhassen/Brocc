using Microsoft.Extensions.DependencyInjection;
using Moq;
using nPreCredit;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.NewUnsecuredLoans;
using nPreCredit.Code.Services.SharedStandard;
using NTech.Banking.PluginApis.CreateApplication;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Services;
using NTech.Core.PreCredit.Services;
using NTech.Core.PreCredit.Shared;
using NTech.Core.PreCredit.Shared.Services;
using NTech.Core.PreCredit.Shared.Services.UlStandard;
using NTech.Core.PreCredit.Shared.Services.UlStandard.ApplicationAutomation;

namespace NTech.Core.Host.IntegrationTests.Shared.Services
{
    public static class SharedPreCredit
    {
        public static Mock<IPreCreditClient> CreateClient(SupportShared support, Func<ServiceProvider> getProvider)
        {
            var preCreditClient = new Mock<IPreCreditClient>(MockBehavior.Strict);
            preCreditClient
                 .Setup(x => x.ReportKycQuestionSessionCompleted(It.IsAny<string>()))
                 .Callback<string>(sessionId =>
                 {
                     var provider = getProvider();
                     var completionService = provider.GetRequiredService<KycQuestionsSessionCompletionCallbackService>();
                     completionService.HandleKycQuestionSessionCompleted(sessionId);
                 });

            preCreditClient
                .Setup(x => x.LoanStandardApproveKycStep(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Callback<string, bool, bool>((applicationNr, isApproved, isAutomatic) =>
                {
                    //TODO: Migrate and implement
                });

            return preCreditClient;
        }

        public static void RegisterServices(SupportShared support, ServiceCollection services, Func<ServiceProvider> getProvider)
        {
            services.AddTransient(x =>
            {
                var creditReportService = new Mock<ICreditReportService>(MockBehavior.Strict);
                return new LoanApplicationCreditReportService(support.CurrentUser, creditReportService.Object,
                    SharedCustomer.CreateClient(support).Object, x.GetRequiredService<IPreCreditEnvSettings>(),
                    support.ClientConfiguration, x.GetRequiredService<IPreCreditContextFactoryService>());
            });
            services.AddTransient<PreCreditContextFactory>(x => new PreCreditContextFactory(
                () => x.GetRequiredService<IPreCreditContextFactoryService>().CreateExtended()));
            services.AddTransient<KeyValueStoreService>();
            services.AddTransient<PolicyFilterService>();
            services.AddTransient<KycQuestionsSessionCompletionCallbackService>();
            services.AddTransient<KycStepAutomation>();
            services.AddTransient<IPreCreditClient>(_ => CreateClient(support, getProvider).Object);
            services.AddTransient<ApplicationInfoService>();
            services.AddTransient<PartialCreditApplicationModelRepository>();
            services.AddTransient<IPartialCreditApplicationModelRepository>(x => x.GetRequiredService<PartialCreditApplicationModelRepository>());
            services.AddTransient<IPreCreditEnvSettings>(_ => ((ISupportSharedPreCredit)support).PreCreditEnvSettings);
            services.AddTransient<IPreCreditContextFactoryService>(_ => ((ISupportSharedPreCredit)support).PreCreditContextService);
            services.AddTransient<UnsecuredLoanStandardApplicationKycQuestionSessionService>();
            services.AddTransient(x =>
            {
                return new UnsecuredCreditApplicationProviderRepository(
                   x.GetRequiredService<IPreCreditEnvSettings>(),
                   support.Clock,
                   support.ClientConfiguration,
                   SharedCustomer.CreateClient(support).Object,
                   () => new CoreLegacyUnsecuredCreditApplicationDbWriter(support.CurrentUser, support.Clock, support.EncryptionService),
                   new CreditApplicationKeySequenceGenerator(((ISupportSharedPreCredit)support).PreCreditContextService));
            });

            services.AddTransient<ApplicationDataSourceService>(x =>
                ApplicationDataSourceService.Create(
                    x.GetRequiredService<ICreditApplicationCustomEditableFieldsService>(), 
                    x.GetRequiredService<IPreCreditContextFactoryService>(),
                    x.GetRequiredService<EncryptionService>(),
                    x.GetRequiredService<ApplicationInfoService>(),
                    x.GetRequiredService<ICustomerClient>()));
            services.AddTransient<IKeyValueStoreService, KeyValueStoreService>();
            services.AddTransient<CreditApplicationListService>();
            services.AddTransient<UnsecuredLoanStandardWorkflowService>();
            services.AddTransient<CreateApplicationUlStandardService>();
            services.AddTransient<SharedCreateApplicationService>();
            services.AddTransient<ICampaignCodeService>(_ =>
            {
                //TODO: Migrate
                var campaignService = new Mock<ICampaignCodeService>(MockBehavior.Strict);
                campaignService
                    .Setup(x => x.MatchCampaignOnCreateApplication(It.IsAny<List<CreateApplicationRequestModel.ComplexItem>>()))
                    .Returns(new List<CreateApplicationRequestModel.ComplexItem>());
                return campaignService.Object;
            });
            services.AddTransient<CreditApplicationCustomerListService>();
            services.AddTransient<ILoanStandardCustomerRelationService>(_ =>
            {
                //TODO: Migrate
                var relationSevice = new Mock<ILoanStandardCustomerRelationService>(MockBehavior.Loose);
                return relationSevice.Object;
            });
            services.AddTransient<ICreditApplicationCustomEditableFieldsService, CreditApplicationCustomEditableFieldsService>(_ => new CreditApplicationCustomEditableFieldsService(new Lazy<int>(() => 2)));
            services.AddTransient<ApplicationInfoService>();
            services.AddTransient<CreditRecommendationUlStandardService>();
            services.AddTransient<IReferenceInterestRateService, ReferenceInterestRateService>();
            services.AddTransient<ICreditClient, DatabaseCreditClient>();
            services.AddTransient<IApplicationCommentService, ApplicationCommentService>();
            services.AddSingleton<IUserDisplayNameService>(_ =>
            {
                var m = new Mock<IUserDisplayNameService>(MockBehavior.Strict);
                m
                    .Setup(x => x.GetUserDisplayNameByUserId(It.IsAny<string>()))
                    .Returns<string>(userId => $"User {userId}");
                m
                    .Setup(x => x.GetUserDisplayNamesByUserId())
                    .Returns(new Dictionary<string, string>());
                return m.Object;
            });
            services
                .AddSingleton<IServiceRegistryUrlService>(_ =>
                {
                    var m = new Mock<IServiceRegistryUrlService>(MockBehavior.Strict);
                    return m.Object;
                });
            services.AddSingleton<IMarkdownTemplateRenderingService>(_ =>
            {
                var m = new Mock<IMarkdownTemplateRenderingService>(MockBehavior.Strict);
                m
                    .Setup(x => x.RenderTemplateToHtml(It.IsAny<string>()))
                    .Returns<string>(template => template);
                return m.Object;
            });
            services.AddTransient<LoanStandardEmailTemplateService>();
        }
    }
}
