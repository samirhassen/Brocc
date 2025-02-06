using Microsoft.Extensions.DependencyInjection;
using Moq;
using nCredit;
using nCustomer.Code.Services.Settings;
using NTech.Banking.Conversion;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Customer.Shared;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Customer.Shared.Services.Settings;
using NTech.Core.Host.IntegrationTests.Shared.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Services.Infrastructure.Email;
using System.Globalization;
using System.Text;
using System.Transactions;

namespace NTech.Core.Host.IntegrationTests
{
    public class SupportShared
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        protected SupportShared() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public ICoreClock Clock { get; set; }
        public INTechCurrentUserMetadata CurrentUser { get; set; }
        public Dictionary<string, object> Context { get; set; }
        public ICustomerEnvSettings CustomerEnvSettings { get; set; }
        public IClientConfigurationCore ClientConfiguration { get; set; }
        public EncryptionService EncryptionService { get; set; }
        public ILoggingService LoggingService { get; } = new Mock<ILoggingService>().Object;
        public Mock<INTechEmailService> EmailServiceMock { get; } = new Mock<INTechEmailService>(MockBehavior.Strict);
        public ServiceProvider Services { get; set; }
        public T GetRequiredService<T>() where T : notnull => Services.GetRequiredService<T>();

        public bool IsEmailProviderDown { get; set; }

        public DateTimeOffset Now { get; set; } = new DateTimeOffset(2022, 3, 5, 8, 30, 0, 0, TimeSpan.FromHours(1));
        public Month CurrentMonth => Month.ContainingDate(Now.Date);

        public void AssertDayOfMonth(int dayOfMonth)
        {
            Assert.That(Now.Day, Is.EqualTo(dayOfMonth));
        }

        public Quarter CurrentQuarter => Quarter.ContainingDate(Now.Date);

        public void AssertThatQuarterIs(int quarterNr)
        {
            Assert.That(CurrentQuarter.InYearOrdinalNr, Is.EqualTo(quarterNr));
        }

        public void MoveToNextQuarter() => Now = CurrentQuarter.GetNext().FromDate;

        public void MoveToNextInstanceOfQuarter(int quarterNr)
        {
            var q = CurrentQuarter;
            do
            {
                q = q.GetNext();
            } while (q.InYearOrdinalNr != quarterNr);
            Now = q.FromDate;
        }

        public void MoveToNextDayOfMonth(int dayOfMonth)
        {
            do
            {
                Now = Now.AddDays(1);
            } while (Now.Day != dayOfMonth);

            AssertDayOfMonth(dayOfMonth);
        }

        public void MoveForwardNDays(int nrOfDays)
        {
            Now = Now.AddDays(nrOfDays);
        }

        public CustomerContextFactory CreateCustomerContextFactory() => new CustomerContextFactory(() => new Customer.Database.CustomerContextExtended(CurrentUser, Clock));

        public const string CurrentEncryptionKeyName = "SharedKey20220704";
        public static Dictionary<string, string> EncryptionKeys = new Dictionary<string, string> { ["SharedKey20220704"] = "94538495gh72340f373y873453f" };

        public static T CreateSupport<T>(string clientName, string clientCountry, ICustomerEnvSettings customerEnvSettings, Action<T> populateProductSpecific,
            Action<Mock<IClientConfigurationCore>>? setupClientConfig = null,
            HashSet<string>? activeFeatures = null,
            Action<ServiceCollection>? registerServices = null) where T : SupportShared, new()
        {
            var clock = new Mock<ICoreClock>();
            
            var currentUser = new Mock<INTechCurrentUserMetadata>();
            currentUser.Setup(x => x.InformationMetadata)
                .Returns("{'providerUserId':13,'providerAuthenticationLevel':'UsernamePassword','isSigned':false}".Replace("'", "\""));
            currentUser.Setup(x => x.IsSystemUser).Returns(true);

            var clientConfig = new Mock<IClientConfigurationCore>(MockBehavior.Strict);
            clientConfig.Setup(x => x.ClientName).Returns(clientName);
            clientConfig.Setup(x => x.Country).Returns(GetClientConfigurationCoreCountry(clientCountry));
            clientConfig.Setup(x => x.IsFeatureEnabled(It.IsAny<string>())).Returns((string feature) => activeFeatures?.Contains(feature) ?? false);
            setupClientConfig?.Invoke(clientConfig);
            var encryptionService = new EncryptionService(
                CurrentEncryptionKeyName,
                EncryptionKeys, clock.Object, currentUser.Object);

            var s = new T()
            {
                Clock = clock.Object,
                CurrentUser = currentUser.Object,
                Context = new Dictionary<string, object>(),
                ClientConfiguration = clientConfig.Object,
                EncryptionService = encryptionService,
                CustomerEnvSettings = customerEnvSettings
            };

            s.EmailServiceMock
                .Setup(x => x.SendTemplateEmail(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string>()))
                .Callback(() =>
                {
                    if (s.IsEmailProviderDown)
                        throw new Exception("Emailprovider is down");
                });

            clock.Setup(x => x.Now)
                .Returns(() => s.Now);
            clock.Setup(x => x.Today)
                .Returns(() => s.Now.DateTime.Date);

            populateProductSpecific(s);

            var sc = new ServiceCollection();
            
            SharedServices.Register(sc, s, () => s.Services);

            registerServices?.Invoke(sc);

            s.Services = sc.BuildServiceProvider();

            return s;
        }

