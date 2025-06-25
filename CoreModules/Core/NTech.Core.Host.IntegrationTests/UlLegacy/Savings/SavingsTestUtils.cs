using Moq;
using Newtonsoft.Json.Linq;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Savings.Database;
using NTech.Core.Savings.Shared.Database;
using System.Text;
using NTech.Core.Savings.Shared.Services;
using NTech.Core.Savings.Shared.Services.FinnishCustomsAccounts;

namespace NTech.Core.Host.IntegrationTests.UlLegacy.Savings
{
    internal static class SavingsTestUtils
    {
        public static SavingsContextFactory CreateContextFactory() => new SavingsContextFactory(() => new SavingsContext(), SavingsContext.IsConcurrencyException);

        public static List<FinnishCustomsFileFormat.UpdateModel> ExportToCustoms(UlLegacyTestRunner.TestSupport support)
        {
            var exportedModels = new List<FinnishCustomsFileFormat.UpdateModel>();
            var settings = new Lazy<NTechSimpleSettingsCore>(() => NTechSimpleSettingsCore.Create(new Dictionary<string, string>
            {
                ["senderBusinessId"] = "11223344-5"
            }));

            var contextFactory = CreateContextFactory();
            var f = new FinnishCustomsFileFormat(new KeyValueStoreService(contextFactory, support.Clock, support.CurrentUser));
            var m = new Mock<IFinnishCustomsMigrationManager>(MockBehavior.Strict);
            m
                .Setup(x => x.ValidateAndThrowOnError(It.IsAny<FinnishCustomsFileFormat.UpdateModel>()))
                .Callback<FinnishCustomsFileFormat.UpdateModel>(x => exportedModels.Add(x));
            m
                .Setup(x => x.CreateFlatZipFile(It.IsAny<Tuple<string, Stream>[]>()))
                .Returns<Tuple<string, Stream>[]>(x => new MemoryStream(Encoding.UTF8.GetBytes("abc123")));

            var ws = new Mock<IFinnishCustomsAccountsWebservice>(MockBehavior.Strict);
            ws
                .Setup(x => x.TryReportUpdate(It.IsAny<JObject>(), It.IsAny<FinnishCustomsAccountsWebservice.LoggingContextModel>()))
                .Returns(true);
            var documentClient = support.GetRequiredService<IDocumentClient>();
            var s = new FinnishCustomsAccountsService(support.Clock, x => $"user {x}", x => x, settings, f,
                support.GetRequiredService<ICustomerClient>(), ws.Object, support.LoggingService, support.ClientConfiguration,
                documentClient, m.Object, contextFactory);

            s.CreateAndDeliverUpdate(
                support.CurrentUser,
                observeArchiveKey: null,
                skipArchive: false,
                skipDeliver: false,
                observeError: x => throw new Exception(x));

            return exportedModels;
        }
    }
}
