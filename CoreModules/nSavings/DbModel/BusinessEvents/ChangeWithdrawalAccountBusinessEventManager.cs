using System;
using System.Collections.Generic;
using System.Linq;
using NTech.Banking.Shared.BankAccounts.Fi;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;

namespace nSavings.DbModel.BusinessEvents
{
    public class ChangeWithdrawalAccountBusinessEventManager : BusinessEventManagerBase
    {
        public ChangeWithdrawalAccountBusinessEventManager(int userId, string informationMetadata) : base(userId,
            informationMetadata)
        {
        }

        public bool TryInitiateChangeWithdrawalIbanFi(string savingsAccountNr, IBANFi withdrawalIban,
            string powerOfAttorneyArchiveDocumentKey, out string failedMessage,
            out SavingsAccountWithdrawalAccountChange result)
        {
            if (withdrawalIban == null)
            {
                result = null;
                failedMessage = "Missing withdrawalIban";
                return false;
            }

            using (var context = new SavingsContext())
            {
                if (context.SavingsAccountWithdrawalAccountChanges.Any(x =>
                        x.SavingsAccountNr == savingsAccountNr && !x.CommitedOrCancelledByEventId.HasValue))
                {
                    result = null;
                    failedMessage =
                        "There is already a pending withdrawal account change on this account. Commit or cancel that first.";
                    return false;
                }

                var h = context.SavingsAccountHeaders.SingleOrDefault(x => x.SavingsAccountNr == savingsAccountNr);

                if (h == null)
                {
                    result = null;
                    failedMessage = "No such savings account";
                    return false;
                }

                if (h.Status != SavingsAccountStatusCode.Active.ToString() &&
                    h.Status != SavingsAccountStatusCode.FrozenBeforeActive.ToString())
                {
                    result = null;
                    failedMessage =
                        "Account is not Active or FrozenBeforeActive"; //Allow frozen to make changing the account a possible fraud mitigation measure before allowing access to the account
                    return false;
                }

                var evt = AddBusinessEvent(BusinessEventType.InitiateWithdrawalAccountChange, context);

                result = new SavingsAccountWithdrawalAccountChange
                {
                    InitiatedByEvent = evt,
                    NewWithdrawalIban = withdrawalIban.NormalizedValue,
                    PowerOfAttorneyDocumentArchiveKey = powerOfAttorneyArchiveDocumentKey,
                    SavingsAccount = h
                };
                FillInInfrastructureFields(result);
                context.SavingsAccountWithdrawalAccountChanges.Add(result);

                AddComment($"External account change initiated.", BusinessEventType.InitiateWithdrawalAccountChange,
                    context, savingsAccount: h,
                    attachmentArchiveKeys: powerOfAttorneyArchiveDocumentKey == null
                        ? null
                        : new List<string> { powerOfAttorneyArchiveDocumentKey });

                context.SaveChanges();

                failedMessage = null;
                return true;
            }
        }

        public bool TryCommitChangeWithdrawalIbanFi(int savingsAccountWithdrawalAccountChangeId,
            out string failedMessage, out SavingsAccountWithdrawalAccountChange result)
        {
            using (var context = new SavingsContext())
            {
                var change = context
                    .SavingsAccountWithdrawalAccountChanges
                    .Include("SavingsAccount")
                    .SingleOrDefault(x => x.Id == savingsAccountWithdrawalAccountChangeId);

                if (change == null)
                {
                    result = null;
                    failedMessage = "No such change exists";
                    return false;
                }

                if (change.CommitedOrCancelledByEventId.HasValue)
                {
                    result = null;
                    failedMessage = "The change is not pending";
                    return false;
                }

                var h = change.SavingsAccount;

                if (h.Status != SavingsAccountStatusCode.Active.ToString() &&
                    h.Status != SavingsAccountStatusCode.FrozenBeforeActive.ToString())
                {
                    result = null;
                    failedMessage =
                        "Account is not Active or FrozenBeforeActive"; //Allow frozen to make changing the account a possible fraud mitigation measure before allowing access to the account
                    return false;
                }

                var evt = AddBusinessEvent(BusinessEventType.WithdrawalAccountChange, context);

                AddDatedSavingsAccountString(DatedSavingsAccountStringCode.WithdrawalIban.ToString(),
                    change.NewWithdrawalIban, h, evt, context);

                change.CommitedOrCancelledByEvent = evt;

                if (!string.IsNullOrWhiteSpace(change.PowerOfAttorneyDocumentArchiveKey))
                {
                    AddSavingsAccountDocument(SavingsAccountDocumentTypeCode.WithdrawalAccountChangeAgreement,
                        change.PowerOfAttorneyDocumentArchiveKey, evt, context, savingsAccount: h);
                }

                AddComment($"External account changed.", BusinessEventType.WithdrawalAccountChange, context,
                    savingsAccount: h,
                    attachmentArchiveKeys: change.PowerOfAttorneyDocumentArchiveKey == null
                        ? null
                        : new List<string> { change.PowerOfAttorneyDocumentArchiveKey });

                context.SaveChanges();

                failedMessage = null;
                result = change;

                return true;
            }
        }

