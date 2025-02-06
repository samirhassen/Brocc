using nCredit.Code.Treasury;
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
    public class ApiTreasuryAmlExportController : NController
    {
        [Route("Api/TreasuryAml/Export")]
        [HttpPost]
        public ActionResult CreateExport(IDictionary<string, string> schedulerData)
        {
            var uniqueLockIdentifier = "treasuryamlexport";
            return CreditContext.RunWithExclusiveLock(uniqueLockIdentifier,
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
            using (var context = new CreditContext())
            {
                var baseResult = context
                    .OutgoingAmlMonitoringExportFileHeaders
                    .AsQueryable();

                baseResult = baseResult.Where(x => x.ProviderName == "Treasury");

                if (filter?.FromDate != null)
                {
                    var fd = filter.FromDate.Value.Date;
                    baseResult = baseResult.Where(x => x.TransactionDate >= fd);
                }

                if (filter?.ToDate != null)
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
            public string DeliveredToProfileName { get; set; }
            public int? TimeInMs { get; set; }
        }

        private static DeliveryResult DeliverExportFile(string archiveKey, List<string> errors, IDocumentClient dc, string exportFilename, string exportProfileName)
        {
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
            string GetSchedulerData(string s) => (schedulerData != null && schedulerData.ContainsKey(s)) ? schedulerData[s] : null;

            var errors = new List<string>();
            var warnings = new List<string>();
            var w = Stopwatch.StartNew();
            string deliveryArchiveKey = null;
            DeliveryResult deliveryResult = null;
            var dc = Service.DocumentClientHttpContext;
            try
            {
                using (var context = new CreditContext())
                {
                    var file = NTechEnvironment.Instance.StaticResourceFile("ntech.credit.Treasury.settingsfile", "Treasury-business-credit-settings.txt", true);

                    var f = NTechSimpleSettings.ParseSimpleSettingsFile(file.FullName, forceFileExistance: true);

                    var deliveryDate = DateTimeOffset.Now;
                    var fileSuffix = deliveryDate.ToString("yyyy-MM-dd") + ".csv";
                    var skipDeliveryExport = GetSchedulerData("skipDeliveryExport") == "true";
                    var clock = Clock;

                    var model = TreasuryDomainModel.GetTreasuryDomainModel(clock, GetCurrentUserMetadata());
                    var deliveryArchiveKeyList = new List<string[]>();
                    var fileFormat = new TreasuryFileFormat();

                    if (model.TransactionsConsumerLoanCashFlow?.Count > 0)
                    {
                        fileFormat.WithTemporaryExportFileCashFlowConsumerLoans(model, deliveryDate.Date,
                            tempFileName =>
                            {
                                var filename = "BACS-ConsumerLoanCashflows-" + fileSuffix;
                                deliveryArchiveKey = dc.ArchiveStoreFile(new FileInfo(tempFileName), "text/csv", filename);
                                string[] arr = { deliveryArchiveKey, f.Opt("TreasuryAmlExportProfileNameTransactionsCredits") };
                                deliveryArchiveKeyList.Add(arr);
                            });
                    }

                    if (model.TransactionsCompanyLoanCashFlow?.Count > 0)
                    {
                        fileFormat.WithTemporaryExportFileCashFlowCompanyLoans(model, deliveryDate.Date,
                            tempFileName =>
                            {
                                var filename = "BACS-CompanyLoanCashflows-" + fileSuffix;
                                deliveryArchiveKey = dc.ArchiveStoreFile(new FileInfo(tempFileName), "text/csv", filename);

                                string[] arr = { deliveryArchiveKey, f.Opt("TreasuryAmlExportProfileNameCashFlow") };
                                deliveryArchiveKeyList.Add(arr);
                            });
                    }

                    if (model.TransactionsConsumerLoans?.Count > 0)
                    {
                        fileFormat.WithTemporaryExportFileConsumer(model, deliveryDate.Date,
                            tempFileName =>
                            {
                                var filename = "BACS-ConsumerLoans-" + fileSuffix;
                                deliveryArchiveKey = dc.ArchiveStoreFile(new FileInfo(tempFileName), "text/csv", filename);

                                string[] arr = { deliveryArchiveKey, f.Opt("TreasuryAmlExportProfileNameConsumerLoan") };
                                deliveryArchiveKeyList.Add(arr);
                            });
                    }

                    if (model.TransactionsCorporateLoans?.Count > 0)
                    {
                        fileFormat.WithTemporaryExportFileCorporateLoan(model, deliveryDate.Date,
                            tempFileName =>
                            {
                                var filename = "BACS-CompanyLoans-" + fileSuffix;
                                deliveryArchiveKey = dc.ArchiveStoreFile(new FileInfo(tempFileName), "text/csv", filename);

                                string[] arr = { deliveryArchiveKey, f.Opt("TreasuryAmlExportProfileNameCorporateLoan") };
                                deliveryArchiveKeyList.Add(arr);
                            });
                    }

                    if (model.GurantorsCorporateLoans?.Count > 0)
                    {
                        fileFormat.WithTemporaryExportFileCorporateLoanGurantors(model, deliveryDate.Date,
                            tempFileName =>
                            {
                                var filename = "BACS-CompanyLoanGurantors-" + fileSuffix;
                                deliveryArchiveKey = dc.ArchiveStoreFile(new FileInfo(tempFileName), "text/csv", filename);

                                string[] arr = { deliveryArchiveKey, f.Opt("TreasuryAmlExportProfileNameGurantorsCorporateLoans") };
                                deliveryArchiveKeyList.Add(arr);
                            });
                    }

                    //Deliver

                    foreach (var item in deliveryArchiveKeyList)
                    {
                        //Try to deliver
                        if (!skipDeliveryExport)
                        {
                            deliveryResult = DeliverExportFile(item[0], errors, dc, null, item[1]);
                        }
                        else
                            NLog.Information("Treasury AML delivery skipped due to scheduler setting override");

                        if (deliveryResult?.DeliveredToProfileName == null)
                        {
                            //File was not delivered even though the profile is set to export
                            if (skipDeliveryExport)
                                warnings.Add("File not exported due to schedulerData override");
                            else if (errors.Count == 0)
                                warnings.Add("File not exported but was expected to be");
                        }
                        createOutgoingAmlMonitoringExportFileHeaders(item[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "CreateTreasuryAmlExport crashed");
                errors.Add("CreateTreasuryAmlExport crashed: " + ex.Message);
            }

            void createOutgoingAmlMonitoringExportFileHeaders(string deliveryArchiveKeyLocal)
            {
                using (var context = new CreditContext())
                {
                    context.OutgoingAmlMonitoringExportFileHeaders.Add(new OutgoingAmlMonitoringExportFileHeader
                    {
                        ChangedById = CurrentUserId,
                        ChangedDate = Clock.Now,
                        InformationMetaData = InformationMetadata,
                        FileArchiveKey = deliveryArchiveKeyLocal,
                        ProviderName = "Treasury",
                        ExportResultStatus = JsonConvert.SerializeObject(new OutgoingExportFileHeader.ExportResultStatusStandardModel
                        {
                            status = errors?.Count > 0 ? "Error" : (warnings?.Count > 0 ? "Warning" : "Success"),
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
            }

            return Json2(new { errors, totalMilliseconds = w.ElapsedMilliseconds, warnings });
        }
    }
}