        private static ClientConfigurationCoreCountry GetClientConfigurationCoreCountry(string country)
        {
            switch (country)
            {
                case "SE": return new ClientConfigurationCoreCountry { BaseCountry = country, BaseCurrency = "SEK", BaseFormattingCulture = "sv-SE" };
                case "FI": return new ClientConfigurationCoreCountry { BaseCountry = country, BaseCurrency = "EUR", BaseFormattingCulture = "fi-FI" };
                default: throw new NotImplementedException();
            }
        }

        public int GetTestPersonCustomerId(int testPersonNumber) => (int)Context[$"TestPerson{testPersonNumber}_CustomerId"];
        public Dictionary<string, string> GetTestPersonData(int testPersonNumber) => (Dictionary<string, string>)Context[$"TestPerson{testPersonNumber}_Data"];

        public nCustomer.Code.Services.KeyValueStoreService CreateCustomerKeyValueStoreService() =>
            new nCustomer.Code.Services.KeyValueStoreService(() => new Customer.Database.CustomerContextExtended(CurrentUser, Clock), Clock);

        public SettingsService CreateSettingsService() => new SettingsService(new Customer.Shared.Settings.SettingsModelSource(ClientConfiguration),
                CreateCustomerKeyValueStoreService(), CurrentUser, ClientConfiguration, (_, __) => CachedSettingsService.ClearCache());

        public CachedSettingsService CreateCachedSettingsService() =>
            new CachedSettingsService(new BugFixCustomerClientLoadSettingsOnly(
                new DirectDbLoadSettingsOnlyCustomerClient(() => new Customer.Database.CustomerContext(), Clock, ClientConfiguration)));

        public PaymentAccountService CreatePaymentAccountService(ICreditEnvSettings creditEnvSettings) =>
            new PaymentAccountService(CreateCachedSettingsService(), creditEnvSettings, ClientConfiguration);

        public CultureInfo FormattingCulture =>
            NTechCoreFormatting.GetPrintFormattingCulture(ClientConfiguration.Country.BaseFormattingCulture);
        
        public T ParseEmbeddedResource<T>(string fileName, Func<string, T> parse)
        {
            return EmbeddedResources.WithEmbeddedStream("NTech.Core.Host.IntegrationTests.Resources", fileName, stream =>
            {
                using (var sr = new StreamReader(stream, Encoding.UTF8))
                {
                    return parse(sr.ReadToEnd());
                }
            });
        }

        /// <summary>
        /// Dotnetcores TransactionScope goes a bit insane when we use several databases at the same time nested and tries to start a distributed transaction and then fails
        /// so we need to supress the ambient transaction when using the customer db here.
        /// </summary>
        private class BugFixCustomerClientLoadSettingsOnly : ICustomerClientLoadSettingsOnly
        {
            private readonly DirectDbLoadSettingsOnlyCustomerClient client;

            public BugFixCustomerClientLoadSettingsOnly(DirectDbLoadSettingsOnlyCustomerClient client)
            {
                this.client = client;
            }

            public Dictionary<string, string> LoadSettings(string settingCode)
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                {
                    return client.LoadSettings(settingCode);
                }
            }            
        }
    }
}