        public bool TryCancelChangeWithdrawalIbanFi(int savingsAccountWithdrawalAccountChangeId,
            out string failedMessage, out SavingsAccountWithdrawalAccountChange result)
        {
            using (var context = new SavingsContext())
            {
                var change = context
                    .SavingsAccountWithdrawalAccountChanges
                    .Include("SavingsAccount")
                    .SingleOrDefault(x => x.Id == savingsAccountWithdrawalAccountChangeId);

                if (change == null)
                {
                    result = null;
                    failedMessage = "No such change exists";
                    return false;
                }

                if (change.CommitedOrCancelledByEventId.HasValue)
                {
                    result = null;
                    failedMessage = "The change is not pending";
                    return false;
                }

                var h = change.SavingsAccount;

                var evt = AddBusinessEvent(BusinessEventType.CancelWithdrawalAccountChange, context);

                change.CommitedOrCancelledByEvent = evt;

                AddComment($"External account change cancelled.", BusinessEventType.CancelWithdrawalAccountChange,
                    context, savingsAccount: h);

                context.SaveChanges();

                failedMessage = null;
                result = change;

                return true;
            }
        }

        public class AccountChangeHistoryModel
        {
            public string SavingsAccountNr { get; set; }
            public int? PendingChangeId { get; set; }

            public int InitiatedBusinessEventId { get; set; }
            public int InitiatedByUserId { get; set; }
            public string InitiatedBusinessEventType { get; set; }
            public DateTime InitiatedTransactionDate { get; set; }

            public bool IsPending { get; set; }
            public bool IsInitial { get; set; }
            public bool IsCancelled { get; set; }
            public bool IsCommited { get; set; }

            public int? CommittedOrCancelledByUserId { get; set; }
            public DateTime? CommittedOrCancelledDate { get; set; }

            public string WithdrawalAccount { get; set; }
            public string PowerOfAttorneyDocumentArchiveKey { get; set; }
        }

        public static IQueryable<AccountChangeHistoryModel> GetWithdrawalAccountHistoryQuery(SavingsContext context)
        {
            return context.SavingsAccountHeaders.Select(x => new
                {
                    Initial = new AccountChangeHistoryModel
                    {
                        IsInitial = true,
                        PendingChangeId = null,
                        SavingsAccountNr = x.SavingsAccountNr,
                        InitiatedBusinessEventId = x.CreatedByBusinessEventId,
                        InitiatedBusinessEventType = x.CreatedByEvent.EventType,
                        InitiatedByUserId = x.CreatedByEvent.ChangedById,
                        InitiatedTransactionDate = x.CreatedByEvent.TransactionDate,
                        CommittedOrCancelledByUserId = x.CreatedByEvent.ChangedById,
                        CommittedOrCancelledDate = x.CreatedByEvent.TransactionDate,
                        IsPending = false,
                        IsCancelled = false,
                        IsCommited = true,
                        WithdrawalAccount = x
                            .DatedStrings
                            .Where(y => y.Name == DatedSavingsAccountStringCode.WithdrawalIban.ToString() &&
                                        y.BusinessEventId == x.CreatedByBusinessEventId)
                            .Select(y => y.Value)
                            .FirstOrDefault(),
                        PowerOfAttorneyDocumentArchiveKey = x
                            .DatedStrings
                            .Where(y =>
                                y.Name == DatedSavingsAccountStringCode.SignedInitialAgreementArchiveKey.ToString() &&
                                y.BusinessEventId == x.CreatedByBusinessEventId)
                            .Select(y => y.Value)
                            .FirstOrDefault(),
                    },
                    Changes = x.SavingsAccountWithdrawalAccountChanges.Select(y => new AccountChangeHistoryModel
                    {
                        IsInitial = false,
                        PendingChangeId = !y.CommitedOrCancelledByEventId.HasValue ? y.Id : new int?(),
                        SavingsAccountNr = x.SavingsAccountNr,
                        InitiatedBusinessEventId = y.InitiatedByBusinessEventId,
                        InitiatedBusinessEventType = y.InitiatedByEvent.EventType,
                        InitiatedByUserId = y.InitiatedByEvent.ChangedById,
                        InitiatedTransactionDate = y.InitiatedByEvent.TransactionDate,
                        CommittedOrCancelledByUserId = y.CommitedOrCancelledByEventId.HasValue
                            ? (int?)y.CommitedOrCancelledByEvent.ChangedById
                            : null,
                        CommittedOrCancelledDate = y.CommitedOrCancelledByEventId.HasValue
                            ? (DateTime?)y.CommitedOrCancelledByEvent.TransactionDate
                            : null,
                        IsPending = !y.CommitedOrCancelledByEventId.HasValue,
                        IsCancelled = y.CommitedOrCancelledByEventId.HasValue &&
                                      y.CommitedOrCancelledByEvent.EventType !=
                                      BusinessEventType.WithdrawalAccountChange.ToString(),
                        IsCommited = y.CommitedOrCancelledByEventId.HasValue &&
                                     y.CommitedOrCancelledByEvent.EventType ==
                                     BusinessEventType.WithdrawalAccountChange.ToString(),
                        WithdrawalAccount = y.NewWithdrawalIban,
                        PowerOfAttorneyDocumentArchiveKey = y.PowerOfAttorneyDocumentArchiveKey
                    })
                })
                .SelectMany(x => x.Changes.Concat(new[] { x.Initial }))
                .Where(x => x.WithdrawalAccount != null);
        }
    }
}