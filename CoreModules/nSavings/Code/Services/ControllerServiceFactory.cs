using System;
using System.Globalization;
using System.Web.Mvc;
using nSavings.Code.Services.FinnishCustomsAccounts;
using nSavings.DbModel;
using NTech;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Core.Savings.Shared.Database;
using NTech.Core.Savings.Shared.Services;
using NTech.Core.Savings.Shared.Services.FinnishCustomsAccounts;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Legacy.Module.Shared.Services;
using NTech.Services.Infrastructure.NTechWs;

namespace nSavings.Code.Services;

public class ControllerServiceFactory(
    UrlHelper urlHelper,
    Func<string, string> getUserDisplayNameByUserId,
    IClock clock,
    Lazy<INTechWsUrlService> wsUrlService)
{
    public static EncryptionService GetEncryptionService(INTechCurrentUserMetadata currentUser)
    {
        var encryptionKeys = NEnv.EncryptionKeys;
        return new EncryptionService(encryptionKeys.CurrentKeyName, encryptionKeys.AsDictionary(),
            CoreClock.SharedInstance, currentUser);
    }

    public KeyValueStoreService KeyValueStore(INTechCurrentUserMetadata user) =>
        new(ContextFactory, CoreClock.SharedInstance, user);

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

    public INTechWsUrlService WsUrl => wsUrlService.Value;

    public IFatcaExportService FatcaExport =>
        new FatcaExportService(clock, getUserDisplayNameByUserId, urlHelper);

    public static ICustomerRelationsMergeService CustomerRelationsMerge =>
        new CustomerRelationsMergeService(
            LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance,
                NEnv.ServiceRegistry));

    private FinnishCustomsFileFormat CustomsFileFormat(INTechCurrentUserMetadata currentUser) =>
        new FinnishCustomsFileFormat(KeyValueStore(currentUser));

    public FinnishCustomsAccountsService FinnishCustomsAccounts(INTechCurrentUserMetadata currentUser)
    {
        var customerClient =
            LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance,
                NEnv.ServiceRegistry);
        var documentClient =
            LegacyServiceClientFactory.CreateDocumentClient(LegacyHttpServiceSystemUser.SharedInstance,
                NEnv.ServiceRegistry);
        var settings = new Lazy<NTechSimpleSettingsCore>(() => NEnv.FinnishCustomsAccountsSettings);
        var ws = new FinnishCustomsAccountsWebservice(settings, !NEnv.IsProduction,
            FinnishCustomsMigrationManagerLegacy.SharedInstance);
        return new FinnishCustomsAccountsService(
            CoreClock.SharedInstance, getUserDisplayNameByUserId, GetArchiveDocumentUrl,
            settings, CustomsFileFormat(currentUser), customerClient, ws, SerilogLoggingService.SharedInstance,
            NEnv.ClientCfgCore, documentClient, FinnishCustomsMigrationManagerLegacy.SharedInstance,
            ContextFactory);

        string GetArchiveDocumentUrl(string archiveKey) =>
            archiveKey == null
                ? null
                : urlHelper.Action("ArchiveDocument", "ApiArchiveDocument",
                    new { key = archiveKey, setFileDownloadName = true });
    }

    public SavingsContextFactory ContextFactory => new(
        () => new SavingsContext(),
        SavingsContext.IsConcurrencyException);
}