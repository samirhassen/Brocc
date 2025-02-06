using Newtonsoft.Json;
using NTech.Core.Credit.Shared.Services.Aml.Cm1;
using NTech.Core.Module;
using NTech.Legacy.Module.Shared;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace nCredit.Controllers
{

    [NTechApi]
    public class ApiCm1AmlExportController : NController
    {
        private CreditCm1AmlExportService CreateExportService()
        {
            var resolver = Service;
            var customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);
            var documentClient = LegacyServiceClientFactory.CreateDocumentClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);

            return new CreditCm1AmlExportService(resolver.ContextFactory, CreateEncryptionService(), GetCurrentUserMetadata(), customerClient,
                new Lazy<NTechSimpleSettingsCore>(() => CreditCm1AmlExportService.GetCm1Settings(NTechEnvironmentLegacy.SharedInstance, true)), 
                    resolver.LoggingService, documentClient);
        }

        [Route("Api/Cm1Aml/Export")]
        [HttpPost]
        public ActionResult CreateExport(IDictionary<string, string> schedulerData) =>
            CreditContext.RunWithExclusiveLock("ntech.scheduledjobs.createcm1amlexport",
                    () =>
                    {
                        var service = CreateExportService();

                        Func<string, string> getSchedulerData = s => (schedulerData != null && schedulerData.ContainsKey(s)) ? schedulerData[s] : null;
                        var skipDeliveryExport = getSchedulerData("skipDeliveryExport") == "true";

                        var exportResult = service.CreateExport(skipDeliveryExport);

                        return Json2(new 
                        { 
                            totalMilliseconds = exportResult.TotalMilliseconds,
                            warnings = exportResult.Warnings,
                            errors = exportResult.Errors
                        });
                    },
                    () => Json2(new { errors = new[] { "Job is already running" } })
            );

        [HttpPost]
        [Route("Api/Cm1Aml/GetFilesPage")]
        public ActionResult GetFilesPage(int pageSize, CreditCm1AmlExportService.Cm1ExportFileFilter filter = null, int pageNr = 0)
        {
            var service = CreateExportService();
            var result = service.GetFilesPage(pageSize, filter, pageNr);

            var currentPage = result.Page
                .Select(x => new
                {
                    x.TransactionDate,
                    ExportResultStatus = JsonConvert.DeserializeObject(x.ExportResultStatus),
                    x.FileArchiveKey,
                    ArchiveDocumentUrl = x.FileArchiveKey == null ? null : Url.Action("ArchiveDocument", "ApiArchiveDocument", new { key = x.FileArchiveKey, setFileDownloadName = true }),
                    x.UserId,
                    UserDisplayName = GetUserDisplayNameByUserId(x.UserId.ToString())
                })
                .ToList();
            
            return Json2(new
            {
                result.CurrentPageNr,
                result.TotalNrOfPages,
                Page = currentPage
            });
        }
    }
}