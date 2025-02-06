using nCredit.DbModel.BusinessEvents;
using NTech;
using NTech.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.Code.EInvoiceFi
{
    public class EInvoiceFiMessageHandler
    {
        public enum MessageAction
        {
            SkipMessage,
            ErrorList,
            LeaveInQueue,
            Start,
            Stop,
            Change
        }

        public class ProcessMessageResult
        {
            public MessageAction Action { get; set; }
            public string MatchedCreditNr { get; set; }
            public string ActionReasonMessage { get; set; }
            public string EInvoiceAddress { get; set; }
            public string EInvoiceBankCode { get; set; }
        }

        public bool IsValidEInvoiceAddress(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        public bool IsValidEInvoiceBankCode(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        public ProcessMessageResult ProcessMatchedMessage(IEInvoiceFiMessageHeader message, string matchedCreditNr, EInvoiceFiBusinessEventManager.EInvoiceState currentState, ICoreClock clock)
        {
            var eInvoiceAddress = new Lazy<string>(() => message.GetItemValue(EInvoiceFiItemCode.EInvoiceAddress));
            var eInvoiceBankCode = new Lazy<string>(() => message.GetItemValue(EInvoiceFiItemCode.EInvoiceBankCode));

            Func<ProcessMessageResult> validateEInvoiceAddressAndBankCode = () =>
            {
                var invalidMessage = "";
                if (!IsValidEInvoiceAddress(eInvoiceAddress.Value))
                    invalidMessage += " invalid eInvoiceAddress";
                if (!IsValidEInvoiceBankCode(eInvoiceBankCode.Value))
                    invalidMessage += " invalid eInvoiceBankCode";

                return (invalidMessage.Length > 0) ? new ProcessMessageResult
                {
                    Action = MessageAction.ErrorList,
                    MatchedCreditNr = matchedCreditNr,
                    ActionReasonMessage = invalidMessage
                } : null;
            };

            if (message.ExternalMessageType == "start")
            {
                if (currentState.IsStarted)
                {
                    var timeInQueue = clock.Today.Subtract(message.ImportDate);
                    if (timeInQueue < TimeSpan.FromDays(7))
                    {
                        //Interpreted as a bank change that arrives in the wrong order. We leave this as unprocessed for a week and see if the stop arrives. If it does, we process this after
                        return new ProcessMessageResult
                        {
                            Action = MessageAction.LeaveInQueue,
                            MatchedCreditNr = matchedCreditNr,
                            ActionReasonMessage = "Left in queue awaiting stop-message (suspected bank change)"
                        };
                    }
                    else
                    {
                        return new ProcessMessageResult
                        {
                            Action = MessageAction.ErrorList,
                            MatchedCreditNr = matchedCreditNr,
                            ActionReasonMessage = $"Stopped waiting for stop-message after {(int)timeInQueue.TotalDays} days"
                        };
                    }
                }
                else
                {
                    var invalidMessage = validateEInvoiceAddressAndBankCode();
                    if (invalidMessage != null)
                        return invalidMessage;

                    return new ProcessMessageResult
                    {
                        Action = MessageAction.Start,
                        MatchedCreditNr = matchedCreditNr,
                        EInvoiceAddress = eInvoiceAddress.Value,
                        EInvoiceBankCode = eInvoiceBankCode.Value
                    };
                }
            }
            else if (message.ExternalMessageType == "stop")
            {
                if (currentState.IsStarted)
                {
                    return new ProcessMessageResult
                    {
                        Action = MessageAction.Stop,
                        MatchedCreditNr = matchedCreditNr
                    };
                }
                else
                {
                    return new ProcessMessageResult
                    {
                        Action = MessageAction.SkipMessage,
                        MatchedCreditNr = matchedCreditNr,
                        ActionReasonMessage = "was already stopped"
                    };
                }
            }
            else if (message.ExternalMessageType == "change")
            {
                var invalidMessage = validateEInvoiceAddressAndBankCode();
                if (invalidMessage != null)
                    return invalidMessage;

                //Doesnt really matter if it's already running or not since we will change to these values regardless
                if (currentState.IsStarted)
                    return new ProcessMessageResult
                    {
                        Action = MessageAction.Change,
                        MatchedCreditNr = matchedCreditNr,
                        EInvoiceAddress = eInvoiceAddress.Value,
                        EInvoiceBankCode = eInvoiceBankCode.Value
                    };
                else
                    return new ProcessMessageResult
                    {
                        Action = MessageAction.Start,
                        ActionReasonMessage = "change interpreted as start",
                        MatchedCreditNr = matchedCreditNr,
                        EInvoiceAddress = eInvoiceAddress.Value,
                        EInvoiceBankCode = eInvoiceBankCode.Value
                    };
            }
            else
            {
                return new ProcessMessageResult
                {
                    Action = MessageAction.ErrorList,
                    ActionReasonMessage = $"Unknown message type: {message.ExternalMessageType}",
                    MatchedCreditNr = matchedCreditNr
                };
            }
        }

        public Tuple<string, ProcessMessageResult> MatchMessageToCredit(IEInvoiceFiMessageHeader message, IEInvoiceFiMessageMatchingRepository messageRepository)
        {
            if (messageRepository.IsDuplicateExternalMessageId(message.ExternalMessageId))
            {
                return Tuple.Create((string)null, new ProcessMessageResult
                {
                    Action = MessageAction.SkipMessage,
                    ActionReasonMessage = "duplicate external message id"
                });
            }
            else
            {
                var eInvoiceAddress = message.GetItemValue(EInvoiceFiItemCode.EInvoiceAddress);
                var eInvoiceBankCode = message.GetItemValue(EInvoiceFiItemCode.EInvoiceBankCode);
                var emailIdentification = message.GetItemValue(EInvoiceFiItemCode.CustomerIdentification1);
                var ocrIdentification = message.GetItemValue(EInvoiceFiItemCode.CustomerIdentification2);

                //Deal with the identifierers being swapped
                if (!string.IsNullOrWhiteSpace(emailIdentification) && !string.IsNullOrWhiteSpace(ocrIdentification) && !emailIdentification.Contains("@") && ocrIdentification.Contains("@"))
                {
                    var tmp = emailIdentification;
                    emailIdentification = ocrIdentification;
                    ocrIdentification = tmp;
                }

                //To to match against a credit
                var matchedCreditNrs = new List<string>();
                if (message.ExternalMessageType == "stop")
                {
                    var matchedNrs = messageRepository.GetCreditNrsUsingEInvoiceIdentifiers(eInvoiceAddress, eInvoiceBankCode);
                    matchedCreditNrs.AddRange(matchedNrs);
                }

                if (matchedCreditNrs.Count == 0) //stop with no matches or start/change always
                {
                    var matchedNrsOnEmail = messageRepository.GetCreditNrsUsingEmail(emailIdentification);
                    var matchedNrsOnOcrNr = messageRepository.GetCreditNrsUsingOcr(ocrIdentification);

                    if ((matchedNrsOnOcrNr?.Any() ?? false) && (matchedNrsOnEmail?.Any() ?? false))
                    {
                        matchedCreditNrs.AddRange(matchedNrsOnEmail.Intersect(matchedNrsOnOcrNr));
                    }
                    else
                    {
                        matchedCreditNrs.AddRange(matchedNrsOnEmail ?? new List<string>());
                        matchedCreditNrs.AddRange(matchedNrsOnOcrNr ?? new List<string>());
                    }
                }
                if (matchedCreditNrs.Count == 1)
                {
                    var matchedCreditNr = matchedCreditNrs.Single();
                    matchedCreditNrs = messageRepository.FilterOutNonActiveCreditNrs(matchedCreditNrs);
                    if (matchedCreditNrs.Count == 0)
                        return Tuple.Create((string)null, new ProcessMessageResult
                        {
                            Action = MessageAction.SkipMessage,
                            MatchedCreditNr = matchedCreditNr,
                            ActionReasonMessage = "Matched credit was not active"
                        });
                }
                else if (matchedCreditNrs.Count > 1)
                {
                    //Filter out credits that are not active
                    matchedCreditNrs = messageRepository.FilterOutNonActiveCreditNrs(matchedCreditNrs);
                }

                if (matchedCreditNrs.Count == 0)
                {
                    return Tuple.Create((string)null, new ProcessMessageResult
                    {
                        Action = MessageAction.ErrorList,
                        ActionReasonMessage = "No matching active credit found"
                    });
                }
                else if (matchedCreditNrs.Count > 1)
                {
                    var moreText = matchedCreditNrs.Count > 5 ? $" and {(matchedCreditNrs.Count - 5).ToString()} more" : "";
                    return Tuple.Create((string)null, new ProcessMessageResult
                    {
                        Action = MessageAction.ErrorList,
                        ActionReasonMessage = $"Several matching credits found. Creditnrs: {string.Join(", ", matchedCreditNrs.Take(5))}" + moreText
                    });
                }
                else
                {
                    return Tuple.Create(matchedCreditNrs.Single(), (ProcessMessageResult)null);
                }
            }
        }

        public interface IEInvoiceFiMessageMatchingRepository
        {
            List<string> FilterOutNonActiveCreditNrs(IList<string> creditNrs);
            List<string> GetCreditNrsUsingEInvoiceIdentifiers(string eInvoiceAddress, string eInvoiceBankCode);
            List<string> GetCreditNrsUsingEmail(string email);
            List<string> GetCreditNrsUsingOcr(string ocr);
            bool IsDuplicateExternalMessageId(string externalMessageId);
        }
    }
}