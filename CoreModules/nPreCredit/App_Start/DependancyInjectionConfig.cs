using Autofac;
using Autofac.Core.Lifetime;
using Autofac.Integration.Mvc;
using nPreCredit.Code;
using nPreCredit.Code.Agreements;
using nPreCredit.Code.Balanzia;
using nPreCredit.Code.Email;
using nPreCredit.Code.Scoring.BalanziaScoringRules;
using nPreCredit.Code.Services;
using NTech;
using NTech.Core;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Core.PreCredit.Shared;
using NTech.Core.PreCredit.Shared.Code.PetrusOnlyScoringService;
using NTech.Core.PreCredit.Shared.Services;
using NTech.Core.PreCredit.Shared.Services.UlLegacy;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Legacy.Module.Shared.Services;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.Email;
using System;
using System.Web;
using System.Web.Mvc;

namespace nPreCredit
{
    //TODO: This is scaffolding while getting DI everywhere. Remove this class when that is done.
    public static class DependancyInjection
    {
        public static IDependencyResolver Services { get; set; }

        public static void WithNewRequestScope(Action<ILifetimeScope> a, INTechCurrentUserMetadata currentUser)
        {
            using (var scope = App_Start.DependancyInjectionConfig.Container.BeginLifetimeScope(MatchingScopeLifetimeTags.RequestLifetimeScopeTag, builder =>
            {
                builder.Register(p => currentUser).As<INTechCurrentUserMetadata>();
            }))
            {
                a(scope);
            }
        }

        public static void WithAffiliateReportingBackgroundSeviceScope(Action<ILifetimeScope> a)
        {
            using (var scope = App_Start.DependancyInjectionConfig.Container.BeginLifetimeScope(MatchingScopeLifetimeTags.RequestLifetimeScopeTag, builder =>
            {
            }))
            {
                a(scope);
            }
        }
    }

    public static class DependencyResolverExtensions
    {
        public static T Resolve<T>(this IDependencyResolver source)
        {
            return source.GetService<T>();
        }
    }
}

namespace nPreCredit.App_Start
{
    public static class DependancyInjectionConfig
    {
        public static IContainer Container { get; set; }

        public static void Configure()
        {
            var builder = new ContainerBuilder();
            ConfigureServices(builder);
            Code.AffiliateReporting.AffiliateReportingInjectionConfig.ConfigureServices(builder);
            Container = builder.Build();
            var resolver = new AutofacDependencyResolver(Container);
            DependancyInjection.Services = resolver;
            System.Web.Mvc.DependencyResolver.SetResolver(resolver);
        }

