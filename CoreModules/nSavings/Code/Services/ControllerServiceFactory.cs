using nSavings.Code.Services.FinnishCustomsAccounts;
using NTech;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Core.Savings.Shared.Database;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Legacy.Module.Shared.Services;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Globalization;
using System.Web.Mvc;

namespace nSavings.Code.Services
{
    public class ControllerServiceFactory
    {
        private readonly UrlHelper urlHelper;
        private readonly Func<string, string> getUserDisplayNameByUserId;
        private readonly IClock clock;
        private readonly Lazy<INTechWsUrlService> wsUrlService;

        public ControllerServiceFactory(
            UrlHelper urlHelper,
            Func<string, string> getUserDisplayNameByUserId,
            IClock clock,
            Lazy<INTechWsUrlService> wsUrlService)
        {
            this.urlHelper = urlHelper;
            this.getUserDisplayNameByUserId = getUserDisplayNameByUserId;
            this.clock = clock;
            this.wsUrlService = wsUrlService;
        }

        public EncryptionService GetEncryptionService(INTechCurrentUserMetadata currentUser)
        {
            var encryptionKeys = NEnv.EncryptionKeys;
            return new EncryptionService(encryptionKeys.CurrentKeyName, encryptionKeys.AsDictionary(), CoreClock.SharedInstance, currentUser);
        }

        public KeyValueStoreService KeyValueStore(INTechCurrentUserMetadata user) => new KeyValueStoreService(ContextFactory, CoreClock.SharedInstance, user);

        public IYearlySummaryService YearlySummary
        {
            get
            {
                return new YearlySummaryService(
                    () => new SavingsContext(),
                    () => new CustomerClient(),
                    () => new DocumentClient(),
                    clock,
                    CultureInfo.GetCultureInfo(NEnv.ClientCfg.Country.BaseFormattingCulture));
            }
        }

        public INTechWsUrlService WsUrl
        {
            get
            {
                return wsUrlService.Value;
            }
        }

        public IFatcaExportService FatcaExport
        {
            get
            {
                return new FatcaExportService(clock, this.getUserDisplayNameByUserId, this.urlHelper);
            }
        }

        public ICustomerRelationsMergeService CustomerRelationsMerge
        {
            get
            {
                return new CustomerRelationsMergeService(
                    LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry));
            }
        }

        private FinnishCustomsFileFormat CustomsFileFormat(INTechCurrentUserMetadata currentUser) => 
            new FinnishCustomsFileFormat(KeyValueStore(currentUser));

        public FinnishCustomsAccountsService FinnishCustomsAccounts(INTechCurrentUserMetadata currentUser)
        {
            string GetArchiveDocumentUrl(string archiveKey) =>
                archiveKey == null ? null : urlHelper.Action("ArchiveDocument", "ApiArchiveDocument", new { key = archiveKey, setFileDownloadName = true });
            var customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);
            var documentClient = LegacyServiceClientFactory.CreateDocumentClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);
            var settings = new Lazy<NTechSimpleSettingsCore>(() => NEnv.FinnishCustomsAccountsSettings);
            var ws = new FinnishCustomsAccountsWebservice(settings, !NEnv.IsProduction, FinnishCustomsMigrationManagerLegacy.SharedInstance);
            return new FinnishCustomsAccountsService(
                CoreClock.SharedInstance, getUserDisplayNameByUserId, GetArchiveDocumentUrl,
                settings, CustomsFileFormat(currentUser), customerClient, ws, SerilogLoggingService.SharedInstance,
                NEnv.ClientCfgCore, documentClient, FinnishCustomsMigrationManagerLegacy.SharedInstance, ContextFactory);
        }

        public SavingsContextFactory ContextFactory => new SavingsContextFactory(
            () => new SavingsContext(),
            SavingsContext.IsConcurrencyException);
    }
}