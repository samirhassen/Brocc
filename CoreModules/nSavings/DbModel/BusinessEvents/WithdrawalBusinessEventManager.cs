using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.Linq;
using nSavings.Code;
using NTech.Banking.Shared.BankAccounts.Fi;
using NTech.Core.Savings.Shared.BusinessEvents;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;

namespace nSavings.DbModel.BusinessEvents
{
    public class WithdrawalBusinessEventManager : BusinessEventManagerBase
    {
        public WithdrawalBusinessEventManager(int userId, string informationMetadata) : base(userId,
            informationMetadata)
        {
        }

        public class WithdrawalRequest
        {
            public string SavingsAccountNr { get; set; }
            public decimal? WithdrawalAmount { get; set; }
            public string ToIban { get; set; }
            public string UniqueOperationToken { get; set; } //For dupe prevention
            public string CustomCustomerMessageText { get; set; }
            public string CustomTransactionText { get; set; }

            //Extra data for analysis/fraud detection/criminal investigations and similar
            public string RequestIpAddress { get; set; }
            public string RequestAuthenticationMethod { get; set; }
            public DateTimeOffset? RequestDate { get; set; }
            public int? RequestedByCustomerId { get; set; }
            public int? RequestedByHandlerUserId { get; set; }
            public decimal? PenaltyFees { get; set; }
        }

        public bool TryCreateNew(WithdrawalRequest request, bool allowSkipUniqueOperationToken, bool allowCheckpoint,
            out string failedMessage, out BusinessEvent evt)
        {
            try
            {
                var r = EncryptionContext.WithEncryption(ec =>
                {
                    var result = TryCreateNewI(ec, request, allowSkipUniqueOperationToken, allowCheckpoint, out var fm,
                        out var e);
                    ec.Context.SaveChanges();
                    return Tuple.Create(result, fm, e);
                });
                failedMessage = r.Item2;
                evt = r.Item3;
                return r.Item1;
            }
            catch (DbUpdateException ex)
            {
                if (!(ex.FormatException()?.Contains("OutgoingPaymentHeader_UniqueToken_UIdx") ?? false)) throw;

                failedMessage =
                    "Operation token has already been used. Generate a new one and try again if this call is not a duplicate.";
                evt = null;
                return false;
            }
        }

        private bool TryCreateNewI(EncryptionContext encryptionContext, WithdrawalRequest request,
            bool allowSkipUniqueOperationToken, bool allowCheckpoint, out string failedMessage, out BusinessEvent evt)
        {
            var context = encryptionContext.Context;

            evt = null;
            BusinessEvent evtWithdrawalPenaltyFee = new BusinessEvent();
            if (request == null)
            {
                failedMessage = "Missing everything";
                return false;
            }

            if (request.RequestedByCustomerId.HasValue && request.RequestedByHandlerUserId.HasValue)
            {
                failedMessage =
                    "RequestedByCustomerId and RequestedByHandlerUserId cannot both be set. Either a customer initiated the withdrawal or a handler did. It cant be both.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.ToIban))
            {
                failedMessage = "Missing toIban";
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.SavingsAccountNr))
            {
                failedMessage = "Missing savings account nr";
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.UniqueOperationToken) && !allowSkipUniqueOperationToken)
            {
                failedMessage = "Missing uniqueOperationToken";
                return false;
            }

            if (!request.WithdrawalAmount.HasValue || request.WithdrawalAmount <= 0m)
            {
                failedMessage = "Missing or invalid WithdrawalAmount";
                return false;
            }

            if (!IBANFi.TryParse(request.ToIban, out var parsedIban))
            {
                failedMessage = "Invalid iban";
                return false;
            }

            evt = AddBusinessEvent(BusinessEventType.Withdrawal, context);

            var h = context
                .SavingsAccountHeaders
                .Where(x => x.SavingsAccountNr == request.SavingsAccountNr)
                .Select(x => new
                {
                    x.MainCustomerId,
                    x.Status,
                    x.MaturesAt,
                    x.AccountTypeCode,
                    CapitalBalance =
                        x.Transactions.Where(y => y.AccountCode == LedgerAccountTypeCode.Capital.ToString())
                            .Sum(y => (decimal?)y.Amount) ?? 0m
                })
                .SingleOrDefault();

            if (h == null)
            {
                failedMessage = "No such account";
                return false;
            }

            if (h.Status != SavingsAccountStatusCode.Active.ToString())
            {
                failedMessage = "Account is not active";
                return false;
            }

            request.WithdrawalAmount = Math.Round(request.WithdrawalAmount.Value, 2);
           
            if (h.CapitalBalance < request.WithdrawalAmount.Value)
            {
                failedMessage = "Balance < Withdrawal amount";
                return false;
            }

            if (!TryGetCustomerNameFromCustomerCard(h.MainCustomerId, out var mainCustomerName, out failedMessage))
            {
                return false;
            }

            if (!allowCheckpoint && HasTransactionBlockCheckpoint(h.MainCustomerId))
            {
                failedMessage = "Withdrawals suspended";
                return false;
            }

            bool hasPenalty = false;
            decimal? penaltyAmount = 0;
            string penaltyComments = string.Empty;
            if (h.AccountTypeCode == nameof(SavingsAccountTypeCode.FixedInterestAccount))
            {
                if (h.MaturesAt == null)
                {
                    failedMessage = "Missing MatureAt date";
                    return false;
                }

                if (Clock.Now.Date <= h.MaturesAt)
                {
                    evtWithdrawalPenaltyFee = AddBusinessEvent(BusinessEventType.WithdrawalPenaltyFee, context);

                    hasPenalty = true;
                    penaltyAmount = Math.Round(Convert.ToDecimal(request.WithdrawalAmount * request.PenaltyFees / 100), 2);
                    request.WithdrawalAmount = request.WithdrawalAmount - penaltyAmount;
                    penaltyComments = "Penalty amount :" + penaltyAmount;
                }
            }

