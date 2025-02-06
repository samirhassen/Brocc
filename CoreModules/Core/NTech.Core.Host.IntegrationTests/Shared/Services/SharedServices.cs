using Microsoft.Extensions.DependencyInjection;
using Moq;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Core.PreCredit.Shared.Services.Utilities;
using NTech.Services.Infrastructure.Email;
using Stubble.Core.Builders;

namespace NTech.Core.Host.IntegrationTests.Shared.Services
{
    internal static class SharedServices
    {
        public static void Register(ServiceCollection services, SupportShared support, Func<ServiceProvider> getProvider)
        {
            services.AddSingleton<IServiceClientSyncConverter>(_ => new Mock<IServiceClientSyncConverter>(MockBehavior.Strict).Object);
            services.AddSingleton<INTechEnvironment>(_ => new Mock<INTechEnvironment>(MockBehavior.Strict).Object);
            services.AddTransient(_ => support.ClientConfiguration);
            services.AddTransient(_ => support.EncryptionService);
            services.AddTransient(_ => support.CreateCachedSettingsService());
            services.AddTransient(_ => support.Clock);
            services.AddTransient(_ => support.CurrentUser);
            services.AddTransient(_ => support.LoggingService);
            services.AddTransient(_ => (ILinqQueryExpander)LinqQueryExpanderDoNothing.SharedInstance);            
            services.AddTransient<INTechServiceRegistry>(_ =>
            {
                var m = new Mock<INTechServiceRegistry>(MockBehavior.Strict);
                m.Setup(x => x.ExternalServiceUrl(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Tuple<string, string>[]>()))
                    .Returns(new Uri("http://localhost/some-external"));
                m.Setup(x => x.InternalServiceUrl(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Tuple<string, string>[]>()))
                    .Returns(new Uri("http://localhost/some-internal"));
                return m.Object;
            });

            var stubble = new StubbleBuilder().Build();
            var mustacheRenderer = new Mock<IMustacheTemplateRenderingService>(MockBehavior.Strict);
            mustacheRenderer
                .Setup(x => x.RenderTemplate(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .Returns<string, Dictionary<string, object>>((text, mines) => stubble.Render(text, mines));
            services.AddTransient<IMustacheTemplateRenderingService>(_ => mustacheRenderer.Object);

            services.AddSingleton<INTechEmailServiceFactory>(x =>
            {
                var m = new Mock<INTechEmailServiceFactory>(MockBehavior.Strict);
                m.Setup(x => x.HasEmailProvider).Returns(true);
                m
                    .Setup(x => x.CreateEmailService())
                    .Returns(() => x.GetRequiredService<INTechEmailService>());
                return m.Object;
            });
            services.AddSingleton<INTechEmailService>(x =>
            {
                var m = new Mock<INTechEmailService>(MockBehavior.Strict);
                m
                    .Setup(x => x.LoadClientResourceTemplate(It.IsAny<string>(), It.IsAny<bool>()))
                    .Returns((SubjectTemplateText: "Subject", BodyTemplateText: "Body", IsEnabled: true));
                return m.Object;
            });

            SharedCustomer.RegisterServices(support, services, getProvider);
            SharedPreCredit.RegisterServices(support, services, getProvider);
            var creditSupport = support as ISupportSharedCredit;
            if(creditSupport != null)
                SharedCredit.RegisterServices(creditSupport, services, getProvider);
        }
    }
}
