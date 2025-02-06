using Newtonsoft.Json;
using nSavings.Code;
using nSavings.Code.Cm1;
using NTech.Core.Module;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;

namespace nSavings.Controllers
{
    [NTechApi]
    public class ApiCm1AmlExportController : NController
    {
        [Route("Api/Cm1Aml/Export")]
        [HttpPost]
        public ActionResult CreateExport(IDictionary<string, string> schedulerData)
        {
            return SavingsContext.RunWithExclusiveLock("ntech.scheduledjobs.createcm1amlexport",
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
        [Route("Api/Cm1Aml/GetFilesPage")]
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
                    baseResult = baseResult.Where(x => x.TransactionDate >= fd);
                }

                if (filter != null && filter.ToDate.HasValue)
                {
                    var td = filter.ToDate.Value.Date;
                    baseResult = baseResult.Where(x => x.TransactionDate <= td);
                }
                baseResult = baseResult.Where(x => x.ProviderName == "Cm1");

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

        private static DeliveryResult DeliverExportFile(string archiveKey, List<string> errors, DocumentClient dc, string exportFilename, string exportProfileName)
        {
            if (exportProfileName == null)
            {
                return new DeliveryResult { DeliveredToProfileName = null, TimeInMs = null };
            }
            else
            {
                int timeInMs;
                List<string> successProfileNames;
                List<string> failedProfileNames;
                var isSuccess = dc.TryExportArchiveFile(archiveKey, exportProfileName, out successProfileNames, out failedProfileNames, out timeInMs, filename: exportFilename);

                if (!isSuccess)
                    errors.Add($"Export with profile '{exportProfileName}' failed");

                return new DeliveryResult { TimeInMs = timeInMs, DeliveredToProfileName = isSuccess ? exportProfileName : null };
            }
        }

        private ActionResult CreateExportI(IDictionary<string, string> schedulerData)
        {
            Func<string, string> getSchedulerData = s => (schedulerData != null && schedulerData.ContainsKey(s)) ? schedulerData[s] : null;

            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();
            var w = Stopwatch.StartNew();
            try
            {
                using (var context = new SavingsContext())
                {
                    var deliveryDate = DateTimeOffset.Now;
                    var skipDeliveryExport = getSchedulerData("skipDeliveryExport") == "true";
                    var resolver = Service;
                    var file = NTechEnvironment.Instance.StaticResourceFile("ntech.credit.cm1.settingsfile", "cm1-business-credit-settings.txt", false);
                    if (!file.Exists)
                        throw new System.InvalidOperationException("The configuration file ntech.credit.cm1.settingsfile is missing!");
                    var f = NTechSimpleSettingsCore.ParseSimpleSettingsFile(file.FullName, forceFileExistance: true);

                    var customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);
                    var model = SavingsCm1DomainModel.GetChangesSinceLastExport(CurrentUserId, InformationMetadata, resolver.ContextFactory, 
                        new Lazy<NTechSimpleSettingsCore>(() => f),
                        customerClient,
                        resolver.GetEncryptionService(GetCurrentUserMetadata()));
                    var cmlExportFileResponse = customerClient.CreateCm1AmlExportFiles(new NTech.Core.Module.Shared.Clients.PerProductCmlExportFileRequest
                    {
                        Savings = true,
                        Transactions = model.Transactions
                    });

                    List<OutgoingAmlMonitoringExportFileHeader> transactionFiles = new List<OutgoingAmlMonitoringExportFileHeader>();
                    OutgoingAmlMonitoringExportFileHeader customerFile = null;

                    foreach (var archiveKey in cmlExportFileResponse.TransactionFileArchiveKeys ?? new List<string>())
                    {
                        transactionFiles.Add(CreateAndAddExportHeader(context, archiveKey));
                    }
                    if (!string.IsNullOrWhiteSpace(cmlExportFileResponse.CustomerFileArchiveKey))
                    {
                        customerFile = CreateAndAddExportHeader(context, cmlExportFileResponse.CustomerFileArchiveKey);
                    }
                    model.UpdateChangeTrackingSystemItems(context);

                    context.SaveChanges();

                    //Try to export
                    var dc = new DocumentClient();
                    var deliveryResult = DeliverCm1Files(context, transactionFiles, customerFile, skipDeliveryExport, f, dc);
                    errors = deliveryResult.Errors;
                    warnings = deliveryResult.Warnings;
                }
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "CreateCm1AmlExport crashed");
                errors.Add("CreateCm1AmlExport crashed: " + ex.Message);
            }

            return Json2(new { errors, totalMilliseconds = w.ElapsedMilliseconds, warnings });
        }

        private OutgoingAmlMonitoringExportFileHeader CreateAndAddExportHeader(SavingsContext context, string archiveKey)
        {
            var header = new OutgoingAmlMonitoringExportFileHeader
            {
                ChangedById = CurrentUserId,
                ChangedDate = Clock.Now,
                InformationMetaData = InformationMetadata,
                FileArchiveKey = archiveKey,
                ProviderName = "Cm1",
                ExportResultStatus = JsonConvert.SerializeObject(new
                {
                    status = "NotExported",
                    providerName = "Cm1"
                }),
                TransactionDate = Clock.Today
            };
            context.OutgoingAmlMonitoringExportFileHeaders.Add(header);
            return header;
        }

        private (List<string> Errors, List<string> Warnings) DeliverCm1Files(SavingsContext context, List<OutgoingAmlMonitoringExportFileHeader> transactionFiles,
                    OutgoingAmlMonitoringExportFileHeader customerFile, bool skipDeliveryExport, NTechSimpleSettingsCore f, DocumentClient documentClient)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            void DeliverFile(OutgoingAmlMonitoringExportFileHeader file, string profileName)
            {
                DeliveryResult deliveryResult = null;
                var fileErrors = new List<string>();
                var fileWarnings = new List<string>();

                //Try to deliver
                if (!skipDeliveryExport)
                {
                    deliveryResult = DeliverExportFile(file.FileArchiveKey, fileErrors, documentClient, null, profileName);
                }
                else
                    NLog.Information("Cm1 AML delivery skipped due to scheduler setting override");

                if (deliveryResult?.DeliveredToProfileName == null)
                {
                    //File was not delivered even though the profile is set to export
                    if (skipDeliveryExport)
                        fileWarnings.Add("File not exported due to schedulerData override");
                    else
                        fileWarnings.Add("File not exported but was expected to be");
                }

                file.ExportResultStatus = JsonConvert.SerializeObject(new
                {
                    status = errors?.Count > 0 ? "Error" : (warnings?.Count > 0 ? "Warning" : "Success"),
                    errors = errors,
                    warnings = warnings,
                    deliveryTimeInMs = deliveryResult?.TimeInMs,
                    deliveredToProfileName = deliveryResult?.DeliveredToProfileName,
                    providerName = "Cm1"
                });

                context.SaveChanges();

                errors.AddRange(fileErrors);
                warnings.AddRange(fileWarnings);
            }

            var transactionsExportProfile = f.Opt("Cm1AmlExportProfileNameTransactionsSavings");
            if (transactionsExportProfile != null)
            {
                foreach (var transactionFile in transactionFiles)
                {
                    DeliverFile(transactionFile, transactionsExportProfile);
                }
            }

            var customerExportProfile = f.Opt("Cm1AmlExportProfileNameCustomersSavings");
            if (customerExportProfile != null && customerFile != null)
                DeliverFile(customerFile, customerExportProfile);

            return (Errors: errors, Warnings: warnings);
        }
    }
}