            var w = new OutgoingPaymentHeader
            {
                BookKeepingDate = Clock.Today,
                TransactionDate = Clock.Today,
                CreatedByEvent = evt,
                UniqueToken = request.UniqueOperationToken,
                Items = new List<OutgoingPaymentHeaderItem>()
            };
            FillInInfrastructureFields(w);

            //Transactions
            AddTransaction(context, LedgerAccountTypeCode.Capital,
                -request.WithdrawalAmount.Value, evt, w.BookKeepingDate,
                savingsAccountNr: request.SavingsAccountNr,
                outgoingPayment: w);

            AddTransaction(context, LedgerAccountTypeCode.ShouldBePaidToCustomer,
                request.WithdrawalAmount.Value, evt, w.BookKeepingDate,
                savingsAccountNr: request.SavingsAccountNr,
                outgoingPayment: w);


            //Penalty Transactions 
            if (hasPenalty)
            {
                AddTransaction(context, LedgerAccountTypeCode.Capital,
               -penaltyAmount.Value, evtWithdrawalPenaltyFee, w.BookKeepingDate,
               savingsAccountNr: request.SavingsAccountNr,
               outgoingPayment: w);

                AddTransaction(context, LedgerAccountTypeCode.PenaltyFee,
                penaltyAmount.Value, evtWithdrawalPenaltyFee, w.BookKeepingDate,
                savingsAccountNr: request.SavingsAccountNr,
                outgoingPayment: w);
            }


            AddItem(OutgoingPaymentHeaderItemCode.CustomerMessage,
                string.IsNullOrWhiteSpace(request.CustomCustomerMessageText)
                    ? NewIncomingPaymentFileBusinessEventManager.GetOutgoingPaymentFileCustomerMessage(NEnv.EnvSettings,
                        eventName: "Withdrawal", contextNumber: request.SavingsAccountNr)
                    : request.CustomCustomerMessageText, false);
            if (!string.IsNullOrWhiteSpace(request.CustomTransactionText))
                AddItem(OutgoingPaymentHeaderItemCode.CustomTransactionMessage, request.CustomTransactionText, false);
            AddItem(OutgoingPaymentHeaderItemCode.CustomerName, mainCustomerName, true);
            AddItem(OutgoingPaymentHeaderItemCode.FromIban, NEnv.OutgoingPaymentIban.NormalizedValue, false);
            AddItem(OutgoingPaymentHeaderItemCode.ToIban, parsedIban.NormalizedValue, false);
            AddItem(OutgoingPaymentHeaderItemCode.SavingsAccountNr, request.SavingsAccountNr, false);
            if (request.RequestIpAddress != null)
                AddItem(OutgoingPaymentHeaderItemCode.RequestIpAddress, request.RequestIpAddress, true);
            if (request.RequestAuthenticationMethod != null)
                AddItem(OutgoingPaymentHeaderItemCode.RequestAuthenticationMethod, request.RequestAuthenticationMethod,
                    false);
            if (request.RequestDate.HasValue)
                AddItem(OutgoingPaymentHeaderItemCode.RequestDate,
                    request.RequestDate.Value.ToString("o", CultureInfo.InvariantCulture), false);
            if (request.RequestedByCustomerId.HasValue)
                AddItem(OutgoingPaymentHeaderItemCode.RequestedByCustomerId,
                    request.RequestedByCustomerId.Value.ToString(), false);
            if (request.RequestedByHandlerUserId.HasValue)
                AddItem(OutgoingPaymentHeaderItemCode.RequestedByHandlerUserId,
                    request.RequestedByHandlerUserId.Value.ToString(), false);

            //Comment
            AddComment(
      $"Withdrawal of {request.WithdrawalAmount.GetValueOrDefault().ToString("C", CommentFormattingCulture)} " + penaltyComments,
      BusinessEventType.Withdrawal, context, savingsAccountNr: request.SavingsAccountNr);

            ////////////////////////////////////////////////
            //////////////// Handle encryption /////////////
            ////////////////////////////////////////////////
            if (w != null)
            {
                var itemsToEncrypt = w.Items.Where(x => x.IsEncrypted == true).ToArray();
                if (itemsToEncrypt.Length > 0)
                {
                    var enc = NEnv.EncryptionKeys;
                    encryptionContext.SaveEncryptItems(
                        itemsToEncrypt,
                        x => x.Value,
                        (x, id) => x.Value = id.ToString(),
                        UserId,
                        enc.CurrentKeyName,
                        enc.AsDictionary());
                }
            }

            failedMessage = null;
            return true;

            //Items
            void AddItem(OutgoingPaymentHeaderItemCode name, string value, bool isEncrypted)
            {
                var item = new OutgoingPaymentHeaderItem { IsEncrypted = isEncrypted, OutgoingPayment = w, Name = name.ToString(), Value = value };
                FillInInfrastructureFields(item);
                w.Items.Add(item);
                context.OutgoingPaymentHeaderItems.Add(item);
            }
        }

        public static bool HasTransactionBlockCheckpoint(int customerId)
        {
            return HasTransactionBlockCheckpoint(new HashSet<int> { customerId })[customerId];
        }

        public static Dictionary<int, bool> HasTransactionBlockCheckpoint(HashSet<int> customerIds)
        {
            var customerClient =
                LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceHttpContextUser.SharedInstance,
                    NEnv.ServiceRegistry);
            return NewIncomingPaymentFileBusinessEventManager.HasTransactionBlockCheckpoint(customerIds,
                customerClient);
        }
    }
}