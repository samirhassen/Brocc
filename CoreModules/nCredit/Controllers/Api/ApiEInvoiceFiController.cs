using nCredit.Code;
using nCredit.Code.EInvoiceFi;
using nCredit.DbModel.BusinessEvents;
using nCredit.DomainModel;
using NTech.Core.Credit.Shared.Database;
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
    [RoutePrefix("Api/EInvoiceFi")]
    [NTechApi]
    public class ApiEInvoiceFiController : NController
    {
        [Route("FetchCreditState")]
        [HttpPost]
        public ActionResult FetchCreditState(string creditNr, bool includeHistory = false, bool includeCreditStatus = false, bool includeLeaveInQueueItems = false)
        {
            var mgr = Service.EInvoiceFi;
            using (var context = CreateCreditContext())
            {
                var state = mgr.GetEInvoiceStateForSingleCredit(context, creditNr);
                string creditStatus = null;
                if (includeCreditStatus)
                {
                    var m = CreditDomainModel.PreFetchForSingleCredit(creditNr, context, NEnv.EnvSettings);
                    creditStatus = m.GetStatus().ToString();
                }

                return Json2(new
                {
                    creditNr,
                    creditStatus,
                    state,
                    history = includeHistory ? GetHistory(creditNr, context, mgr, includeLeaveInQueueItems) : null
                });
            }
        }

        [Route("StartEInvoice")]
        [HttpPost]
        public ActionResult StartEInvoice(string creditNr, string eInvoiceAddress, string eInvoiceBankCode, bool includeHistory = false, bool includeLeaveInQueueItems = false)
        {
            var mgr = Service.EInvoiceFi;
            using (var context = CreateCreditContext())
            {
                string failedMessage;
                if (!mgr.TryStartManually(context, creditNr, eInvoiceAddress, eInvoiceBankCode, out failedMessage))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
                }
                context.SaveChanges();
                var state = mgr.GetEInvoiceStateForSingleCredit(context, creditNr);

                return Json2(new
                {
                    creditNr,
                    state,
                    history = includeHistory ? GetHistory(creditNr, context, mgr, includeLeaveInQueueItems) : null
                });
            }
        }

        [Route("StopEInvoice")]
        [HttpPost]
        public ActionResult StopEInvoice(string creditNr, bool includeHistory = false, bool includeLeaveInQueueItems = false)
        {
            var mgr = Service.EInvoiceFi;
            using (var context = CreateCreditContext())
            {
                string failedMessage;
                if (!mgr.TryStopManually(context, creditNr, out failedMessage))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
                }
                context.SaveChanges();
                var state = mgr.GetEInvoiceStateForSingleCredit(context, creditNr);

                return Json2(new
                {
                    creditNr,
                    state,
                    history = includeHistory ? GetHistory(creditNr, context, mgr, includeLeaveInQueueItems) : null
                });
            }
        }

        private object GetHistory(string creditNr, ICreditContextExtended context, EInvoiceFiBusinessEventManager mgr, bool includeLeaveInQueueItems)
        {
            var items = mgr.GetCreditActionHistoryItems(context, creditNr, includeLeaveInQueueItems);
            return new
            {
                items = items.Select(x => new
                {
                    x.Id,
                    x.ActionName,
                    x.ActionMessage,
                    x.ActionDate,
                    x.CreatedByUserId,
                    CreatedByUserDisplayName = GetUserDisplayNameByUserId(x.CreatedByUserId.ToString()),
                    x.HandledByUserId,
                    HandledByUserDisplayName = x.HandledByUserId.HasValue ? GetUserDisplayNameByUserId(x.HandledByUserId.Value.ToString()) : null,
                    x.HandledDate,
                    x.CreditNr,
                    x.EInvoiceFiMessageHeaderId
                }).ToList()
            };
        }

        [Route("ImportIncomingMessageFile")]
        [HttpPost]
        public ActionResult ImportIncomingMessageFile(string fileName, string fileAsDataUrl, bool processMessages = true)
        {
            if (fileAsDataUrl == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Missing fileAsDataUrl");

            string mimetype;
            byte[] binaryData;

            if (!TryParseDataUrl(fileAsDataUrl, out mimetype, out binaryData))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Invalid file");
            }

            string failedMessage;
            var f = new EInvoiceFiIncomingMessageFileFormat();
            IList<EInvoiceFiIncomingMessageFileFormat.Message> messages;
            if (!f.TryParseFile(new MemoryStream(binaryData), out messages, out failedMessage))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            }

            var mgr = Service.EInvoiceFi;

            mgr.ImportMessages(messages);

            EInvoiceFiBusinessEventManager.ProcessMessagesResult processResult = null;
            if (processMessages)
            {
                processResult = mgr.ProcessMessages();
            }

            return Json2(new
            {
                messageCount = messages.Count,
                processResult = processResult == null ? null : new
                {
                    processedCountTotal = processResult.ProcessedMessageCount,
                    processedCountByCode = processResult.CountByActionCode.Select(x => new { code = x.Key, count = x.Value }).ToList()
                }
            });
        }

        [Route("FetchActionDetails")]
        [HttpPost]
        public ActionResult FetchActionDetails(int actionId)
        {
            var mgr = Service.EInvoiceFi;
            using (var context = Service.ContextFactory.CreateContext())
            {
                var details = mgr.GetActionItemDetails(context, actionId);
                return Json2(details);
            }
        }

        [Route("FetchErrorListActionItems")]
        [HttpPost]
        public ActionResult FetchErrorListActionItems(bool isHandled, int pageNr, int pageSize, bool isOrderedByHandledDate)
        {
            var mgr = Service.EInvoiceFi;
            using (var context = Service.ContextFactory.CreateContext())
            {
                var result = mgr.GetActionErrorListItems(context, isHandled, pageNr, pageSize, isOrderedByHandledDate);
                return Json2(new
                {
                    pageItems = result.Item1,
                    totalCount = result.Item2
                });
            }
        }

        [Route("MarkActionAsHandled")]
        [HttpPost]
        public ActionResult MarkActionAsHandled(int actionId)
        {
            var mgr = Service.EInvoiceFi;
            using (var context = Service.ContextFactory.CreateContext())
            {
                var wasHandled = mgr.TryMarkActionAsHandled(context, actionId);
                context.SaveChanges();
                return Json2(new
                {
                    wasHandled
                });
            }
        }

        [Route("ImportAndRemoveMessageFilesFromFtp")]
        [HttpPost]
        public ActionResult ImportAndRemoveMessageFilesFromFtp(IDictionary<string, string> schedulerData, bool processMessages = true)
        {
            if (!NEnv.IsEInvoiceFiEnabled)
                return HttpNotFound();

            return RunScheduledJob("ntech.scheduledjobs.einvoicefiprocessmessages", w =>
            {
                List<string> warnings = new List<string>();

                var settings = NEnv.EInvoiceFiSettingsFile;
                if (settings.Protocol != "sftp")
                    throw new Exception($"Protcol '{settings.Protocol}' not supported");

                var importer = EInvoiceFiSftpMessageFileImporter.Create(settings.Host, settings.Username, settings.Password, settings.Port, settings.SkipRecentlyWrittenMinutes);
                var parser = new EInvoiceFiIncomingMessageFileFormat();
                var dc = Service.DocumentClientHttpContext;
                var mgr = Service.EInvoiceFi;

                importer.ImportAndRemoveFiles(settings.RemoteDirectory, settings.RemoteFilenamePattern, (fileData, filename) =>
                    {
                        //Parse the file
                        IList<EInvoiceFiIncomingMessageFileFormat.Message> messages;
                        string failedMessage;
                        if (!parser.TryParseFile(fileData, out messages, out failedMessage))
                        {
                            Log.Warning($"EInvoiceFI: Skipped invalid file '{filename}' on the ftp server: {failedMessage}");
                            warnings.Add($"Skipped invalid file '{filename}' on the ftp server. Reason: {failedMessage}");
                            return false;
                        }

                        //Store the original file in the archive
                        var archiveKey = dc.ArchiveStore(fileData.ToArray(), "application/xml", filename);

                        //Log the messages
                        using (var context = new CreditContext())
                        {
                            mgr.ImportMessages(messages, sourceFileArchiveKey: archiveKey);
                            context.SaveChanges();
                        }

                        return true;
                    });

                using (var context = new CreditContext())
                {
                    processMessages = processMessages || (schedulerData?.Opt("eInvoiceFiAlsoProcessMessages") == "true");
                    if (processMessages)
                    {
                        var result = mgr.ProcessMessages();
                        var errorListCount = result.CountByActionCode?.OptS(EInvoiceFiMessageHandler.MessageAction.ErrorList.ToString());
                        if (errorListCount.GetValueOrDefault() > 0)
                        {
                            warnings.Add($"{errorListCount.GetValueOrDefault()} messages ended up on the error list");
                        }
                    }

                    return Json2(new
                    {
                        warnings,
                        totalMilliseconds = w.ElapsedMilliseconds,
                    });
                }
            });
        }

        [Route("ProcessMessages")]
        [HttpPost]
        public ActionResult ProcessMessages(IDictionary<string, string> schedulerData)
        {
            if (!NEnv.IsEInvoiceFiEnabled)
                return HttpNotFound();

            return RunScheduledJob("ntech.scheduledjobs.einvoicefiprocessmessages", w =>
            {
                var mgr = Service.EInvoiceFi;
                List<string> warnings = new List<string>();
                using (var context = new CreditContext())
                {
                    var result = mgr.ProcessMessages();
                    var errorListCount = result.CountByActionCode?.OptS(EInvoiceFiMessageHandler.MessageAction.ErrorList.ToString());
                    if (errorListCount.GetValueOrDefault() > 0)
                    {
                        warnings.Add($"{errorListCount.GetValueOrDefault()} messages ended up on the error list");
                    }

                    return Json2(new
                    {
                        warnings,
                        totalMilliseconds = w.ElapsedMilliseconds,
                    });
                }
            });
        }

        private ActionResult RunScheduledJob(string mutexKey, Func<Stopwatch, ActionResult> run)
        {
            var w = Stopwatch.StartNew();
            try
            {
                return CreditContext.RunWithExclusiveLock(mutexKey,
                        () => run(w),
                        () => Json2(new { errors = new[] { "Job is already running" } })
                );
            }
            catch (Exception ex)
            {
                Log.Error(ex, mutexKey);
                return Json2(new
                {
                    errors = new List<string> { "The job crashed, see errorlog for details" }
                });
            }
            finally
            {
                w.Stop();
            }
        }
    }
}