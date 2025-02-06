using nCredit;
using nCredit.Code.Cm1;
using nCredit.DbModel.Model;
using Newtonsoft.Json;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services.Aml.Cm1
{
    public class CreditCm1AmlExportService
    {
        private readonly CreditContextFactory contextFactory;
        private readonly EncryptionService encryptionService;
        private readonly INTechCurrentUserMetadata currentUser;
        private readonly ICustomerClient customerClient;
        private readonly Lazy<NTechSimpleSettingsCore> cm1Settings;
        private readonly ILoggingService loggingService;
        private readonly IDocumentClient documentClient;

        public CreditCm1AmlExportService(CreditContextFactory contextFactory, EncryptionService encryptionService, INTechCurrentUserMetadata currentUser,
            ICustomerClient customerClient, Lazy<NTechSimpleSettingsCore> cm1Settings, ILoggingService loggingService,
            IDocumentClient documentClient)
        {
            this.contextFactory = contextFactory;
            this.encryptionService = encryptionService;
            this.currentUser = currentUser;
            this.customerClient = customerClient;
            this.cm1Settings = cm1Settings;
            this.loggingService = loggingService;
            this.documentClient = documentClient;
        }

        public class CreditCm1ExportResult
        {
            public List<string> Errors { get; set; }
            public long TotalMilliseconds { get; set; }
            public List<string> Warnings { get; set; }
        }

        public CreditCm1ExportResult CreateExport(bool skipDeliveryExport)
        {
            List<string> errors = null;
            List<string> warnings = null;
            var w = Stopwatch.StartNew();
            try
            {
                using (var context = contextFactory.CreateContext())
                {
                    var deliveryDate = DateTimeOffset.Now;

                    var model = CreditCm1DomainModel.GetChangesSinceLastExport(currentUser, contextFactory, encryptionService);

                    var cmlExportFileResponse = customerClient.CreateCm1AmlExportFiles(new PerProductCmlExportFileRequest
                    {
                        Transactions = model.Transactions,
                        Credits = true
                    });

                    var f = cm1Settings.Value;

                    List<OutgoingAmlMonitoringExportFileHeader> transactionFiles = new List<OutgoingAmlMonitoringExportFileHeader>();
                    OutgoingAmlMonitoringExportFileHeader customerFile = null;

                    /*
                     Save export files
                     */
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
                    var deliveryResult = DeliverCm1Files(context, transactionFiles, customerFile, skipDeliveryExport, f);
                    errors = deliveryResult.Errors;
                    warnings = deliveryResult.Warnings;
                }
            }
            catch (Exception ex)
            {
                if (DisableErrorSupression)
                    throw;

                loggingService.Error(ex, "CreateCm1AmlExport crashed");
                errors.Add("CreateCm1AmlExport crashed: " + ex.Message);
            }

            return new CreditCm1ExportResult
            {
                Errors = errors,
                TotalMilliseconds = w.ElapsedMilliseconds,
                Warnings = warnings
            };
        }

        private (List<string> Errors, List<string> Warnings) DeliverCm1Files(ICreditContextExtended context, List<OutgoingAmlMonitoringExportFileHeader> transactionFiles, 
            OutgoingAmlMonitoringExportFileHeader customerFile, bool skipDeliveryExport, NTechSimpleSettingsCore f)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            void DeliverFile(OutgoingAmlMonitoringExportFileHeader file, string profileName)
            {
                LocalDeliveryResult deliveryResult = null;
                var fileErrors = new List<string>();
                var fileWarnings = new List<string>();

                //Try to deliver
                if (!skipDeliveryExport)
                {
                    deliveryResult = DeliverExportFile(file.FileArchiveKey, fileErrors, documentClient, null, profileName);
                }
                else
                    loggingService.Information("Cm1 AML delivery skipped due to scheduler setting override");

                if (deliveryResult?.DeliveredToProfileName == null)
                {
                    //File was not delivered even though the profile is set to export
                    if (skipDeliveryExport)
                        fileWarnings.Add("File not exported due to schedulerData override");
                    else
                        fileWarnings.Add("File not exported but was expected to be");
                }

                file.ExportResultStatus = JsonConvert.SerializeObject(new OutgoingExportFileHeader.ExportResultStatusStandardModel
                {
                    status = fileErrors.Count > 0 ? "Error" : fileWarnings.Count > 0 ? "Warning" : "Success",
                    errors = fileErrors,
                    warnings = fileWarnings,
                    deliveryTimeInMs = deliveryResult?.TimeInMs,
                    deliveredToProfileName = deliveryResult?.DeliveredToProfileName,
                    providerName = "Cm1"
                });

                context.SaveChanges();

                errors.AddRange(fileErrors);
                warnings.AddRange(fileWarnings);
            }

            var profiles = GetCm1ExportProfiles(f);

            var creditsExportProfile = profiles.TransactionsExportProfile;;
            if(creditsExportProfile != null)
            {
                foreach (var transactionFile in transactionFiles)
                {
                    DeliverFile(transactionFile, creditsExportProfile);
                }
            }

            var customerExportProfile = profiles.CustomerExportProfile;;
            if (customerExportProfile != null && customerFile != null)
                DeliverFile(customerFile, customerExportProfile);

            return (Errors: errors, Warnings: warnings);
        }

        public static (string CustomerExportProfile, string TransactionsExportProfile) GetExportProfiles(INTechEnvironment env)
        {
            var cm1Settings = GetCm1Settings(env, false);
            var profiles = GetCm1ExportProfiles(cm1Settings);
            return (CustomerExportProfile: profiles.CustomerExportProfile, TransactionsExportProfile: profiles.TransactionsExportProfile);
        }

        public static NTechSimpleSettingsCore GetCm1Settings(INTechEnvironment env, bool isRequired)
        {
            var f = env.StaticResourceFile("ntech.credit.cm1.settingsfile", "cm1-business-credit-settings.txt", isRequired);
            if (!f.Exists)
                return null;
            return NTechSimpleSettingsCore.ParseSimpleSettingsFile(f.FullName, forceFileExistance: isRequired);
        }

        private static (string CustomerExportProfile, string TransactionsExportProfile) GetCm1ExportProfiles(NTechSimpleSettingsCore settings) =>
            (CustomerExportProfile: settings?.Opt("Cm1AmlExportProfileNameCustomersCredits"), TransactionsExportProfile: settings?.Opt("Cm1AmlExportProfileNameTransactionsCredits"));
        

        private OutgoingAmlMonitoringExportFileHeader CreateAndAddExportHeader(ICreditContextExtended context, string archiveKey)
        {
            var header = new OutgoingAmlMonitoringExportFileHeader
            {
                ChangedById = context.CurrentUser.UserId,
                ChangedDate = context.CoreClock.Now,
                InformationMetaData = context.CurrentUser.InformationMetadata,
                FileArchiveKey = archiveKey,
                ProviderName = "Cm1",
                ExportResultStatus = JsonConvert.SerializeObject(new OutgoingExportFileHeader.ExportResultStatusStandardModel
                {
                    status = "NotExported",
                    providerName = "Cm1"
                }),
                TransactionDate = context.CoreClock.Today
            };
            context.AddOutgoingAmlMonitoringExportFileHeaders(header);
            return header;
        }

        //Used to simplify testing
        public static bool DisableErrorSupression { get; set; }

        private class LocalDeliveryResult
        {
            public string DeliveredToProfileName { get; set; }
            public int? TimeInMs { get; set; }
        }

        private LocalDeliveryResult DeliverExportFile(string archiveKey, List<string> errors, IDocumentClient dc, string exportFilename, string exportProfileName)
        {
            if (exportProfileName == null)
            {
                return new LocalDeliveryResult { DeliveredToProfileName = null, TimeInMs = null };
            }
            else
            {
                var exportResult = dc.ExportArchiveFile(archiveKey, exportProfileName, exportFilename);

                if (!exportResult.IsSuccess)
                    errors.Add($"Export with profile '{exportProfileName}' failed");

                return new LocalDeliveryResult { TimeInMs = exportResult.TimeInMs, DeliveredToProfileName = exportResult.IsSuccess ? exportProfileName : null };
            }
        }

        public class Cm1ExportFileFilter
        {
            public DateTime? FromDate { get; set; }
            public DateTime? ToDate { get; set; }
        }

        public Cm1ExportFilesResult GetFilesPage(int pageSize, Cm1ExportFileFilter filter, int pageNr)
        {
            using (var context = contextFactory.CreateContext())
            {
                var baseResult = context
                    .OutgoingAmlMonitoringExportFileHeadersQueryable;

                if (filter != null && filter.FromDate.HasValue)
                {
                    var fd = filter.FromDate.Value.Date;
                    baseResult = baseResult.Where(x => x.TransactionDate >= fd && x.ProviderName == "Cm1");
                }

                if (filter != null && filter.ToDate.HasValue)
                {
                    var td = filter.ToDate.Value.Date;
                    baseResult = baseResult.Where(x => x.TransactionDate <= td && x.ProviderName == "Cm1");
                }

                if (filter == null)
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
                    .Select(x => new Cm1ExportFilesResultItem
                    {
                        TransactionDate = x.TransactionDate,
                        ExportResultStatus = x.ExportResultStatus,
                        FileArchiveKey = x.FileArchiveKey,
                        UserId = x.UserId
                    })
                    .ToList();

                var nrOfPages = totalCount / pageSize + (totalCount % pageSize == 0 ? 0 : 1);

                return new Cm1ExportFilesResult
                {
                    CurrentPageNr = pageNr,
                    TotalNrOfPages = nrOfPages,
                    Page = currentPage.ToList()
                };
            }
        }

        public class Cm1ExportFilesResult
        {
            public int CurrentPageNr { get; set; }
            public int TotalNrOfPages { get; set; }
            public List<Cm1ExportFilesResultItem> Page { get; set; }
        }

        public class Cm1ExportFilesResultItem
        {
            public DateTime TransactionDate { get; set; }
            public string ExportResultStatus { get; set; }
            public string FileArchiveKey { get; set; }
            public int UserId { get; set; }
        }
    }
}