using nCredit.Code.EInvoiceFi;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    public class EInvoiceFiBusinessEventManager : BusinessEventManagerOrServiceBase
    {
        private readonly EncryptionService encryptionService;
        private readonly CreditContextFactory contextFactory;
        private readonly ICreditEnvSettings envSettings;

        public EInvoiceFiBusinessEventManager(INTechCurrentUserMetadata currentUser, ICoreClock clock, IClientConfigurationCore clientConfiguration,
            EncryptionService encryptionService, CreditContextFactory contextFactory, ICreditEnvSettings envSettings) : base(currentUser, clock, clientConfiguration)
        {
            this.encryptionService = encryptionService;
            this.contextFactory = contextFactory;
            this.envSettings = envSettings;
        }

        public const string StartedStatusName = "Started";
        public const string StoppedStatusName = "Stopped";
        

        public class ActionItemDetails
        {
            public int ActionId { get; set; }
            public string ActionMessage { get; set; }

            public bool HasEInvoiceMessage { get; set; }
            public List<Item> EInvoiceMessageItems { get; set; }

            public bool HasConnectedBusinessEvent { get; set; }
            public string ConnectedBusinessEventType { get; set; }
            public List<Item> ConnectedBusinessEventItems { get; set; }

            public class Item
            {
                public string Name { get; set; }
                public string Value { get; set; }
            }
        }

        public class ActionItem
        {
            public int Id { get; set; }
            public string ActionName { get; set; }
            public string ActionMessage { get; set; }
            public DateTime ActionDate { get; set; }
            public int CreatedByUserId { get; set; }
            public int? HandledByUserId { get; set; }
            public DateTime? HandledDate { get; set; }
            public string CreditNr { get; set; }
            public int? EInvoiceFiMessageHeaderId { get; set; }
        }

        public ActionItemDetails GetActionItemDetails(ICreditContextExtended context, int actionId)
        {
            var a = context
                .EInvoiceFiActionsQueryable
                .Where(x => x.Id == actionId)
                .Select(x => new
                {
                    x.Id,
                    Evt = x.ConnectedBusinessEventId.HasValue ? new
                    {
                        x.ConnectBusinessEvent.EventType,
                        Items = x.ConnectBusinessEvent.DatedCreditStrings.Select(y => new ActionItemDetails.Item { Name = y.Name, Value = y.Value })
                    } : null,
                    Msg = x.EInvoiceFiMessageHeaderId.HasValue ? new
                    {
                        x.EInvoiceFiMessage.ExternalMessageType,
                        x.EInvoiceFiMessage.ExternalMessageId,
                        x.EInvoiceFiMessage.ExternalTimestamp,
                        Items = x.EInvoiceFiMessage.Items.Select(y => new { y.Name, y.Value, y.IsEncrypted })
                    } : null,
                    x.ActionMessage
                })
                .Single();

            var result = new ActionItemDetails
            {
                ActionId = a.Id,
                ActionMessage = a.ActionMessage
            };

            if (a.Evt != null)
            {
                result.HasConnectedBusinessEvent = true;
                result.ConnectedBusinessEventType = a.Evt.EventType;
                result.ConnectedBusinessEventItems = a.Evt.Items.ToList();
            }

            if (a.Msg != null)
            {
                var encryptedItemIds = a.Msg.Items.Where(x => x.IsEncrypted).Select(x => long.Parse(x.Value)).ToArray();
                IDictionary<long, string> decryptedValues = null;
                if (encryptedItemIds.Length > 0)
                {
                    decryptedValues = encryptionService.DecryptEncryptedValues(context, encryptedItemIds);
                }
                result.HasEInvoiceMessage = true;
                result.EInvoiceMessageItems = new List<ActionItemDetails.Item>();
                foreach (var i in a.Msg.Items)
                    result.EInvoiceMessageItems.Add(new ActionItemDetails.Item { Name = i.Name, Value = i.IsEncrypted ? decryptedValues[long.Parse(i.Value)] : i.Value });
                result.EInvoiceMessageItems.Add(new ActionItemDetails.Item { Name = "ExternalMessageId", Value = a.Msg.ExternalMessageId });
                result.EInvoiceMessageItems.Add(new ActionItemDetails.Item { Name = "ExternalMessageType", Value = a.Msg.ExternalMessageType });
                result.EInvoiceMessageItems.Add(new ActionItemDetails.Item { Name = "ExternalTimestamp", Value = a.Msg.ExternalTimestamp.ToString("o") });
            }

            return result;
        }

        public bool TryMarkActionAsHandled(ICreditContextExtended context, int actionId)
        {
            var a = context.EInvoiceFiActionsQueryable.SingleOrDefault(x => x.Id == actionId);

            if (a == null)
                return false;
            if (a.HandledByUserId.HasValue)
                return false;

            a.HandledByUserId = this.UserId;
            a.HandledDate = this.Clock.Now.DateTime;

            return true;
        }

        public Tuple<List<ActionItem>, int> GetActionErrorListItems(ICreditContextExtended context, bool isHandled, int pageNr, int pageSize, bool isOrderedByHandledDate)
        {
            return GetActionItemsAndTotalCount(context,
                onlyThisAction: EInvoiceFiMessageHandler.MessageAction.ErrorList,
                isHandled: isHandled,
                pageNrAndPageSize: Tuple.Create(pageNr, pageSize),
                isOrderedByHandledDate: isOrderedByHandledDate);
        }

        public List<ActionItem> GetCreditActionHistoryItems(ICreditContextExtended context, string creditNr, bool includeLeaveInQueueItems = false)
        {
            return GetActionItemsAndTotalCount(context, onlyForCreditNr: creditNr, includeLeaveInQueueItems: includeLeaveInQueueItems).Item1;
        }

        private Tuple<List<ActionItem>, int> GetActionItemsAndTotalCount(ICreditContextExtended context,
            string onlyForCreditNr = null,
            EInvoiceFiMessageHandler.MessageAction? onlyThisAction = null,
            bool includeLeaveInQueueItems = false,
            bool? isHandled = null,
            Tuple<int, int> pageNrAndPageSize = null,
            bool isOrderedByHandledDate = false)
        {
            var q = context
                .EInvoiceFiActionsQueryable;

            if (onlyForCreditNr != null)
                q = q.Where(x => x.CreditNr == onlyForCreditNr);

            if (onlyThisAction.HasValue)
            {
                var an = onlyThisAction.Value.ToString();
                q = q.Where(x => x.ActionName == an);
            }

            if (!includeLeaveInQueueItems)
            {
                var an = EInvoiceFiMessageHandler.MessageAction.LeaveInQueue.ToString();
                q = q.Where(x => x.ActionName != an);
            }

            if (isHandled.HasValue)
            {
                if (isHandled.Value)
                    q = q.Where(x => x.HandledDate.HasValue);
                else
                    q = q.Where(x => !x.HandledDate.HasValue);
            }

            IOrderedQueryable<EInvoiceFiAction> orderedQ;
            if (isOrderedByHandledDate)
                orderedQ = q.OrderByDescending(x => x.HandledDate).ThenByDescending(x => x.Id);
            else
                orderedQ = q.OrderByDescending(x => x.Id);

            var resultBase = orderedQ.AsQueryable();
            if (pageNrAndPageSize != null)
            {
                resultBase = resultBase
                    .Skip(pageNrAndPageSize.Item1 * pageNrAndPageSize.Item2)
                    .Take(pageNrAndPageSize.Item2);
            }

            var resultPage = resultBase
                .Select(x => new ActionItem
                {
                    Id = x.Id,
                    ActionName = x.ActionName,
                    ActionMessage = x.ActionMessage,
                    ActionDate = x.ActionDate,
                    CreatedByUserId = x.CreatedByUserId,
                    HandledByUserId = x.HandledByUserId,
                    HandledDate = x.HandledDate,
                    CreditNr = x.CreditNr,
                    EInvoiceFiMessageHeaderId = x.EInvoiceFiMessageHeaderId
                }).ToList();

            if (pageNrAndPageSize != null)
                return Tuple.Create(resultPage, orderedQ.Count());
            else
                return Tuple.Create(resultPage, resultPage.Count);
        }

        public class EInvoiceState
        {
            public bool IsStarted { get; set; }
            public DateTime? StartedDate { get; set; }
            public string EInvoiceAddress { get; set; }
            public string EInvoiceBankCode { get; set; }
        }

        public void ImportMessages(IList<EInvoiceFiIncomingMessageFileFormat.Message> messages, string sourceFileArchiveKey = null)
        {
            Action<List<EInvoiceFiMessageItem>, EInvoiceFiItemCode, string, Boolean> addItem =
                (list, code, value, isEncrypted) =>
                    list.Add(new EInvoiceFiMessageItem
                    {
                        Name = code.ToString(),
                        Value = value,
                        IsEncrypted = isEncrypted
                    });

            using (var context = contextFactory.CreateContext())
            {
                context.BeginTransaction();
                try
                {
                    var e = AddBusinessEvent(BusinessEventType.ImportedEInvoiceFiMessageFile, context);
                    var importDate = Clock.Now.DateTime;
                    var encryptedItems = new List<EInvoiceFiMessageItem>();
                    foreach (var m in messages)
                    {
                        var items = new List<EInvoiceFiMessageItem>();

                        if (!string.IsNullOrWhiteSpace(m.CustomerAddressArea))
                            addItem(items, EInvoiceFiItemCode.CustomerAddressArea, m.CustomerAddressArea, true);

                        if (!string.IsNullOrWhiteSpace(m.CustomerAddressStreet))
                            addItem(items, EInvoiceFiItemCode.CustomerAddressStreet, m.CustomerAddressStreet, true);

                        if (!string.IsNullOrWhiteSpace(m.CustomerAddressZipcode))
                            addItem(items, EInvoiceFiItemCode.CustomerAddressZipcode, m.CustomerAddressZipcode, true);

                        if (!string.IsNullOrWhiteSpace(m.CustomerIdentification1))
                            addItem(items, EInvoiceFiItemCode.CustomerIdentification1, m.CustomerIdentification1, false);

                        if (!string.IsNullOrWhiteSpace(m.CustomerIdentification2))
                            addItem(items, EInvoiceFiItemCode.CustomerIdentification2, m.CustomerIdentification2, false);

                        if (!string.IsNullOrWhiteSpace(m.CustomerLanguageCode))
                            addItem(items, EInvoiceFiItemCode.CustomerLanguageCode, m.CustomerLanguageCode, false);

                        if (!string.IsNullOrWhiteSpace(m.CustomerName))
                            addItem(items, EInvoiceFiItemCode.CustomerName, m.CustomerName, true);

                        if (!string.IsNullOrWhiteSpace(m.EInvoiceAddress))
                            addItem(items, EInvoiceFiItemCode.EInvoiceAddress, m.EInvoiceAddress, true);

                        if (!string.IsNullOrWhiteSpace(m.EInvoiceBankCode))
                            addItem(items, EInvoiceFiItemCode.EInvoiceBankCode, m.EInvoiceBankCode, true);

                        if (!string.IsNullOrWhiteSpace(m.LastInvoicePaidOcr))
                            addItem(items, EInvoiceFiItemCode.LastInvoicePaidOcr, m.LastInvoicePaidOcr, true);

                        if (!string.IsNullOrWhiteSpace(sourceFileArchiveKey))
                            addItem(items, EInvoiceFiItemCode.SourceFileArchiveKey, sourceFileArchiveKey, false);

                        var h = new EInvoiceFiMessageHeader
                        {
                            CreatedByEvent = e,
                            ExternalMessageId = m.MessageId,
                            ExternalMessageType = m.MessageType,
                            ExternalTimestamp = m.Timestamp,
                            ImportDate = importDate,
                            ImportedByUserId = this.UserId,
                            Items = items
                        };
                        context.AddEInvoiceFiMessageHeaders(h);
                        FillInInfrastructureFields(h);
                        encryptedItems.AddRange(items.Where(x => x.IsEncrypted));
                    }
                    if (encryptedItems.Any())
                    {
                        encryptionService.SaveEncryptItems(encryptedItems.ToArray(), x => x.Value, (x, encId) => x.Value = encId.ToString(), context);
                    }
                    context.SaveChanges();
                    context.CommitTransaction();
                }
                catch
                {
                    context.RollbackTransaction();
                    throw;
                }
            }
        }

        private EInvoiceFiAction AddEInvoiceFiAction(ICreditContextExtended context, EInvoiceFiMessageHandler.MessageAction action, string reasonMessage = null, string creditNr = null, int? eInvoiceFiMessageHeaderId = null, BusinessEvent connectedEvent = null)
        {
            var a = new EInvoiceFiAction
            {
                ActionDate = Clock.Now.DateTime,
                ActionName = action.ToString(),
                ActionMessage = reasonMessage,
                CreditNr = creditNr,
                EInvoiceFiMessageHeaderId = eInvoiceFiMessageHeaderId,
                CreatedByUserId = this.UserId,
                ConnectBusinessEvent = connectedEvent
            };
            FillInInfrastructureFields(a);
            context.AddEInvoiceFiActions(a);
            return a;
        }

        public class ProcessMessagesResult
        {
            public int ProcessedMessageCount { get; set; }
            public Dictionary<string, int> CountByActionCode { get; set; }
        }

        private class MessageModel : IEInvoiceFiMessageHeader
        {
            private IDictionary<string, string> itemsValuesByName;

            public string ExternalMessageType { get; private set; }
            public string ExternalMessageId { get; private set; }
            public DateTime ImportDate { get; private set; }

            public string GetItemValue(EInvoiceFiItemCode itemCode)
            {
                return itemsValuesByName?.Opt(itemCode.ToString());
            }

            public static MessageModel FromMessage(ICreditContextExtended context, EncryptionService encryptionService, EInvoiceFiMessageHeader header, IList<EInvoiceFiMessageItem> items)
            {
                var encryptedItems = items.Where(x => x.IsEncrypted).ToList();
                IDictionary<long, string> decryptedValues = null;
                if (encryptedItems.Any())
                {
                    decryptedValues = encryptionService.DecryptEncryptedValues(
                        context,
                        encryptedItems.Select(x => long.Parse(x.Value)).ToArray());
                }

                var m = new MessageModel();
                m.itemsValuesByName = items.ToDictionary(x => x.Name, x => x.IsEncrypted ? decryptedValues[long.Parse(x.Value)] : x.Value);
                m.ExternalMessageType = header.ExternalMessageType;
                m.ExternalMessageId = header.ExternalMessageId;
                m.ImportDate = header.ImportDate;

                return m;
            }
        }

        public ProcessMessagesResult ProcessMessages()
        {
            var settings = envSettings.EInvoiceFiSettingsFile;

            var processResult = new ProcessMessagesResult
            {
                ProcessedMessageCount = 0,
                CountByActionCode = new Dictionary<string, int>()
            };

            Action<EInvoiceFiMessageHeader> flagAsProcessed = m =>
            {
                m.ProcessedByUserId = this.UserId;
                m.ProcessedDate = Clock.Now.DateTime;
            };

            var messageHandler = new EInvoiceFiMessageHandler();

            Func<bool> process = () =>
            {
                List<int> unprocessedMessageIds;
                HashSet<string> allActiveCreditNrs;

                using (var context = contextFactory.CreateContext())
                {
                    unprocessedMessageIds = context
                        .EInvoiceFiMessageHeadersQueryable
                        .Where(x => !x.ProcessedByUserId.HasValue)
                        .OrderBy(x => x.ExternalTimestamp)
                        .Select(x => x.Id)
                        .ToList();
                    allActiveCreditNrs = new HashSet<string>(GetEInvoiceCreditModels(context)
                        .Where(x => x.CreditStatus == CreditStatus.Normal.ToString())
                        .Select(x => x.CreditNr)
                        .ToList());
                }
                bool isOutOfOrderMessageDetected = false;
                foreach (var id in unprocessedMessageIds)
                {
                    processResult.ProcessedMessageCount += 1;
                    using (var context = contextFactory.CreateContext())
                    {
                        var message2 = context.EInvoiceFiMessageHeadersQueryable.Select(x => new
                        {
                            Header = x,
                            Items = x.Items
                        }).Single(x => x.Header.Id == id);
                        var messageModel = MessageModel.FromMessage(context, encryptionService, message2.Header, message2.Items);
                        var messageRepo = new EInvoiceMessageProcessRepository(allActiveCreditNrs, context, settings.AllowDuplicateMessageIds);
                        var matchResult = messageHandler.MatchMessageToCredit(messageModel, messageRepo);

                        EInvoiceFiMessageHandler.ProcessMessageResult result;
                        if (matchResult.Item1 != null)
                        {
                            var matchedCreditNr = matchResult.Item1;
                            var currentState = GetEInvoiceStateForSingleCredit(context, matchedCreditNr);
                            result = messageHandler.ProcessMatchedMessage(messageModel, matchedCreditNr, currentState, context.CoreClock);
                        }
                        else
                            result = matchResult.Item2;

                        processResult.CountByActionCode[result.Action.ToString()] = (processResult.CountByActionCode.OptS(result.Action.ToString()) ?? 0) + 1;

                        switch (result.Action)
                        {
                            case EInvoiceFiMessageHandler.MessageAction.SkipMessage:
                            case EInvoiceFiMessageHandler.MessageAction.ErrorList:
                                {
                                    AddEInvoiceFiAction(context, result.Action, reasonMessage: result.ActionReasonMessage, creditNr: result.MatchedCreditNr, eInvoiceFiMessageHeaderId: id);
                                    flagAsProcessed(message2.Header);
                                    break;
                                };
                            case EInvoiceFiMessageHandler.MessageAction.LeaveInQueue:
                                {
                                    isOutOfOrderMessageDetected = true;
                                    AddEInvoiceFiAction(context, result.Action, reasonMessage: result.ActionReasonMessage, creditNr: result.MatchedCreditNr, eInvoiceFiMessageHeaderId: id);
                                    break;
                                };
                            case EInvoiceFiMessageHandler.MessageAction.Start:
                                {
                                    string failedMessage;
                                    BusinessEvent eInvoiceEvt;
                                    if (TryStartDirectly(context, result.MatchedCreditNr, result.EInvoiceAddress, result.EInvoiceBankCode, out failedMessage, out eInvoiceEvt))
                                        AddEInvoiceFiAction(context, result.Action, reasonMessage: result.ActionReasonMessage, creditNr: result.MatchedCreditNr, eInvoiceFiMessageHeaderId: id, connectedEvent: eInvoiceEvt);
                                    else
                                        AddEInvoiceFiAction(context, EInvoiceFiMessageHandler.MessageAction.ErrorList, reasonMessage: $"start failed: {failedMessage}", creditNr: result.MatchedCreditNr, eInvoiceFiMessageHeaderId: id);
                                    flagAsProcessed(message2.Header);
                                    break;
                                };
                            case EInvoiceFiMessageHandler.MessageAction.Stop:
                                {
                                    string failedMessage;
                                    BusinessEvent eInvoiceEvt;
                                    if (TryStopDirectly(context, result.MatchedCreditNr, out failedMessage, out eInvoiceEvt))
                                        AddEInvoiceFiAction(context, result.Action, reasonMessage: result.ActionReasonMessage, creditNr: result.MatchedCreditNr, eInvoiceFiMessageHeaderId: id, connectedEvent: eInvoiceEvt);
                                    else
                                        AddEInvoiceFiAction(context, EInvoiceFiMessageHandler.MessageAction.ErrorList, reasonMessage: $"stop failed: {failedMessage}", creditNr: result.MatchedCreditNr, eInvoiceFiMessageHeaderId: id);
                                    flagAsProcessed(message2.Header);
                                    break;
                                };
                            case EInvoiceFiMessageHandler.MessageAction.Change:
                                {
                                    string failedMessage;
                                    BusinessEvent eInvoiceEvt;
                                    if (TryStartDirectly(context, result.MatchedCreditNr, result.EInvoiceAddress, result.EInvoiceBankCode, out failedMessage, out eInvoiceEvt, isChange: true))
                                        AddEInvoiceFiAction(context, result.Action, reasonMessage: result.ActionReasonMessage, creditNr: result.MatchedCreditNr, eInvoiceFiMessageHeaderId: id, connectedEvent: eInvoiceEvt);
                                    else
                                        AddEInvoiceFiAction(context, EInvoiceFiMessageHandler.MessageAction.ErrorList, reasonMessage: $"change failed: {failedMessage}", creditNr: result.MatchedCreditNr, eInvoiceFiMessageHeaderId: id);
                                    flagAsProcessed(message2.Header);
                                    break;
                                };
                            default:
                                throw new NotImplementedException();
                        }
                        context.SaveChanges();
                    }
                }
                return isOutOfOrderMessageDetected;
            };

            //We run twice if the first pass comes across start messages for already started credits
            //since the stop message might be ahead of it and we can then actually deal with the queued start message on pass two
            //This could in theory require as many passes as there are messages but the practical important case of a bank change with
            //the two banks sending their stop and start out of order should be handled by this just fine.

            if (process())
            {
                process();
            }

            return processResult;
        }

        private class EInvoiceMessageProcessRepository : EInvoiceFiMessageHandler.IEInvoiceFiMessageMatchingRepository
        {
            private ICreditContextExtended context;
            private HashSet<string> allActiveCreditNrs;
            private bool allowDuplicateMessageIds;

            public EInvoiceMessageProcessRepository(HashSet<string> allActiveCreditNrs, ICreditContextExtended context, bool allowDuplicateMessageIds)
            {
                this.context = context;
                this.allActiveCreditNrs = allActiveCreditNrs;
                this.allowDuplicateMessageIds = allowDuplicateMessageIds;
            }

            public bool IsDuplicateExternalMessageId(string externalMessageId)
            {
                if (allowDuplicateMessageIds)
                    return false;
                return context
                    .EInvoiceFiMessageHeadersQueryable
                    .Any(x => x.ProcessedByUserId.HasValue && x.ExternalMessageId == externalMessageId);
            }

            public List<string> FilterOutNonActiveCreditNrs(IList<string> creditNrs)
            {
                return creditNrs.Intersect(allActiveCreditNrs).ToList();
            }

            public List<string> GetCreditNrsUsingEInvoiceIdentifiers(string eInvoiceAddress, string eInvoiceBankCode)
            {
                return GetEInvoiceCreditModels(context)
                    .Where(x => x.EInvoiceAddress == eInvoiceAddress && x.EInvoiceFiBankCode == eInvoiceBankCode)
                    .Select(x => x.CreditNr)
                    .ToList();
            }

            public List<string> GetCreditNrsUsingEmail(string email)
            {
                email = email?.Trim() ?? "";
                if (email.Length == 0)
                    return new List<string>();
                throw new Exception("EInvoice does not exist. Will delete in separate commit.");
            }

            public List<string> GetCreditNrsUsingOcr(string ocr)
            {
                ocr = ocr?.Trim() ?? "";
                if (ocr.Length == 0)
                    return new List<string>();
                return GetEInvoiceCreditModels(context)
                    .Where(x => x.CreditOcrPaymentReference == ocr || x.NotificationOcrPaymentReferences.Contains(ocr))
                    .Select(x => x.CreditNr)
                    .ToList();
            }
        }

        public EInvoiceState GetEInvoiceStateForSingleCredit(ICreditContextExtended context, string creditNr)
        {
            return GetEInvoiceStateByCreditNr(context, onlyTheseCreditNrs: new List<string> { creditNr }).Opt(creditNr) ?? new EInvoiceState
            {
                IsStarted = false
            };
        }

        private class EInvoiceCreditModel
        {
            public string CreditNr { get; internal set; }
            public string CreditOcrPaymentReference { get; set; }
            public IEnumerable<int> CustomerIds { get; set; }
            public IEnumerable<string> NotificationOcrPaymentReferences { get; set; }
            public string CreditStatus { get; internal set; }
            public string EInvoiceFiStatus { get; internal set; }
            public DateTime? EInvoiceFiStatusDate { get; internal set; }
            public string EInvoiceFiBankCode { get; internal set; }
            public string EInvoiceAddress { get; internal set; }
        }

        private static IQueryable<EInvoiceCreditModel> GetEInvoiceCreditModels(ICreditContextExtended context)
        {
            return context
                .CreditHeadersQueryable
                .Select(x => new
                {
                    x.CreditNr,
                    CurrentDatedCreditStrings = x
                        .DatedCreditStrings
                        .GroupBy(y => y.Name)
                        .Select(y => y.OrderByDescending(z => z.TransactionDate).ThenByDescending(z => z.Timestamp).FirstOrDefault()),
                    x.Notifications,
                    x.CreditCustomers
                })
                .Select(x => new EInvoiceCreditModel
                {
                    CreditNr = x.CreditNr,
                    CreditOcrPaymentReference = x.CurrentDatedCreditStrings.Where(y => y.Name == DatedCreditStringCode.OcrPaymentReference.ToString()).Select(y => y.Value).FirstOrDefault(),
                    NotificationOcrPaymentReferences = x.Notifications.Where(y => y.OcrPaymentReference != null).Select(y => y.OcrPaymentReference),
                    CustomerIds = x.CreditCustomers.Select(y => y.CustomerId),
                    CreditStatus = x.CurrentDatedCreditStrings.Where(y => y.Name == DatedCreditStringCode.CreditStatus.ToString()).Select(y => y.Value).FirstOrDefault(),
                    EInvoiceFiStatus = x.CurrentDatedCreditStrings.Where(y => y.Name == DatedCreditStringCode.EInvoiceFiStatus.ToString()).Select(y => y.Value).FirstOrDefault(),
                    EInvoiceFiStatusDate = x.CurrentDatedCreditStrings.Where(y => y.Name == DatedCreditStringCode.EInvoiceFiStatus.ToString()).Select(y => (DateTime?)y.TransactionDate).FirstOrDefault(),
                    EInvoiceFiBankCode = x.CurrentDatedCreditStrings.Where(y => y.Name == DatedCreditStringCode.EInvoiceFiBankCode.ToString()).Select(y => y.Value).FirstOrDefault(),
                    EInvoiceAddress = x.CurrentDatedCreditStrings.Where(y => y.Name == DatedCreditStringCode.EInvoiceAddress.ToString()).Select(y => y.Value).FirstOrDefault(),
                });
        }

        /// <summary>
        /// Get the e-invoice state of all active credits
        /// (optionally only a subset)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="onlyTheseCreditNrs"></param>
        /// <returns></returns>        
        public IDictionary<string, EInvoiceState> GetEInvoiceStateByCreditNr(ICreditContextExtended context, IList<string> onlyTheseCreditNrs = null)
        {            
            var c = GetEInvoiceCreditModels(context);

            if (onlyTheseCreditNrs != null)
            {
                c = c.Where(x => onlyTheseCreditNrs.Contains(x.CreditNr));
            }
            return c
                .Where(x => x.CreditStatus == CreditStatus.Normal.ToString())
                .Select(x => new
                {
                    x.CreditNr,
                    x.EInvoiceFiStatus,
                    x.EInvoiceFiStatusDate,
                    x.EInvoiceAddress,
                    x.EInvoiceFiBankCode
                })
                .ToList()
                .ToDictionary(x => x.CreditNr, x =>
                {
                    var isStarted = x.EInvoiceFiStatus == StartedStatusName;
                    return new EInvoiceState
                    {
                        IsStarted = isStarted,
                        StartedDate = isStarted ? x.EInvoiceFiStatusDate : new DateTime?(),
                        EInvoiceAddress = isStarted ? x.EInvoiceAddress : null,
                        EInvoiceBankCode = isStarted ? x.EInvoiceFiBankCode : null
                    };
                });
        }

        private bool TryStartDirectly(ICreditContextExtended context, string creditNr, string eInvoiceAddress, string eInvoiceBankCode, out string failedMessage, out BusinessEvent evt, bool isChange = false)
        {
            eInvoiceAddress = eInvoiceAddress?.Trim();
            eInvoiceBankCode = eInvoiceBankCode?.Trim();

            evt = null;

            if (string.IsNullOrWhiteSpace(creditNr))
            {
                failedMessage = "Missing creditNr";
                return false;
            }
            if (string.IsNullOrWhiteSpace(eInvoiceAddress) || eInvoiceAddress.Length > 22)
            {
                failedMessage = "eInvoiceAddress must be 1 - 22 chars";
                return false;
            }
            if (string.IsNullOrWhiteSpace(eInvoiceBankCode) || eInvoiceBankCode.Length > 8)
            {
                failedMessage = "eInvoiceBankCode must be 1 - 22 chars";
                return false;
            }

            var e = AddBusinessEvent(BusinessEventType.StartedEInvoiceFi, context);

            AddDatedCreditString(DatedCreditStringCode.EInvoiceFiStatus.ToString(), StartedStatusName, creditNr, e, context);
            AddDatedCreditString(DatedCreditStringCode.EInvoiceAddress.ToString(), eInvoiceAddress, creditNr, e, context);
            AddDatedCreditString(DatedCreditStringCode.EInvoiceFiBankCode.ToString(), eInvoiceBankCode, creditNr, e, context);
            AddComment($"{(isChange ? "Changed" : "Started")} e-invoice", BusinessEventType.StartedEInvoiceFi, context, creditNr: creditNr);

            failedMessage = null;
            evt = e;

            return true;
        }

        private bool TryStopDirectly(ICreditContextExtended context, string creditNr, out string failedMessage, out BusinessEvent evt)
        {
            evt = null;

            if (string.IsNullOrWhiteSpace(creditNr))
            {
                failedMessage = "Missing creditNr";
                return false;
            }

            var e = AddBusinessEvent(BusinessEventType.StoppedEInvoiceFi, context);

            AddDatedCreditString(DatedCreditStringCode.EInvoiceFiStatus.ToString(), StoppedStatusName, creditNr, e, context);
            AddComment("Stopped e-invoice", BusinessEventType.StartedEInvoiceFi, context, creditNr: creditNr);

            failedMessage = null;
            evt = e;

            return true;
        }

        public bool TryStartManually(ICreditContextExtended context, string creditNr, string eInvoiceAddress, string eInvoiceBankCode, out string failedMessage)
        {
            BusinessEvent evt;
            var isOk = TryStartDirectly(context, creditNr, eInvoiceAddress, eInvoiceBankCode, out failedMessage, out evt);
            if (isOk)
            {
                AddEInvoiceFiAction(context, EInvoiceFiMessageHandler.MessageAction.Start, reasonMessage: "started manually", creditNr: creditNr, connectedEvent: evt);
            }

            return isOk;
        }

        public bool TryStopManually(ICreditContextExtended context, string creditNr, out string failedMessage)
        {
            BusinessEvent evt;
            var isOk = TryStopDirectly(context, creditNr, out failedMessage, out evt);
            if (isOk)
            {
                AddEInvoiceFiAction(context, EInvoiceFiMessageHandler.MessageAction.Stop, reasonMessage: "stopped manually", creditNr: creditNr, connectedEvent: evt);
            }

            return isOk;
        }
    }
}