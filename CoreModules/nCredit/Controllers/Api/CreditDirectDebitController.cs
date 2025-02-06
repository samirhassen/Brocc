using nCredit.Code;
using nCredit.DbModel;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class CreditDirectDebitController : NController
    {
        [HttpPost]
        [Route("Api/Credit/DirectDebit/FetchDetails")]
        public ActionResult FetchDetails(string creditNr, string backTarget = null, bool includeEvents = false)
        {
            if (!NEnv.IsDirectDebitPaymentsEnabled)
                return HttpNotFound();

            var directDebitService = this.Service.GetDirectDebitService(GetCurrentUserMetadata());

            return includeEvents ? Json2(new
            {
                Details = directDebitService.FetchCreditDetails(creditNr, this.GetCurrentUserMetadata(), backTarget),
                Events = directDebitService.FetchEvents(creditNr)
            }) : Json2(new
            {
                Details = directDebitService.FetchCreditDetails(creditNr, this.GetCurrentUserMetadata(), backTarget)
            });
        }

        [HttpPost]
        [Route("Api/Credit/DirectDebit/FetchEvents")]
        public ActionResult FetchEvents(string creditNr)
        {
            if (!NEnv.IsDirectDebitPaymentsEnabled)
                return HttpNotFound();

            return Json2(this.Service.GetDirectDebitService(GetCurrentUserMetadata()).FetchEvents(creditNr));
        }

        [HttpPost]
        [Route("Api/Credit/DirectDebit/UpdateStatus")]
        public ActionResult UpdateStatus(string creditNr, string newStatus, string bankAccountNr, int? bankAccountOwnerApplicantNr)
        {
            if (!NEnv.IsDirectDebitPaymentsEnabled)
                return HttpNotFound();

            if (!this.Service.GetDirectDebitService(GetCurrentUserMetadata()).TryUpdateDirectDebitCheckStatusState(creditNr, newStatus, bankAccountNr, bankAccountOwnerApplicantNr, this.GetCurrentUserMetadata(), out string failedMessage))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            }

            return Json2(new { });
        }

        [HttpPost]
        [Route("Api/Credit/DirectDebit/ScheduleActivation")]
        public ActionResult ScheduleActivation(bool? isChangeActivated, string creditNr, string bankAccountNr, string paymentNr, int applicantNr, int customerId)
        {
            if (!NEnv.IsDirectDebitPaymentsEnabled)
                return HttpNotFound();

            if (!this.Service.GetDirectDebitService(GetCurrentUserMetadata()).TryScheduleDirectDebitActivation(creditNr, bankAccountNr, paymentNr, customerId, this.GetCurrentUserMetadata(), isChangeActivated, out string failedMessage))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            }

            if (isChangeActivated == true)
            {
                if (!this.Service.GetDirectDebitService(GetCurrentUserMetadata()).TryUpdateDirectDebitCheckStatusState(creditNr, "Active", bankAccountNr, applicantNr, this.GetCurrentUserMetadata(), out failedMessage))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
                }

                if (!this.Service.GetDirectDebitService(GetCurrentUserMetadata()).TryRemoveSchedulationDirectDebit(creditNr, paymentNr, this.GetCurrentUserMetadata(), out failedMessage))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
                }
            }

            return Json2(new { });
        }

        [HttpPost]
        [Route("Api/Credit/DirectDebit/ScheduleCancellation")]
        public ActionResult ScheduleCancellation(string creditNr, bool? isChangeActivated, string paymentNr)
        {
            if (!NEnv.IsDirectDebitPaymentsEnabled)
                return HttpNotFound();

            if (!this.Service.GetDirectDebitService(GetCurrentUserMetadata()).TryScheduleDirectDebitCancellation(creditNr, paymentNr, this.GetCurrentUserMetadata(), isChangeActivated, out string failedMessage))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            }


            if (isChangeActivated == true)
            {
                if (!this.Service.GetDirectDebitService(GetCurrentUserMetadata()).TryUpdateDirectDebitCheckStatusState(creditNr, "NotActive", null, null, this.GetCurrentUserMetadata(), out failedMessage))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
                }

                if (!this.Service.GetDirectDebitService(GetCurrentUserMetadata()).TryRemoveSchedulationDirectDebit(creditNr, paymentNr, this.GetCurrentUserMetadata(), out failedMessage))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
                }
            }

            return Json2(new { });
        }

        [HttpPost]
        [Route("Api/Credit/DirectDebit/ScheduleChange")]
        public ActionResult ScheduleChange(string currentStatus, bool? isChangeActivated, string creditNr, string bankAccountNr, string paymentNr, int applicantNr, int customerId)
        {
            if (!NEnv.IsDirectDebitPaymentsEnabled)
                return HttpNotFound();

            var status = currentStatus == "Activation" ? "Active" : "NotActive";


            if (!this.Service.GetDirectDebitService(GetCurrentUserMetadata()).TryScheduleDirectDebitChange(creditNr, bankAccountNr, paymentNr, customerId, this.GetCurrentUserMetadata(), isChangeActivated, currentStatus, out string failedMessage))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            }

            if (isChangeActivated == true)
            {
                if (!this.Service.GetDirectDebitService(GetCurrentUserMetadata()).TryUpdateDirectDebitCheckStatusState(creditNr, status, bankAccountNr, applicantNr, this.GetCurrentUserMetadata(), out failedMessage))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
                }

                if (!this.Service.GetDirectDebitService(GetCurrentUserMetadata()).TryRemoveSchedulationDirectDebit(creditNr, paymentNr, this.GetCurrentUserMetadata(), out failedMessage))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
                }
            }


            return Json2(new { });
        }

        [HttpPost]
        [Route("Api/Credit/DirectDebit/RemoveSchedulation")]
        public ActionResult RemoveSchedulation(string creditNr, string paymentNr)
        {
            if (!NEnv.IsDirectDebitPaymentsEnabled)
                return HttpNotFound();

            if (!this.Service.GetDirectDebitService(GetCurrentUserMetadata()).TryRemoveSchedulationDirectDebit(creditNr, paymentNr, this.GetCurrentUserMetadata(), out string failedMessage))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            }

            return Json2(new { });
        }

        [Route("Api/BankAccount/ValidateNr")]
        [HttpPost]
        public ActionResult ValidateBankAccountNr(string bankAccountNr)
        {
            if (string.IsNullOrWhiteSpace(bankAccountNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing bankAccountNr");
            return Json2(this.Service.BankAccountValidation.ValidateBankAccountNr(bankAccountNr));
        }

        [Route("Api/DirectDebit/CreateOutgoingStatusFile")]
        [HttpPost]
        public ActionResult CreateOutgoingStatusFile(IDictionary<string, string> schedulerData = null)
        {
            Func<string, string> getSchedulerData = s => (schedulerData != null && schedulerData.ContainsKey(s)) ? schedulerData[s] : null;
            var skipDeliveryExport = getSchedulerData("skipDeliveryExport") == "true";

            return CreditContext.RunWithExclusiveLock("ntech.scheduledjobs.directdebit.createoutgoingstatusfile", () =>
            {
                if (!NEnv.IsMortgageLoansEnabled)
                    return HttpNotFound();

                var successCount = 0;
                var failCount = 0;
                List<string> errors = new List<string>();
                List<string> warnings = new List<string>();
                var w = Stopwatch.StartNew();
                try
                {
                    var mgr = new DirectDebitBusinessEventManager(GetCurrentUserMetadata(), new CoreClock(), NEnv.ClientCfgCore, Service.ContextFactory, NEnv.EnvSettings);

                    string failedMessage;
                    List<Tuple<CreditOutgoingDirectDebitItem, string>> skippedItemsWithErrors;
                    int? outgoingDirectDebitStatusChangeFileHeaderId;

                    var services = Service;
                    if (!mgr.TryCreateOutgoingDirectDebitStatusFile(NEnv.AutogiroSettings, services.PaymentAccount.GetIncomingPaymentBankAccountNrRequireBankgiro(),
                        !NEnv.IsProduction, new CreditCustomerClient(), services.DocumentClientHttpContext, skipDeliveryExport, out outgoingDirectDebitStatusChangeFileHeaderId,
                        out failedMessage, out skippedItemsWithErrors))
                    {
                        errors.Add(failedMessage);
                    }
                    else if (outgoingDirectDebitStatusChangeFileHeaderId.HasValue)
                    {
                        using (var context = new CreditContext())
                        {
                            successCount = context.OutgoingDirectDebitStatusChangeFileHeaders.Where(x => x.Id == outgoingDirectDebitStatusChangeFileHeaderId.Value).Select(x => x.CreditOutgoingDirectDebitItems.Count).Single();
                        }
                        failCount = skippedItemsWithErrors.Count;
                        foreach (var c in skippedItemsWithErrors)
                            warnings.Add($"{c.Item1.CreditNr} - {c.Item1.Id} skipped: {c.Item2}");

                        if (skipDeliveryExport)
                        {
                            warnings.Add("Delivery skipped due to scheduler override");
                        }
                    }
                    else
                    {
                        warnings.Add("No items found to export. Skipped creating a file");
                    }
                }
                catch (Exception ex)
                {
                    NLog.Error(ex, "CreateOutgoingDirectDebitStatusFile crashed");
                    return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
                }
                finally
                {
                    w.Stop();
                }
                return Json2(new { successCount, failCount, errors, totalMilliseconds = w.ElapsedMilliseconds, warnings = warnings });
            }, () => new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Job is already running"));
        }

        [HttpPost]
        [Route("Api/DirectDebit/ImportIncomingStatusFile")]
        public ActionResult ImportFile(string fileName, string fileAsDataUrl, bool? overrideDuplicateCheck, bool? overrideClientBgCheck)
        {
            if (fileAsDataUrl == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing fileAsDataUrl");
            }

            string mimeType;
            byte[] binaryData;

            if (!Files.TryParseDataUrl(fileAsDataUrl, out mimeType, out binaryData))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid file");
            }

            try
            {
                string failedMessage;
                if (!this.Service.GetDirectDebitService(GetCurrentUserMetadata()).TryImportIncomingStatusFile(fileName, binaryData, mimeType, overrideClientBgCheck, overrideClientBgCheck, this.GetCurrentUserMetadata(), out failedMessage))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
                }
                return Json2(new { });
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Incoming payment file could not be imported");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [HttpPost]
        [Route("Api/DirectDebit/ImportAllIncomingStatusFilesInSourceFolder")]
        public ActionResult ImportAllIncomingStatusFilesInSourceFolder(IDictionary<string, string> schedulerData = null)
        {
            Func<string, string> getSchedulerData = s => (schedulerData != null && schedulerData.ContainsKey(s)) ? schedulerData[s] : null;
            var overrideDuplicateCheck = getSchedulerData("overrideDuplicateCheck") == "true";
            var overrideClientBgCheck = getSchedulerData("overrideClientBgCheck") == "true";

            return CreditContext.RunWithExclusiveLock("ntech.scheduledjobs.directdebit.importincomingstatusfiles", () =>
            {
                if (!NEnv.IsMortgageLoansEnabled)
                    return HttpNotFound();

                var successCount = 0;
                var failCount = 0;
                List<string> errors = new List<string>();
                List<string> warnings = new List<string>();
                var w = Stopwatch.StartNew();
                try
                {
                    var sourceFolder = NEnv.AutogiroSettings.IncomingStatusFileImportFolder;
                    if (Directory.Exists(sourceFolder))
                    {
                        var files = Directory.GetFiles(sourceFolder);
                        if (files.Length > 0)
                        {
                            var handledDirectory = Path.Combine(sourceFolder, "handled");
                            Directory.CreateDirectory(handledDirectory);
                            foreach (var file in files)
                            {
                                var targetFile = Files.MoveFileToFolder(file, handledDirectory);
                                var filename = Path.GetFileName(targetFile);
                                var fileBytes = System.IO.File.ReadAllBytes(targetFile);
                                string failedMessage;
                                if (!this.Service.GetDirectDebitService(GetCurrentUserMetadata()).TryImportIncomingStatusFile(filename, fileBytes, "text/plain", overrideDuplicateCheck, overrideClientBgCheck, GetCurrentUserMetadata(), out failedMessage))
                                {
                                    errors.Add($"Failed to import '{targetFile}': {failedMessage}");
                                    failCount++;
                                }
                                else
                                {
                                    successCount++;
                                }
                            }
                        }
                        if (successCount == 0 && !warnings.Any() && !errors.Any())
                            warnings.Add("No files to import");
                    }
                }
                catch (Exception ex)
                {
                    NLog.Error(ex, "ImportAllIncomingStatusFilesInSourceFolder crashed");
                    return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
                }
                finally
                {
                    w.Stop();
                }
                return Json2(new { successCount, failCount, errors, totalMilliseconds = w.ElapsedMilliseconds, warnings = warnings });
            }, () => new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Job is already running"));
        }
    }
}

public class DirectDebitConsentFile
{
    public int? DocumentId { get; set; }
    public string Filename { get; set; }
    public string DocumentArchiveKey { get; set; }
    public DateTimeOffset? DocumentDate { get; set; }
}