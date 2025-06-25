using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using Newtonsoft.Json;
using nSavings.Code;
using nSavings.Code.Treasury;
using nSavings.DbModel;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Services.Infrastructure;
using Serilog;

namespace nSavings.Controllers.Api
{
    [NTechApi]
    public class ApiTreasuryAmlExportController : NController
    {
        [Route("Api/TreasurySavingsAccount/Export")]
        [HttpPost]
        public ActionResult CreateExport(IDictionary<string, string> schedulerData)
        {
            return SavingsContext.RunWithExclusiveLock("ntech.scheduledjobs.createtreasurysavingsamlexport",
                () => CreateExportI(schedulerData),
                () => Json2(new { errors = new[] { "Job is already running" } })
            );
        }

        public class Filter
        {
            public DateTime? FromDate { get; set; }
            public DateTime? ToDate { get; set; }
        }

        [HttpPost]
        [Route("Api/TreasuryAml/GetFilesPage")]
        public ActionResult GetFilesPage(int pageSize, Filter filter = null, int pageNr = 0)
        {
            using (var context = new SavingsContext())
            {
                var baseResult = context
                    .OutgoingAmlMonitoringExportFileHeaders
                    .AsQueryable();

                if (filter != null && filter.FromDate.HasValue)
                {
                    var fd = filter.FromDate.Value.Date;
                    baseResult = baseResult.Where(x => x.TransactionDate >= fd && x.ProviderName == "Treasury");
                }

                if (filter != null && filter.ToDate.HasValue)
                {
                    var td = filter.ToDate.Value.Date;
                    baseResult = baseResult.Where(x => x.TransactionDate <= td && x.ProviderName == "Treasury");
                }

                if (filter == null)
                    baseResult = baseResult.Where(x => x.ProviderName == "Treasury");

                var totalCount = baseResult.Count();
                var currentPage = baseResult
                    .OrderByDescending(x => x.Timestamp)
                    .Skip(pageSize * pageNr)
                    .Take(pageSize)
                    .ToList()
                    .Select(x => new
                    {
                        x.TransactionDate,
                        x.FileArchiveKey,
                        x.ExportResultStatus,
                        UserId = x.ChangedById
                    })
                    .ToList()
                    .Select(x => new
                    {
                        x.TransactionDate,
                        ExportResultStatus = JsonConvert.DeserializeObject(x.ExportResultStatus),
                        x.FileArchiveKey,
                        ArchiveDocumentUrl = x.FileArchiveKey == null
                            ? null
                            : Url.Action("ArchiveDocument", "ApiArchiveDocument",
                                new { key = x.FileArchiveKey, setFileDownloadName = true }),
                        x.UserId,
                        UserDisplayName = GetUserDisplayNameByUserId(x.UserId.ToString())
                    })
                    .ToList();

                var nrOfPages = (totalCount / pageSize) + (totalCount % pageSize == 0 ? 0 : 1);

                return Json2(new
                {
                    CurrentPageNr = pageNr,
                    TotalNrOfPages = nrOfPages,
                    Page = currentPage.ToList()
                });
            }
        }

        private class DeliveryResult
        {
            public string DeliveredToProfileName { get; set; }
            public int? TimeInMs { get; set; }
        }

        private static DeliveryResult DeliverExportFile(string archiveKey, List<string> errors, DocumentClient dc,
            string exportFilename, string exportProfileName)
        {
            if (exportProfileName == null)
            {
                return new DeliveryResult { DeliveredToProfileName = null, TimeInMs = null };
            }

            var isSuccess = dc.TryExportArchiveFile(archiveKey, exportProfileName, out _,
                out _, out var timeInMs, filename: exportFilename);

            if (!isSuccess)
                errors.Add($"Export with profile '{exportProfileName}' failed");

            return new DeliveryResult
                { TimeInMs = timeInMs, DeliveredToProfileName = isSuccess ? exportProfileName : null };
        }

        private ActionResult CreateExportI(IDictionary<string, string> schedulerData)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            var w = Stopwatch.StartNew();
            string deliveryArchiveKey = null;
            DeliveryResult deliveryResult = null;
            try
            {
                using (var context = new SavingsContext())
                {
                    var file = NTechEnvironment.Instance.StaticResourceFile("ntech.credit.Treasury.settingsfile",
                        "Treasury-business-credit-settings.txt", true);

                    var f = NTechSimpleSettings.ParseSimpleSettingsFile(file.FullName, forceFileExistance: true);

                    if (string.IsNullOrWhiteSpace(f.Opt("TreasuryAmlExportProfileNameAccountSavings")))
                        throw new InvalidOperationException(
                            "TreasuryAmlExportProfileNameAccountSavings is missing in " + file.FullName);

                    var deliveryDate = DateTimeOffset.Now;
                    var skipDeliveryExport = GetSchedulerData("skipDeliveryExport") == "true";
                    var clock = Clock;

                    var model = TreasuryDomainModel.GetChangesSinceLastExport(CurrentUserId, InformationMetadata,
                        clock);

                    var fileFormat = new TreasuryFileFormat();
                    fileFormat.WithTemporaryExportFile(model, deliveryDate.Date, tempFileName =>
                    {
                        var dc = new DocumentClient();
                        var filename = "BACS-SavingsAccounts-" + deliveryDate.ToString("yyyy-MM-dd") + ".csv";
                        deliveryArchiveKey = dc.ArchiveStoreFile(new FileInfo(tempFileName), "text/csv", filename);

                        //Try to deliver
                        if (!skipDeliveryExport)
                        {
                            deliveryResult = DeliverExportFile(deliveryArchiveKey, errors, dc, null,
                                f.Req("TreasuryAmlExportProfileNameAccountSavings"));
                        }
                        else
                            NLog.Information("Treasury Account AML delivery skipped due to scheduler setting override");

                        if (deliveryResult?.DeliveredToProfileName == null)
                        {
                            //File was not delivered even though the profile is set to export
                            if (skipDeliveryExport)
                                warnings.Add("File not exported due to schedulerData override");
                            else if (errors.Count == 0)
                                warnings.Add("File not exported but was expected to be");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "CreateTreasuryAmlExport crashed");
                errors.Add("CreateTreasuryAmlExport crashed: " + ex.Message);
            }

            using (var context = new SavingsContext())
            {
                context.OutgoingAmlMonitoringExportFileHeaders.Add(new OutgoingAmlMonitoringExportFileHeader
                {
                    ChangedById = CurrentUserId,
                    ChangedDate = Clock.Now,
                    InformationMetaData = InformationMetadata,
                    FileArchiveKey = deliveryArchiveKey,
                    ProviderName = "Treasury",
                    ExportResultStatus = JsonConvert.SerializeObject(new
                    {
                        status = errors.Count > 0 ? "Error" : (warnings?.Count > 0 ? "Warning" : "Success"),
                        errors = errors,
                        warnings = warnings,
                        deliveryTimeInMs = deliveryResult?.TimeInMs,
                        deliveredToProfileName = deliveryResult?.DeliveredToProfileName,
                        providerName = "Treasury"
                    }),
                    TransactionDate = Clock.Today
                });

                context.SaveChanges();
            }

            return Json2(new { errors, totalMilliseconds = w.ElapsedMilliseconds, warnings });

            string GetSchedulerData(string s) =>
                schedulerData != null && schedulerData.TryGetValue(s, out var value) ? value : null;
        }
    }
}