﻿using nCredit.Code;
using nCredit.Code.Trapets;
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
    public class ApiTrapetsAmlExportController : NController
    {
        [Route("Api/TrapetsAml/Export")]
        [HttpPost]
        public ActionResult CreateExport(IDictionary<string, string> schedulerData)
        {
            return CreditContext.RunWithExclusiveLock("ntech.scheduledjobs.createtrapetsamlexport",
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
        [Route("Api/TrapetsAml/GetFilesPage")]
        public ActionResult GetFilesPage(int pageSize, Filter filter = null, int pageNr = 0)
        {
            using (var context = new CreditContext())
            {
                var baseResult = context
                    .OutgoingAmlMonitoringExportFileHeaders
                    .AsQueryable();

                if (filter != null && filter.FromDate.HasValue)
                {
                    var fd = filter.FromDate.Value.Date;
                    baseResult = baseResult.Where(x => x.TransactionDate >= fd && (x.ProviderName == "" || x.ProviderName == null));
                }

                if (filter != null && filter.ToDate.HasValue)
                {
                    var td = filter.ToDate.Value.Date;
                    baseResult = baseResult.Where(x => x.TransactionDate <= td && (x.ProviderName == "" || x.ProviderName == null));
                }

                if (filter == null)
                {
                    baseResult = baseResult.Where(x => (x.ProviderName == "" || x.ProviderName == null));
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
            public string DeliveredToProfileName { get; set; }
            public int? TimeInMs { get; set; }
        }

        private static DeliveryResult DeliverExportFile(string archiveKey, List<string> errors, IDocumentClient dc, string exportFilename)
        {
            var exportProfileName = NEnv.TrapetsAmlExportProfileName;
            if (exportProfileName == null)
            {
                return new DeliveryResult { DeliveredToProfileName = null, TimeInMs = null };
            }
            else
            {
                var exportResult = dc.ExportArchiveFile(archiveKey, exportProfileName, filename: exportFilename);

                if (!exportResult.IsSuccess)
                    errors.Add($"Export with profile '{exportProfileName}' failed");

                return new DeliveryResult { TimeInMs = exportResult.TimeInMs, DeliveredToProfileName = exportResult.IsSuccess ? exportProfileName : null };
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
                    var trapetsConfig = NEnv.TrapetsAmlConfig;
                    var deliveryDate = DateTimeOffset.Now;
                    var skipDeliveryExport = getSchedulerData("skipDeliveryExport") == "true";

                    var model = TrapetsDomainModel.GetChangesSinceLastExport(CurrentUserId, InformationMetadata, trapetsConfig);

                    var fileFormat = new TrapetsFileFormat();
                    fileFormat.WithTemporaryExportFile(model, deliveryDate.Date, tempFileName =>
                    {
                        var dc = Service.DocumentClientHttpContext;;
                        var filename = string.Format(trapetsConfig.ExportFileNamePattern, deliveryDate);
                        deliveryArchiveKey = dc.ArchiveStoreFile(new FileInfo(tempFileName), "application/xml", filename);

                        //Update timestamps as soon as the file is commited locally
                        model.UpdateChangeTrackingSystemItems();

                        //Try to deliver
                        if (!skipDeliveryExport)
                        {
                            deliveryResult = DeliverExportFile(deliveryArchiveKey, errors, dc, filename);
                        }
                        else
                            NLog.Information("Trapets AML delivery skipped due to scheduler setting override");

                        if (deliveryResult?.DeliveredToProfileName == null && NEnv.TrapetsAmlExportProfileName != null)
                        {
                            //File was not delivered even though the profile is set to export
                            if (skipDeliveryExport)
                                warnings.Add("File not exported due to schedulerData override");
                            else if (errors.Count == 0)
                                warnings.Add("File not exported but was expected to be");
                        }
                    }, trapetsConfig);
                }
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "CreateTrapetsAmlExport crashed");
                errors.Add("CreateTrapetsAmlExport crashed: " + ex.Message);
            }

            using (var context = new CreditContext())
            {
                context.OutgoingAmlMonitoringExportFileHeaders.Add(new OutgoingAmlMonitoringExportFileHeader
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
                        deliveredToProfileName = deliveryResult?.DeliveredToProfileName,
                        providerName = "trapets"
                    }),
                    TransactionDate = Clock.Today
                });

                context.SaveChanges();
            }

            return Json2(new { errors, totalMilliseconds = w.ElapsedMilliseconds, warnings });
        }
    }
}