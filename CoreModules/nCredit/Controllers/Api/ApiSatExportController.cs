using nCredit.Code;
using nCredit.Code.Sat;
using nCredit.DbModel.Model;
using Newtonsoft.Json;
using NTech.Core.Module.Shared.Clients;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class ApiSatExportController : NController
    {
        [Route("Api/Sat/Export")]
        [HttpPost]
        public ActionResult CreateExport(IDictionary<string, string> schedulerData)
        {
            return CreditContext.RunWithExclusiveLock("ntech.scheduledjobs.createsatexport",
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
        [Route("Api/Sat/GetFilesPage")]
        public ActionResult GetFilesPage(int pageSize, Filter filter = null, int pageNr = 0)
        {
            using (var context = new CreditContext())
            {
                var baseResult = context
                    .OutgoingSatExportFileHeaders
                    .AsQueryable();

                if (filter != null && filter.FromDate.HasValue)
                {
                    var fd = filter.FromDate.Value.Date;
                    baseResult = baseResult.Where(x => x.TransactionDate >= fd);
                }

                if (filter != null && filter.ToDate.HasValue)
                {
                    var td = filter.ToDate.Value.Date;
                    baseResult = baseResult.Where(x => x.TransactionDate <= td);
                }

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
                        ArchiveDocumentUrl = x.FileArchiveKey == null ? null : Url.Action("ArchiveDocument", "ApiArchiveDocument", new { key = x.FileArchiveKey, setFileDownloadName = true }),
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
            public List<string> SuccessProfileNames { get; set; }
            public List<string> FailedProfileNames { get; set; }
            public int? TimeInMs { get; set; }
        }

        private static DeliveryResult DeliverExportFile(string archiveKey, List<string> errors, IDocumentClient dc)
        {
            var exportProfileName = NEnv.SatExportProfileName;
            if (exportProfileName == null)
            {
                return new DeliveryResult { SuccessProfileNames = new List<string>(), FailedProfileNames = new List<string>(), TimeInMs = null };
            }
            else
            {
                var exportResult = dc.ExportArchiveFile(archiveKey, exportProfileName, null);

                if (!exportResult.IsSuccess)
                    errors.Add($"Export with profile '{exportProfileName}' failed");

                return new DeliveryResult { TimeInMs = exportResult.TimeInMs, SuccessProfileNames = exportResult.SuccessProfileNames, FailedProfileNames = exportResult.FailedProfileNames };
            }
        }

        private ActionResult CreateExportI(IDictionary<string, string> schedulerData)
        {
            Func<string, string> getSchedulerData = s => (schedulerData != null && schedulerData.ContainsKey(s)) ? schedulerData[s] : null;

            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();
            var w = Stopwatch.StartNew();
            string deliveryArchiveKey = null;
            DeliveryResult deliveryResult = null;
            try
            {
                using (var context = new CreditContext())
                {
                    var today = Clock.Today;
                    var satRepo = new SatRepository(() => new CreditCustomerClient());
                    var export = satRepo.GetSatExportItems(context, today);

                    var skipDeliveryExport = getSchedulerData("skipDeliveryExport") == "true";

                    var satFormat = new SatExportFileFormat();
                    satFormat.WithTemporaryExportFile(export, tempFileName =>
                    {
                        var dc = Service.DocumentClientHttpContext;
                        deliveryArchiveKey = dc.ArchiveStoreFile(new FileInfo(tempFileName), "application/xml", "colo.xml");
                        if (!skipDeliveryExport)
                        {
                            deliveryResult = DeliverExportFile(deliveryArchiveKey, errors, dc);
                        }
                        else
                            NLog.Information("SAT delivery skipped due to scheduler setting override");

                        var count = (deliveryResult?.FailedProfileNames?.Count ?? 0) + (deliveryResult?.FailedProfileNames?.Count ?? 0);

                        if (NEnv.SatExportProfileName != null)
                        {
                            if (count == 0 && skipDeliveryExport)
                            {
                                //File was not delivered even though the profile is set to export
                                warnings.Add("File not exported due to schedulerData override");
                            }
                            else if (count > 0 && deliveryResult.FailedProfileNames.Count > 0)
                            {
                                warnings.Add($"Failed profiles: {string.Join(",", deliveryResult.FailedProfileNames)}");
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "CreateSatExport crashed");
                errors.Add("CreateSatExport crashed: " + ex.Message);
            }

            using (var context = new CreditContext())
            {
                context.OutgoingSatExportFileHeaders.Add(new OutgoingSatExportFileHeader
                {
                    ChangedById = CurrentUserId,
                    ChangedDate = Clock.Now,
                    InformationMetaData = InformationMetadata,
                    FileArchiveKey = deliveryArchiveKey,
                    ExportResultStatus = JsonConvert.SerializeObject(new OutgoingExportFileHeader.ExportResultStatusStandardModel
                    {
                        status = errors?.Count > 0 ? "Error" : (warnings?.Count > 0 ? "Warning" : "Success"),
                        errors = errors,
                        warnings = warnings,
                        deliveryTimeInMs = deliveryResult?.TimeInMs,
                        deliveredToProfileName = deliveryResult?.SuccessProfileNames?.FirstOrDefault(),
                        deliveredToProfileNames = deliveryResult?.SuccessProfileNames,
                        failedProfileNames = deliveryResult?.FailedProfileNames
                    }),
                    TransactionDate = Clock.Today
                });
                context.SaveChanges();
            }

            return Json2(new { errors, totalMilliseconds = w.ElapsedMilliseconds, warnings });
        }
    }
}