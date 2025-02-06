using nCredit.DbModel.BusinessEvents;
using nCredit.DomainModel;
using NTech;
using NTech.Banking.Autogiro;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace nCredit.Code.Services
{
    public class DirectDebitNotificationDeliveryService
    {
        private readonly IDocumentClient documentClient;
        private readonly PaymentAccountService paymentAccountService;
        private readonly CreditContextFactory creditContextFactory;
        private readonly ICreditEnvSettings envSettings;
        private readonly PaymentOrderService paymentOrderService;
        private readonly IClientConfigurationCore clientConfiguration;

        public DirectDebitNotificationDeliveryService(IDocumentClient documentClient,
            PaymentAccountService paymentAccountService, CreditContextFactory creditContextFactory, ICreditEnvSettings envSettings,
            PaymentOrderService paymentOrderService, IClientConfigurationCore clientConfiguration)
        {
            this.documentClient = documentClient;
            this.paymentAccountService = paymentAccountService;
            this.creditContextFactory = creditContextFactory;
            this.envSettings = envSettings;
            this.paymentOrderService = paymentOrderService;
            this.clientConfiguration = clientConfiguration;
        }

        private class DirectDebitItemModel
        {
            public string NotficationMasterPdfArchiveKey { get; set; }
            public string NotficationMasterCreditNr { get; set; }
            public decimal NotificationGroupRemainingBalance { get; set; }
            public string OcrReference { get; set; }
            public string DirectDebitPaymentNumber { get; set; }
            public DateTime DueDate { get; set; }
            public List<CreditNotificationHeader> IncludedNotifications { get; set; }
        }

        private List<DirectDebitItemModel> GetDirectDebitItems(ICreditContextExtended context, Action<List<(string CreditNr, string Reason)>> observeSkippedCredits = null)
        {
            var today = context.CoreClock.Today;
            var ns = context
                .CreditNotificationHeadersQueryable
                .Where(x => x.DueDate > today && !x.OutgoingCreditNotificationDeliveryFileHeaderId.HasValue)
                .Select(x => new
                {
                    h = x,
                    Applicant1CustomerId = x.Credit.CreditCustomers.Where(y => y.ApplicantNr == 1).Select(y => (int?)y.CustomerId).FirstOrDefault(),
                    IsCoNotificationDelivered = x.CoNotificationId != null && context
                        .CreditNotificationHeadersQueryable
                        .Any(y => y.Id != x.Id && x.CoNotificationId == y.CoNotificationId && y.OutgoingCreditNotificationDeliveryFileHeaderId.HasValue)
                })
                .ToList()
                .GroupBy(x => x.h.CreditNr) //EF Cores shitty linq parser cant manage groupby hence this hack
                .Select(x => x.OrderByDescending(y => y.h.DueDate).FirstOrDefault())
                .ToList();

            var result = new List<DirectDebitItemModel>();

            if (ns.Count == 0)
                return result;

            string GetDirectDebitSkipReason(CreditNotificationHeader h, bool isCoNotificationDelivered)
            {
                var nrOfDaysUntilDueDate = Dates.GetAbsoluteNrOfDaysBetweenDates(h.DueDate, today);
                if (nrOfDaysUntilDueDate < 3)
                    return $"days until duedate: {nrOfDaysUntilDueDate} < 3";
                if (nrOfDaysUntilDueDate > 8)
                    return $"days until duedate: {nrOfDaysUntilDueDate} > 8";
                if (isCoNotificationDelivered)
                    return "Already delivered on other conotification master";
                return null;
            }

            var filterNs = ns.Select(x => new
            {
                x.h,
                x.Applicant1CustomerId,
                x.IsCoNotificationDelivered,
                skipReason = GetDirectDebitSkipReason(x.h, x.IsCoNotificationDelivered)
            }).ToList();

            observeSkippedCredits?.Invoke(filterNs.Where(x => x.skipReason != null).Select(x => (CreditNr: x.h.CreditNr, Reason: x.skipReason)).ToList());
            ns = filterNs.Where(x => x.skipReason == null).Select(x => new { x.h, x.Applicant1CustomerId, x.IsCoNotificationDelivered }).ToList();

            var creditModels = CreditDomainModel.PreFetchForCredits(context, ns.Select(x => x.h.CreditNr).Distinct().ToArray(), envSettings);
            var notificationModels = CreditNotificationDomainModel.CreateForNotifications(ns.Select(x => x.h.Id).ToList(), context, paymentOrderService.GetPaymentOrderItems());

            //Co notified
            foreach (var coNotificationGroup in ns.Where(x => x.h.CoNotificationId != null).GroupBy(x => x.h.CoNotificationId)) 
            {
                try
                {
                    var master = coNotificationGroup.Single(x => x.h.IsCoNotificationMaster == true);
                    var masterCredit = creditModels[master.h.CreditNr];

                    //NOTE: If we change how direct debit nrs work to make co notification less awkward try to preserve the intent here
                    //      which is that if there are multiple to choose from we pick the one that is most likely to be working
                    var latestActivatedDirectDebitPaymentNumber = coNotificationGroup
                        .Select(x =>
                        {
                            DateTime? transactionDate = null;
                            var directDebitPaymentNumber = creditModels[x.h.CreditNr].GetActiveDirectDebitPaymentNumberOrNull(today, observeTransactionDate: y => transactionDate = y);
                            return new { directDebitPaymentNumber, transactionDate };
                        })
                        .Where(x => x.directDebitPaymentNumber != null && x.transactionDate != null)
                        .OrderByDescending(x => x.transactionDate)
                        .FirstOrDefault()
                        ?.directDebitPaymentNumber;

                    result.Add(new DirectDebitItemModel
                    {
                        NotficationMasterCreditNr = masterCredit.CreditNr,
                        OcrReference = masterCredit.GetDatedCreditString(today, DatedCreditStringCode.SharedOcrPaymentReference, null),
                        NotficationMasterPdfArchiveKey = master.h.PdfArchiveKey,
                        DirectDebitPaymentNumber = latestActivatedDirectDebitPaymentNumber,
                        DueDate = master.h.DueDate,
                        IncludedNotifications = coNotificationGroup.Select(x => x.h).ToList(),
                        NotificationGroupRemainingBalance = coNotificationGroup.Sum(x => notificationModels[x.h.Id].GetRemainingBalance(today))
                    });
                }
                catch (Exception ex)
                {
                    throw new Exception($"Exception in co notification group {coNotificationGroup.Key}", ex);
                }
            }

            //Non co notified
            foreach (var n in ns.Where(x => x.h.CoNotificationId == null))
            {
                var header = n.h;
                var notification = notificationModels[n.h.Id];
                var credit = creditModels[n.h.CreditNr];
                
                result.Add(new DirectDebitItemModel
                {
                    NotficationMasterPdfArchiveKey = header.PdfArchiveKey,
                    NotificationGroupRemainingBalance = notification.GetRemainingBalance(today),
                    DirectDebitPaymentNumber = credit.GetActiveDirectDebitPaymentNumberOrNull(today),
                    OcrReference = notification.OcrPaymentReference,
                    DueDate = notification.DueDate,
                    NotficationMasterCreditNr = header.CreditNr,
                    IncludedNotifications = new List<CreditNotificationHeader> { header }
                });
            }

            return result;
        }

        public (OutgoingCreditNotificationDeliveryFileHeader ExportFile, List<string> SkipList, List<string> Errors) CreateDelivery(
            Action<(DateTime DueDate, decimal Amount, string Ocr)> observePayments = null, bool includeTestingComment = false)
        {
            var errors = new List<string>();
            var skipList = new List<string>();
            (OutgoingCreditNotificationDeliveryFileHeader ExportFile, List<string> SkipList, List<string> Errors) R(OutgoingCreditNotificationDeliveryFileHeader exportFile) =>
                (ExportFile: exportFile, SkipList: skipList, Errors: errors);

            using (var context = creditContextFactory.CreateContext())
            {
                var ns = GetDirectDebitItems(context);

                if (ns.Count == 0)
                    return R(null);

                var serviceBase = new BusinessEventManagerOrServiceBase(context.CurrentUser, context.CoreClock, clientConfiguration);
                
                var f = context.FillInfrastructureFields(new OutgoingCreditNotificationDeliveryFileHeader
                {
                    ExternalId = Guid.NewGuid().ToString(),
                    TransactionDate = context.CoreClock.Today,
                    CreatedByEvent = serviceBase.AddBusinessEvent(BusinessEventType.ScheduledDirectDebitPayment, context)
                });

                var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                var tempZipfile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");
                Directory.CreateDirectory(tempFolder);
                try
                {
                    var agSettings = envSettings.AutogiroSettings;
                    var agFile = new Lazy<AutogiroPaymentFileToBgcBuilder>(() => 
                        AutogiroPaymentFileToBgcBuilder.New(agSettings.GetRequiredCustomerNr(), paymentAccountService.GetIncomingPaymentBankAccountNrRequireBankgiro(), () => context.CoreClock.Now.DateTime, !envSettings.IsProduction));

                    var printCulture = new Lazy<CultureInfo>(() => NTechCoreFormatting.GetPrintFormattingCulture(clientConfiguration.Country.BaseFormattingCulture));

                    foreach (var n in ns)
                    {
                        if (n.NotficationMasterPdfArchiveKey == null)
                        {
                            errors.Add("Missing PdfArchiveKey for credit " + n.NotficationMasterCreditNr);
                        }
                        else
                        {
                            decimal amount = n.NotificationGroupRemainingBalance;
                            string paymentNr = n.DirectDebitPaymentNumber;
                            string ocrNr = n.OcrReference; 

                            if (paymentNr == null)
                            {
                                skipList.Add($"{n.NotficationMasterCreditNr}\tAutgiro is not active");
                            }
                            else if (amount <= 0m)
                            {
                                skipList.Add($"{n.NotficationMasterCreditNr}\tThe notification is already paid");
                            }
                            else
                            {
                                observePayments?.Invoke((DueDate: n.DueDate, Amount: amount, Ocr: ocrNr));
                                agFile.Value.AddPaymentFromCustomerToClient(amount, n.DueDate, paymentNr, ocrNr);
                                foreach(var notification in n.IncludedNotifications)
                                {                                    
                                    notification.DeliveryFile = f;
                                    if (includeTestingComment && (notification.IsCoNotificationMaster == true || !notification.IsCoNotificationMaster.HasValue))
                                    {
                                        //NOTE: Temporary solution to allow tracking afterwards when we scheduled direct debit. We should replace this with real tracking that alsow works for acutal system users
                                        context.AddCreditComment(context.FillInfrastructureFields(new CreditComment
                                        {
                                            CommentText = $"Direct debit payment of {amount:C} scheduled for {n.DueDate}",
                                            Attachment = CreditCommentAttachmentModel.RawDataOnly(new DirectDebitPaymentLogData
                                            {
                                                Amount = amount,
                                                Ocr = ocrNr,
                                                DueDate = notification.DueDate,
                                                IsCoNotificationMaster = notification.IsCoNotificationMaster                                               
                                            }).Serialize(),
                                            CreatedByEvent = f.CreatedByEvent,
                                            CommentDate = context.CoreClock.Now,
                                            CreditNr = notification.CreditNr,
                                            EventType = $"BusinessEvent_{f.CreatedByEvent.EventType}",
                                            CommentById = context.CurrentUser.UserId
                                        }));
                                    }
                                }                                    
                            }
                        }
                    }

                    if(!agFile.IsValueCreated)
                    {
                        //No credits to send
                        return R(null);
                    }

                    var unsealedD = new DirectoryInfo(Path.Combine(tempFolder, "unsealed"));
                    unsealedD.Create();

                    agFile.Value.SaveToFolderWithCorrectFilename(unsealedD);

                    if (agSettings.IsHmacFileSealEnabled)
                    {
                        var sealer = new AutogiroHmacSealer(agSettings.HmacFileSealKey, () => context.CoreClock.Now.DateTime);

                        var sealedD = new DirectoryInfo(Path.Combine(tempFolder, "sealed"));
                        sealedD.Create();

                        agFile.Value.SaveToFolderWithCorrectFilename(sealedD, alsoSealWith: sealer);
                    }

                    if (skipList.Count > 0)
                        File.WriteAllLines(Path.Combine(tempFolder, "skipped.txt"), skipList);

                    var fs = new ICSharpCode.SharpZipLib.Zip.FastZip();

                    fs.CreateZip(tempZipfile, tempFolder, true, null);

                    var filename = $"creditnotification-directdebit_{context.CoreClock.Today.ToString("yyyy-MM-dd")}_{f.ExternalId}.zip";
                    var fileData = File.ReadAllBytes(tempZipfile);
                    f.FileArchiveKey = documentClient.ArchiveStore(
                        fileData,
                        "application/zip",
                        filename);

                    context.SaveChanges();

                    if (agSettings.OutgoingPaymentFileExportProfileName != null)
                    {
                        documentClient.ExportArchiveFile(f.FileArchiveKey, agSettings.OutgoingPaymentFileExportProfileName, filename);
                    }

                    return R(f);
                }
                finally
                {
                    try
                    {
                        Directory.Delete(tempFolder, true);
                        if (System.IO.File.Exists(tempZipfile)) System.IO.File.Delete(tempZipfile);
                    }
                    catch { /* ignored*/ }
                }
            }
        }

        public class DirectDebitPaymentLogData
        {
            public decimal Amount { get; set; }
            public string Ocr { get; set; }
            public DateTime DueDate { get; set; }
            public bool? IsCoNotificationMaster { get; set; }

            public static DirectDebitPaymentLogData FromCommentAttachment(CreditCommentAttachmentModel creditCommentAttachment) =>
                creditCommentAttachment?.ParseRawData<DirectDebitPaymentLogData>();
        }
    }
}