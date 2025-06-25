using System;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Autofac;
using Autofac.Core.Lifetime;
using Autofac.Integration.Mvc;
using nPreCredit.App_Start;
using nPreCredit.Code;
using nPreCredit.Code.AffiliateReporting;
using nPreCredit.Code.Agreements;
using nPreCredit.Code.Balanzia;
using nPreCredit.Code.BalanziaSe;
using nPreCredit.Code.Email;
using nPreCredit.Code.Scoring.BalanziaScoringRules;
using nPreCredit.Code.Services;
using NTech;
using NTech.Banking.Autogiro;
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
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.Email;
using CreditClient = nPreCredit.Code.CreditClient;
using ICreditClient = nPreCredit.Code.ICreditClient;
using IDocumentClient = nPreCredit.Code.IDocumentClient;
using IUserClient = nPreCredit.Code.Clients.IUserClient;
using UserClient = nPreCredit.Code.Clients.UserClient;

namespace nPreCredit
{
    //TODO: This is scaffolding while getting DI everywhere. Remove this class when that is done.
    public static class DependancyInjection
    {
        public static IDependencyResolver Services { get; set; }

        public static void WithNewRequestScope(Action<ILifetimeScope> a, INTechCurrentUserMetadata currentUser)
        {
            using (var scope = DependancyInjectionConfig.Container.BeginLifetimeScope(
                       MatchingScopeLifetimeTags.RequestLifetimeScopeTag,
                       builder => { builder.Register(p => currentUser).As<INTechCurrentUserMetadata>(); }))
            {
                a(scope);
            }
        }

        public static void WithAffiliateReportingBackgroundSeviceScope(Action<ILifetimeScope> a)
        {
            using (var scope =
                   DependancyInjectionConfig.Container.BeginLifetimeScope(
                       MatchingScopeLifetimeTags.RequestLifetimeScopeTag, builder => { }))
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
            AffiliateReportingInjectionConfig.ConfigureServices(builder);
            Container = builder.Build();
            var resolver = new AutofacDependencyResolver(Container);
            DependancyInjection.Services = resolver;
            DependencyResolver.SetResolver(resolver);
        }

        private static void ConfigureServices(ContainerBuilder builder)
        {
            builder.RegisterType<NTechExternalAssemblyLoader>().SingleInstance();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            builder.RegisterControllers(typeof(Global).Assembly);
            builder.RegisterModule<AutofacWebTypesModule>();

            builder.Register<NTechServiceRegistry>(p => NEnv.ServiceRegistry);

            //Per http request instances
            builder
                .Register(x => x.Resolve<HttpContextBase>()?.User?.Identity)
                .As<IIdentity>()
                .InstancePerRequest();

            builder.Register(x => NEnv.ClientCfgCore).As<IClientConfigurationCore>().InstancePerRequest();
            builder.RegisterType<LegacyPreCreditEnvSettings>().As<IPreCreditEnvSettings>().InstancePerRequest();

            builder
                .Register(x =>
                {
                    var user = x.Resolve<HttpContextBase>()?.User?.Identity as IIdentity;
                    return new NTechCurrentUserMetadataImpl(user);
                })
                .As<INTechCurrentUserMetadata>()
                .InstancePerRequest();

            ServicesConfig.RegisterServices(builder);

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
                .RegisterType<AgreementSigningProvider>()
                .InstancePerRequest();

            builder
                .RegisterType<UlLegacyAgreementSignatureService>()
                .InstancePerRequest();

            builder.RegisterType<AdditionalQuestionsSender>()
                .As<IAdditionalQuestionsSender>()
                .InstancePerRequest();

            builder.Register<IAdServiceIntegrationService>(p =>
                new AdServiceIntegrationService(NEnv.AdServicesSettings));

            builder
                .Register<ICreditApplicationTypeHandler>(p =>
                {
                    var user = p.Resolve<INTechCurrentUserMetadata>();
                    var clock = p.Resolve<IClock>();
                    if (NEnv.IsUnsecuredLoansEnabled)
                        return new UnsecuredCreditApplicationTypeHandler(clock,
                            p.Resolve<IPartialCreditApplicationModelRepository>(),
                            p.Resolve<IHttpContextUrlService>(),
                            p.Resolve<LoanAgreementPdfBuilderFactory>());
                    else
                        throw new NotImplementedException();
                })
                .InstancePerRequest();

            builder
                .Register(p => EmailServiceFactory.CreateEmailService())
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
            builder.Register(p => NEnv.ClientCfg).As<IClientConfiguration>();
            builder.RegisterType<AutogiroPaymentNumberGenerator>();

            builder.RegisterType<PreCreditCustomerClient>().As<ICustomerClient>();
            builder.RegisterType<CreditClient>().As<ICreditClient>();
            builder.Register(x =>
                    LegacyServiceClientFactory.CreateCreditClient(LegacyHttpServiceHttpContextUser.SharedInstance,
                        NEnv.ServiceRegistry))
                .As<NTech.Core.Module.Shared.Clients.ICreditClient>()
                .InstancePerRequest();
            builder.RegisterType<UcBvCreditReportClient>().As<IUcBvCreditReportClient>();
            builder.Register(x => new CreditReportService(
                x.Resolve<ServiceClientFactory>(), LegacyHttpServiceHttpContextUser.SharedInstance,
                NEnv.EnvSettings, NEnv.ClientCfgCore));
            builder.Register(x => x.Resolve<CreditReportService>())
                .As<ICreditReportClient>();
            builder.Register(x => x.Resolve<CreditReportService>()).As<ICreditReportService>();
            builder.RegisterType<nDocumentClient>().As<IDocumentClient>();
            builder.RegisterType<UserClient>().As<IUserClient>();
            builder.Register(x =>
            {
                //We keep this here instead of in NEnv since this is a client specific strange edgecase that we dont want shared or reused.
                var settingValue =
                    NTechEnvironment.Instance.Setting("ntech.precredit.scoringRuleRejectIfBelowRandomNr", false);
                var rejectIfBelowRandomNr = settingValue == null
                    ? RandomNrScoringVariableProvider.RejectHalfCutOff
                    : int.Parse(settingValue);
                return new RandomNrScoringVariableProvider(
                    x.Resolve<IPreCreditContextFactoryService>(),
                    rejectIfBelowRandomNr) as IRandomNrScoringVariableProvider;
            }).InstancePerRequest();
            builder.RegisterType<ServiceClientSyncConverterLegacy>().As<IServiceClientSyncConverter>();

            builder.Register(p =>
                new CachedSettingsService(
                    LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance,
                        NEnv.ServiceRegistry)));
            builder.RegisterType<PetrusOnlyScoringServiceFactory>().InstancePerRequest();

            builder.Register(x => new LoanAgreementPdfBuilderFactory(
                    x.Resolve<ICombinedClock>(), x.Resolve<IPartialCreditApplicationModelRepository>(),
                    x.Resolve<ICustomerClient>(),
                    x.Resolve<IClientConfigurationCore>(), x.Resolve<PreCreditContextFactory>(),
                    x.Resolve<ILoggingService>(),
                    x.Resolve<IPreCreditEnvSettings>(), x.Resolve<NTech.Core.Module.Shared.Clients.IDocumentClient>(),
                    Translations.GetTranslationTable()?.Opt("fi"),
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

            BalanziaSeDiRegistration.ConfigureServices(builder);
            BalanziaFiDiRegistration.ConfigureServices(builder);
        }
    }
}