        private static void ConfigureServices(ContainerBuilder builder)
        {
            builder.RegisterType<NTech.Services.Infrastructure.NTechExternalAssemblyLoader>().SingleInstance();

            builder.RegisterControllers(typeof(Global).Assembly);
            builder.RegisterModule<AutofacWebTypesModule>();

            builder.Register<NTech.Services.Infrastructure.NTechServiceRegistry>(p => NEnv.ServiceRegistry);

            //Per http request instances
            builder
                .Register(x => x.Resolve<HttpContextBase>()?.User?.Identity)
                .As<System.Security.Principal.IIdentity>()
            .InstancePerRequest();

            builder.Register(x => NEnv.ClientCfgCore).As<IClientConfigurationCore>().InstancePerRequest();
            builder.RegisterType<LegacyPreCreditEnvSettings>().As<IPreCreditEnvSettings>().InstancePerRequest();

            builder
                .Register(x =>
                {
                    var user = x.Resolve<HttpContextBase>()?.User?.Identity as System.Security.Principal.IIdentity;
                    return new NTechCurrentUserMetadataImpl(user);
                })
                .As<INTechCurrentUserMetadata>()
                .InstancePerRequest();

            Code.Services.ServicesConfig.RegisterServices(builder);

            builder
                .RegisterType<PartialCreditApplicationModelRepository>()
                .As<IPartialCreditApplicationModelRepository>()
                .InstancePerRequest();

            builder
                .RegisterType<PartialCreditApplicationModelRepository>()
                .As<IPartialCreditApplicationModelRepositoryExtended>()
                .InstancePerRequest();

            builder
                .RegisterType<UpdateCreditApplicationRepository>()
                .InstancePerRequest();

            builder
                .RegisterType<Code.AgreementSigningProvider>()
                .InstancePerRequest();

            builder
                .RegisterType<UlLegacyAgreementSignatureService>()
                .InstancePerRequest();

            builder.RegisterType<AdditionalQuestionsSender>()
                .As<IAdditionalQuestionsSender>()
                .InstancePerRequest();

            builder.Register<IAdServiceIntegrationService>(p => new Code.Services.AdServiceIntegrationService(NEnv.AdServicesSettings));

            builder
                .Register<Code.ICreditApplicationTypeHandler>(p =>
                    {
                        var user = p.Resolve<INTechCurrentUserMetadata>();
                        var clock = p.Resolve<IClock>();
                        if (NEnv.IsUnsecuredLoansEnabled)
                            return new Code.UnsecuredCreditApplicationTypeHandler(clock,
                                p.Resolve<IPartialCreditApplicationModelRepository>(), p.Resolve<Code.Services.IHttpContextUrlService>(),
                                p.Resolve<LoanAgreementPdfBuilderFactory>());
                        else
                            throw new NotImplementedException();
                    })
                .InstancePerRequest();

            builder
                .Register(p => Code.Email.EmailServiceFactory.CreateEmailService())
                .As<INTechEmailService>();
            builder
                .Register(_ => new EmailServiceFactory.ServiceFactoryImpl())
                .As<INTechEmailServiceFactory>();

            builder
                .Register(p =>
                {
                    var s = NEnv.CampaignCodeSettings;
                    return new BalanziaCampaignCode(s.DisableRemoveInitialFee, s.DisableForceManualControl);
                })
                .As<ICampaignCode>()
                .SingleInstance();

            builder.Register(p => ClockFactory.SharedInstance).As<IClock>();
            builder.RegisterType<CoreClock>().As<ICoreClock>();
            builder.RegisterType<CoreClock>().As<ICombinedClock>();
            builder.Register(p => NEnv.BaseCivicRegNumberParser);
            builder.Register(p => NEnv.BaseOrganisationNumberParser);
            builder.Register(p => NEnv.MortgageLoanScoringSetup);
            builder.Register(p => NEnv.ClientCfg).As<NTech.Services.Infrastructure.IClientConfiguration>();
            builder.RegisterType<NTech.Banking.Autogiro.AutogiroPaymentNumberGenerator>();

            builder.RegisterType<Code.PreCreditCustomerClient>().As<ICustomerClient>();
            builder.RegisterType<Code.CreditClient>().As<Code.ICreditClient>();
            builder.Register(x =>
                LegacyServiceClientFactory.CreateCreditClient(LegacyHttpServiceHttpContextUser.SharedInstance, NEnv.ServiceRegistry))
                .As<NTech.Core.Module.Shared.Clients.ICreditClient>()
                .InstancePerRequest();
            builder.RegisterType<Code.UcBvCreditReportClient>().As<Code.IUcBvCreditReportClient>();
            builder.Register(x => new CreditReportService(
                x.Resolve<ServiceClientFactory>(), LegacyHttpServiceHttpContextUser.SharedInstance,
                NEnv.EnvSettings, NEnv.ClientCfgCore));
            builder.Register(x => x.Resolve<CreditReportService>()).As<NTech.Core.Module.Shared.Clients.ICreditReportClient>();
            builder.Register(x => x.Resolve<CreditReportService>()).As<ICreditReportService>();
            builder.RegisterType<Code.nDocumentClient>().As<Code.IDocumentClient>();
            builder.RegisterType<Code.Clients.UserClient>().As<Code.Clients.IUserClient>();
            builder.Register(x =>
            {
                //We keep this here instead of in NEnv since this is a client specific strange edgecase that we dont want shared or reused.
                var settingValue = NTechEnvironment.Instance.Setting("ntech.precredit.scoringRuleRejectIfBelowRandomNr", false);
                var rejectIfBelowRandomNr = settingValue == null ? RandomNrScoringVariableProvider.RejectHalfCutOff : int.Parse(settingValue);
                return new RandomNrScoringVariableProvider(
                    x.Resolve<IPreCreditContextFactoryService>(),
                    rejectIfBelowRandomNr) as IRandomNrScoringVariableProvider;
            }).InstancePerRequest();
            builder.RegisterType<ServiceClientSyncConverterLegacy>().As<IServiceClientSyncConverter>();

            builder.Register(p => new CachedSettingsService(LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry)));
            builder.RegisterType<PetrusOnlyScoringServiceFactory>().InstancePerRequest();            

            builder.Register(x => new LoanAgreementPdfBuilderFactory(
                x.Resolve<ICombinedClock>(), x.Resolve<IPartialCreditApplicationModelRepository>(), x.Resolve<ICustomerClient>(),
                x.Resolve<IClientConfigurationCore>(), x.Resolve<PreCreditContextFactory>(), x.Resolve<ILoggingService>(),
                x.Resolve<IPreCreditEnvSettings>(), x.Resolve<NTech.Core.Module.Shared.Clients.IDocumentClient>(), Translations.GetTranslationTable()?.Opt("fi"),
                y => nDocumentClient.GetPdfTemplate(y, disableTemplateCache: NEnv.IsTemplateCacheDisabled)))
                .InstancePerRequest();

            builder.RegisterType<CustomerServiceRepository>().As<ICustomerServiceRepository>();

            builder.Register(x => LegacyServiceClientFactory.CreateClientFactory(NEnv.ServiceRegistry));
            builder.Register(x =>
                new CreditReportService(
                    x.Resolve<ServiceClientFactory>(),
                    LegacyHttpServiceHttpContextUser.SharedInstance,
                    NEnv.EnvSettings,
                    NEnv.ClientCfgCore))
                .InstancePerRequest();

            builder.RegisterType<CreditManagementWorkListService>().InstancePerRequest();

            Code.BalanziaSe.BalanziaSeDiRegistration.ConfigureServices(builder);
            Code.Balanzia.BalanziaFiDiRegistration.ConfigureServices(builder);
        }
    }
}