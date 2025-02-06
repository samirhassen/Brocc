using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nPreCredit;
using nPreCredit.Code.Services;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.PreCredit.Database;
using NTech.Core.PreCredit.Shared;
using NTech.Core.PreCredit.Shared.Code.PetrusOnlyScoringService;
using NTech.Core.PreCredit.Shared.Services;
using NTech.Core.PreCredit.Shared.Services.UlLegacy;
using NTech.Core.PreCredit.Shared.Services.UlStandard;
using NTech.Core.PreCredit.Shared.Services.UlStandard.ApplicationAutomation;
using NTech.Core.PreCredit.Shared.Services.Utilities;

namespace NTech.Core.PreCredit
{
    public class PreCreditNTechModule : NTechModule
    {
        public override string ServiceName => "nPreCredit";

        public override void AddServices(IServiceCollection services, NEnv env)
        {
            services.AddScoped<IPreCreditEnvSettings, PreCreditEnvSettings>();
            services.AddScoped(x =>
            {
                var user = x.GetRequiredService<INTechCurrentUserMetadata>();
                var clock = x.GetRequiredService<ICoreClock>();
                return new PreCreditContextFactory(() => new PreCreditContextExtended(user, clock));
            });
            services.AddScoped<IPreCreditContextFactoryService>(x => x.GetRequiredService<PreCreditContextFactory>());
            services.AddScoped<UnsecuredLoanStandardApplicationKycQuestionSessionService>();
            services.AddScoped<ApplicationInfoService>();
            services.AddScoped<PartialCreditApplicationModelRepository>();
            services.AddScoped<IPartialCreditApplicationModelRepository>(x => x.GetRequiredService<PartialCreditApplicationModelRepository>());
            services.AddScoped<IPartialCreditApplicationModelRepositoryExtended>(x => x.GetRequiredService<PartialCreditApplicationModelRepository>());
            services.AddScoped<ILinqQueryExpander>(_ => LinqQueryExpanderDoNothing.SharedInstance);
            services.AddScoped<KycQuestionsSessionCompletionCallbackService>();
            services.AddScoped<KycStepAutomation>();
            services.AddScoped<TranslationService>();
            services.AddScoped<ApplicationInfoService>();
            services.AddScoped<LegacyUlApplicationBasisService>();
            services.AddScoped<IApplicationCommentService, ApplicationCommentService>();
            services.AddScoped<IServiceRegistryUrlService, ServiceRegistryUrlService>();
            services.AddScoped<IUserDisplayNameService>(x =>
                new UserDisplayNameService(x.GetRequiredService<IUserClient>(), FewItemsCache.SharedInstance));
            services.AddScoped<PetrusOnlyScoringServiceFactory>();
            services.AddScoped<CreditApplicationItemService>();
            services.AddScoped<PolicyFilterService>();
            services.AddScoped<BankShareTestService>();
            services.AddScoped<KeyValueStoreService>();
        }

        public override void OnApplicationStarted(ILogger logger)
        {
            logger.LogInformation($"Testing precredit ef context");
            using (var context = new Database.PreCreditContext())
            {
                context.CreditApplicationCustomerListMembers.Count();
                logger.LogInformation($"Testing precredit  ef context: Ok");
            }
        }
    }
}