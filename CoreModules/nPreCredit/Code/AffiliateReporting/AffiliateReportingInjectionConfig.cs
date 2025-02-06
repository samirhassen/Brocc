using Autofac;
using NTech.Core.Module.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using System;
using System.Linq;

namespace nPreCredit.Code.AffiliateReporting
{
    internal static class AffiliateReportingInjectionConfig
    {
        public static void ConfigureServices(ContainerBuilder builder)
        {
            builder
                .RegisterAssemblyTypes(typeof(JsonAffiliateWebserviceBase).Assembly)
                .Where(t => t.IsSubclassOf(typeof(JsonAffiliateWebserviceBase)))
                .AsImplementedInterfaces()
                .SingleInstance();

            builder
                .RegisterAssemblyTypes(typeof(AffiliateCallbackDispatcherBase).Assembly)
                .Where(t => t.IsSubclassOf(typeof(AffiliateCallbackDispatcherBase)))
                .SingleInstance();

            builder
                .RegisterType<AffiliateCallbackDispatcherFactory>()
                .As<IAffiliateCallbackDispatcherFactory>()
                .SingleInstance();

            builder
                .RegisterType<FileSystemAffiliateDataSource>()
                .As<IAffiliateDataSource>()
                .SingleInstance();

            builder
                .RegisterType<DbThrottlingPolicyDataSource>()
                .As<IThrottlingPolicyDataSource>()
                .SingleInstance();

            builder
                .RegisterType<AffiliateEventProcessor>()
                .As<IAffiliateEventProcessor>()
                .SingleInstance();

            builder
                .RegisterType<AffiliateReportingLogger>()
                .As<IAffiliateReportingLogger>()
                .SingleInstance();


            /*
             We need to do some extra work here since this runs in the background with no http context
            so we cant lean on that for the user.
             */
            Lazy<NTechSelfRefreshingBearerToken> affiliateReportingSystemUser = new Lazy<NTechSelfRefreshingBearerToken>(() =>
                NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(
                    NEnv.ServiceRegistry, NEnv.ApplicationAutomationUsernameAndPassword));
            builder
                .Register(x =>
                {
                    var e = NEnv.EncryptionKeys;
                    var clock = CoreClock.SharedInstance;
                    var user = affiliateReportingSystemUser.Value.GetUserMetadata();
                    return new AffiliateReportingService(
                        clock,
                        () => new PreCreditContextExtended(user, clock),
                        new EncryptionService(e.CurrentKeyName, e.AsDictionary(), clock, user),
                        NEnv.EnvSettings,
                        LegacyServiceClientFactory.CreateCreditClient(
                            new LegacyHttpServiceBearerTokenUser(affiliateReportingSystemUser), NEnv.ServiceRegistry));
                })
                .As<IAffiliateReportingService>()
                .InstancePerLifetimeScope();
        }
    }
}