using nSavings.Code;
using nSavings.DbModel.BusinessEvents;
using NTech.Banking.IncomingPaymentFiles;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nSavings.Controllers
{
    [NTechApi]
    public class ApiIncomingPaymentsController : NController
    {
        public class IncomingPaymentFileWithOriginalExtended : IncomingPaymentFileWithOriginal
        {
            public IncomingPaymentFileFormat_Camt_054_001_02.ExtendedData Data { get; set; }
        }

        private bool TryParse(byte[] fileData, string fileName, bool skipOutgoingPayments, Action<Exception> logError, out IncomingPaymentFileWithOriginalExtended pf, out string errorMessage)
        {
            var f = new IncomingPaymentFileFormat_Camt_054_001_02(logError, skipOutgoingPayments: skipOutgoingPayments);

            IncomingPaymentFileFormat_Camt_054_001_02.ExtendedData extendedData;
            if (f.TryParseExtended(fileData, out pf, out errorMessage, out extendedData))
            {
                pf.OriginalFileData = fileData;
                pf.OriginalFileName = fileName;
                pf.Data = extendedData;
                return true;
            }
            return false;
        }

        [HttpPost]
        [Route("Api/IncomingPayments/GetFileData")]
        public ActionResult GetFileData(string fileFormatName, string fileName, string fileAsDataUrl)
        {
            if (fileFormatName != new IncomingPaymentFileFormat_Camt_054_001_02(null).FileFormatName)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Fileformat '{fileFormatName}' not supported");

            if (fileName == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Missing fileName");

            if (fileAsDataUrl == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Missing fileAsDataUrl");

            string mimetype;
            byte[] binaryData;

            if (!TryParseDataUrl(fileAsDataUrl, out mimetype, out binaryData))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Invalid file");
            }

            if (!new IncomingPaymentFileFormat_Camt_054_001_02(null).MightBeAValidFile(binaryData))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Invalid '{fileFormatName}'-file");
            }

            IncomingPaymentFileWithOriginalExtended file;
            string errorMessage;
            if (!TryParse(binaryData, fileName, true, null, out file, out errorMessage))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Invalid '{fileFormatName}'-file");
            }
            file.OriginalFileName = fileName;
            file.OriginalFileData = binaryData;

            var currencies = file.Accounts.Select(x => x.Currency).Distinct();
            if (currencies.Count() > 1)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"File contains payments in multiple currencies which is not supported");
            }

            if (currencies.Single() != "EUR")
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"File contains non euro payments which is not supported");
            }

            var allPayments = file.Accounts.SelectMany(x => x.DateBatches).SelectMany(x => x.Payments);

            using (var context = new SavingsContext())
            {
                var includedIbans = file.Accounts.Select(x => x.AccountNr.NormalizedValue).ToList();
                var expectedIban = NEnv.DepositsIban.NormalizedValue;
                return Json2(new
                {
                    hasBeenImported = context.IncomingPaymentFileHeaders.Any(x => x.ExternalId == file.ExternalId),
                    fileCreationDate = file.ExternalCreationDate,
                    externalId = file.ExternalId,
                    ibans = string.Join(", ", includedIbans),
                    expectedIban = expectedIban,
                    hasUnexpectedIbans = includedIbans.Any(x => x != expectedIban),
                    outgoingPayments = file.Data.NrOfSkippedOutgoingPayments > 0 ? new
                    {
                        nrOfSkippedOutgoingPayments = file.Data.NrOfSkippedOutgoingPayments,
                        amountOfSkippedOutgoingPayments = file.Data.AmountOfSkippedOutgoingPayments
                    } : null,
                    totalPaymentCount = allPayments.Count(),
                    totalPaymentSum = allPayments.Sum(x => (decimal?)x.Amount) ?? 0m
                });
            }
        }

        [HttpPost]
        [Route("Api/IncomingPayments/ImportFile")]
        public ActionResult ImportFile(string fileFormatName, string fileName, string fileAsDataUrl, bool? autoPlace, bool? overrideDuplicateCheck, bool? overrideIbanCheck, bool? skipOutgoingPayments)
        {
            if (fileFormatName != new IncomingPaymentFileFormat_Camt_054_001_02(null).FileFormatName)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Fileformat '{fileFormatName}' not supported");

            if (fileName == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Missing fileName");

            if (fileAsDataUrl == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Missing fileAsDataUrl");

            using (var context = new SavingsContext())
            {
                context.Configuration.AutoDetectChangesEnabled = false;
                using (var tx = context.Database.BeginTransaction())
                {
                    try
                    {
                        string mimetype;
                        byte[] binaryData;

                        if (!TryParseDataUrl(fileAsDataUrl, out mimetype, out binaryData))
                        {
                            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Invalid file");
                        }

                        IncomingPaymentFileWithOriginalExtended file;
                        string errorMessage;
                        if (!TryParse(binaryData, fileName, (skipOutgoingPayments ?? false), ex => NLog.Error(ex, "Api/IncomingPayments/ImportFile: file could not be parsed. {fileName}, {fileFormatName}", fileName, fileFormatName), out file, out errorMessage))
                        {
                            return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
                        }

                        if (!overrideDuplicateCheck.HasValue || !overrideDuplicateCheck.Value)
                        {
                            if (context.IncomingPaymentFileHeaders.Any(x => x.ExternalId == file.ExternalId))
                            {
                                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "File has already been imported. Override with overrideDuplicateCheck.");
                            }
                        }

                        if (!overrideIbanCheck.GetValueOrDefault())
                        {
                            var otherIbans = file
                                .Accounts
                                .Where(x => x.AccountNr.NormalizedValue != NEnv.DepositsIban.NormalizedValue)
                                .Select(x => x.AccountNr.NormalizedValue)
                                .Distinct()
                                .ToList();
                            if (otherIbans.Any())
                            {
                                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "File has payments to unexpected ibans. Override with overrideIbanCheck.");
                            }
                        }

                        var futureBookKeepingDateDates = file
                            .Accounts
                            .SelectMany(x => x.DateBatches.Select(y => y.BookKeepingDate))
                            .Where(x => x > Clock.Today)
                            .Distinct()
                            .ToList();
                        if (futureBookKeepingDateDates.Any())
                        {
                            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Invalid payment file. There are payments for future dates: {string.Join(", ", futureBookKeepingDateDates.Select(x => x.ToString("yyyy-MM-dd")))}");
                        }

                        var resolver = Service;
                        var currentUser = GetCurrentUserMetadata();
                        var customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceHttpContextUser.SharedInstance, NEnv.ServiceRegistry);
                        var mgr = new NewIncomingPaymentFileBusinessEventManager(currentUser, CoreClock.SharedInstance, NEnv.ClientCfgCore, NEnv.EnvSettings,
                            resolver.GetEncryptionService(currentUser),  resolver.ContextFactory, customerClient);
                        var documentClient = LegacyServiceClientFactory.CreateDocumentClient(LegacyHttpServiceHttpContextUser.SharedInstance, NEnv.ServiceRegistry);
                        string message;
                        var createdFile = mgr.ImportIncomingPaymentFile(context, file, documentClient, out message, skipAutoPlace: !(autoPlace ?? true));
                        context.ChangeTracker.DetectChanges();
                        context.SaveChanges();
                        tx.Commit();
                        return Json2(new { message = message });
                    }
                    catch (Exception ex)
                    {
                        NLog.Error(ex, "Incoming payment file could not be imported");
                        return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
                    }
                }
            }
        }
    }
}