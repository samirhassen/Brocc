using nSavings.Code;
using nSavings.Code.Services;
using NTech.Banking.BankAccounts.Fi;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace nSavings.DbModel.BusinessEvents
{
    public class AccountClosureBusinessEventManager : BusinessEventManagerBase
    {
        private readonly ICustomerRelationsMergeService customerRelationsMergeService;

        public AccountClosureBusinessEventManager(int userId, string informationMetadata, ICustomerRelationsMergeService customerRelationsMergeService) : base(userId, informationMetadata)
        {
            this.customerRelationsMergeService = customerRelationsMergeService;
        }

        public class AccountClosureRequest
        {
            public string SavingsAccountNr { get; set; }
            public string ToIban { get; set; }
            public string UniqueOperationToken { get; set; } //For dupe prevention
            public bool? IncludeCalculationDetails { get; set; }
            public string CustomCustomerMessageText { get; set; }
            public string CustomTransactionText { get; set; }
            public string RequestAuthenticationMethod { get; set; }
            public string RequestIpAddress { get; set; }
            public DateTimeOffset? RequestDate { get; set; }
            public int? RequestedByCustomerId { get; set; }
            public int? RequestedByHandlerUserId { get; set; }
        }

        public class AccountClosurePreviewResult
        {
            public decimal CapitalBalanceBefore { get; set; }
            public decimal WithdrawalAmount { get; set; }
            public CapItem CapitalizedInterest { get; set; }
            public class CapItem
            {
                public decimal InterestAmount { get; set; }
                public decimal ShouldBeWithheldForTaxAmount { get; set; }
                public DateTime FromInterestDate { get; set; }
                public DateTime ToInterestDate { get; set; }
                public int NrOfInterestDays { get; set; }
            }
        }

        public bool TryPreviewCloseAccount(string savingsAccountNr, bool allowCheckpoint, out string failedMessage, out AccountClosurePreviewResult result)
        {
            using (var context = new SavingsContext())
            {
                ClosureData d;
                if (TryComputeData(context, allowCheckpoint, savingsAccountNr, out failedMessage, out d))
                {
                    result = new AccountClosurePreviewResult
                    {
                        CapitalBalanceBefore = d.CapitalBalanceBefore,
                        CapitalizedInterest = d.CapitalizationResult.IsCapitalizationNeeded() ? new AccountClosurePreviewResult.CapItem
                        {
                            InterestAmount = d.CapitalizationResult.TotalInterestAmount,
                            ShouldBeWithheldForTaxAmount = d.CapitalizationResult.ShouldBeWithheldForTaxAmount,
                            FromInterestDate = d.CapitalizationResult.FromDate,
                            ToInterestDate = d.CapitalizationResult.ToDate,
                            NrOfInterestDays = d.CapitalizationResult.InterestAmountParts.Count
                        } : null,
                        WithdrawalAmount = d.CapitalBalanceBefore + d.CapitalizationResult.TotalInterestAmount - d.CapitalizationResult.ShouldBeWithheldForTaxAmount
                    };
                    return true;
                }
                else
                {
                    result = null;
                    return false;
                }
            }
        }

        public bool TryCloseAccount(AccountClosureRequest request, bool allowSkipUniqueOperationToken, bool allowCheckpoint, out string failedMessage, out BusinessEvent evt)
        {
            try
            {
                var r = EncryptionContext.WithEncryption(ec =>
                {
                    var result = TryCloseAccountI(ec, request, allowSkipUniqueOperationToken, allowCheckpoint, out var fm, out var e);
                    ec.Context.SaveChanges();

                    return Tuple.Create(result, fm, e);
                });

                if (r.Item1) //if was closed
                {
                    try
                    {

                        customerRelationsMergeService.MergeSavingsAccountsToCustomerRelations(onlySavingsAccountNrs: new HashSet<string>
                            {
                                request.SavingsAccountNr
                            });
                    }
                    catch
                    {
                        /* Ignored. Daily maintainance job will fix any issues with relation */
                    }
                }

                failedMessage = r.Item2;
                evt = r.Item3;
                return r.Item1;
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
            {
                if (ex?.FormatException()?.Contains("OutgoingPaymentHeader_UniqueToken_UIdx") ?? false)
                {
                    failedMessage = "Operation token has already been used. Generate a new one and try again if this call is not a duplicate.";
                    evt = null;
                    return false;
                }
                else
                    throw;
            }
        }

        public bool TryGetWithdrawalIban(string savingsAccountNr, out IBANFi iban, out string failedMessage)
        {
            using (var context = new SavingsContext())
            {
                var a = context
                    .SavingsAccountHeaders
                    .Where(x => x.SavingsAccountNr == savingsAccountNr)
                    .Select(x => new
                    {
                        WithdrawalIban = x
                                .DatedStrings
                                .Where(y => y.Name == DatedSavingsAccountStringCode.WithdrawalIban.ToString())
                                .OrderByDescending(y => y.BusinessEventId)
                                .Select(y => y.Value)
                                .FirstOrDefault(),
                    })
                    .SingleOrDefault();

                if (a == null)
                {
                    failedMessage = "No such savings account";
                    iban = null;
                    return false;
                }

                if (a.WithdrawalIban == null)
                {
                    failedMessage = "Savings account has no withdrawaliban";
                    iban = null;
                    return false;
                }

                failedMessage = null;
                iban = IBANFi.Parse(a.WithdrawalIban);

                return true;
            }
        }

        private bool TryCloseAccountI(EncryptionContext encryptionContext, AccountClosureRequest request, bool allowSkipUniqueOperationToken, bool allowCheckpoint, out string failedMessage, out BusinessEvent evt)
        {
            var context = encryptionContext.Context;

            evt = null;

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

            if (!IBANFi.TryParse(request.ToIban, out var parsedIban))
            {
                failedMessage = "Invalid iban";
                return false;
            }

            if (!TryComputeData(context, allowCheckpoint, request.SavingsAccountNr, out failedMessage, out var cd))
            {
                return false;
            }
            var h = cd.Account;

            evt = AddBusinessEvent(BusinessEventType.AccountClosure, context);

            if (h.Status != SavingsAccountStatusCode.Active.ToString())
            {
                failedMessage = "Account is not active";
                return false;
            }

            string mainCustomerName;
            if (!TryGetCustomerNameFromCustomerCard(h.MainCustomerId, out mainCustomerName, out failedMessage))
            {
                return false;
            }

            ///////////////////////////////////
            /// Capitalize interest ///////////
            ///////////////////////////////////
            string calculationDetailsDocumentArchiveKey = null;

            var ir = cd.CapitalizationResult;
            if (ir.IsCapitalizationNeeded())
            {
                var c = new SavingsAccountInterestCapitalization
                {
                    FromDate = ir.FromDate,
                    ToDate = ir.ToDate,
                    SavingsAccountNr = request.SavingsAccountNr,
                    CreatedByEvent = evt
                };
                context.SavingsAccountInterestCapitalizations.Add(c);
                FillInInfrastructureFields(c);
                if (request.IncludeCalculationDetails.GetValueOrDefault())
                {
                    var dc = new DocumentClient();
                    c.CalculationDetailsDocumentArchiveKey = dc.CreateXlsxToArchive(ir.ToDocumentClientExcelRequest(), $"AccountClosureInterestCapitalizationCalculationDetails_{ir.ToDate.ToString("yyyy")}.xlsx");
                    calculationDetailsDocumentArchiveKey = c.CalculationDetailsDocumentArchiveKey;
                }
                if (ir.TotalInterestAmount > 0m)
                {
                    AddTransaction(context, LedgerAccountTypeCode.Capital, ir.TotalInterestAmount, evt, Clock.Today,
                        savingsAccountNr: request.SavingsAccountNr,
                        interestFromDate: ir.ToDate.AddDays(1),
                        businessEventRoleCode: "CapitalizedInterest");
                    AddTransaction(context, LedgerAccountTypeCode.CapitalizedInterest, ir.TotalInterestAmount, evt, Clock.Today,
                        savingsAccountNr: request.SavingsAccountNr);
                }

                if (ir.ShouldBeWithheldForTaxAmount > 0m)
                {
                    AddTransaction(context, LedgerAccountTypeCode.Capital, -ir.ShouldBeWithheldForTaxAmount, evt, Clock.Today,
                        savingsAccountNr: request.SavingsAccountNr,
                        interestFromDate: ir.ToDate.AddDays(1),
                        businessEventRoleCode: "WithheldTax");
                    AddTransaction(context, LedgerAccountTypeCode.WithheldCapitalizedInterestTax, ir.ShouldBeWithheldForTaxAmount, evt, Clock.Today,
                        savingsAccountNr: request.SavingsAccountNr);
                }
            }

            ///////////////////////////////////
            /// Withdrawal          ///////////
            ///////////////////////////////////

            var capitalBalanceAfterInterest = cd.CapitalBalanceBefore + ir.TotalInterestAmount - ir.ShouldBeWithheldForTaxAmount;
            OutgoingPaymentHeader op = null;
            if (capitalBalanceAfterInterest > 0m)
            {
                op = new OutgoingPaymentHeader
                {
                    BookKeepingDate = Clock.Today,
                    TransactionDate = Clock.Today,
                    CreatedByEvent = evt,
                    UniqueToken = request.UniqueOperationToken,
                    Items = new List<OutgoingPaymentHeaderItem>()
                };
                FillInInfrastructureFields(op);

                AddTransaction(context, LedgerAccountTypeCode.Capital,
                    -capitalBalanceAfterInterest, evt, op.BookKeepingDate,
                    savingsAccountNr: request.SavingsAccountNr,
                    outgoingPayment: op,
                    businessEventRoleCode: "Withdrawal");

                AddTransaction(context, LedgerAccountTypeCode.ShouldBePaidToCustomer,
                    capitalBalanceAfterInterest, evt, op.BookKeepingDate,
                    savingsAccountNr: request.SavingsAccountNr,
                    outgoingPayment: op);

                //Items
                Action<OutgoingPaymentHeaderItemCode, string, bool> addItem = (name, value, isEncrypted) =>
                {
                    var item = new OutgoingPaymentHeaderItem
                    {
                        IsEncrypted = isEncrypted,
                        OutgoingPayment = op,
                        Name = name.ToString(),
                        Value = value
                    };
                    FillInInfrastructureFields(item);
                    op.Items.Add(item);
                    context.OutgoingPaymentHeaderItems.Add(item);
                };
                var hasCustomCustomerMessagText = !string.IsNullOrWhiteSpace(request.CustomCustomerMessageText);
                addItem(OutgoingPaymentHeaderItemCode.CustomerMessage, hasCustomCustomerMessagText
                    ? request.CustomCustomerMessageText
                    : NewIncomingPaymentFileBusinessEventManager.GetOutgoingPaymentFileCustomerMessage(NEnv.EnvSettings,
                            eventName: "Closed", contextNumber: request.SavingsAccountNr), hasCustomCustomerMessagText);
                if (!string.IsNullOrWhiteSpace(request.CustomTransactionText))
                    addItem(OutgoingPaymentHeaderItemCode.CustomTransactionMessage, request.CustomTransactionText, true);
                addItem(OutgoingPaymentHeaderItemCode.CustomerName, mainCustomerName, true);
                addItem(OutgoingPaymentHeaderItemCode.FromIban, NEnv.OutgoingPaymentIban.NormalizedValue, false);
                addItem(OutgoingPaymentHeaderItemCode.ToIban, parsedIban.NormalizedValue, false);
                addItem(OutgoingPaymentHeaderItemCode.SavingsAccountNr, request.SavingsAccountNr, false);
                if (request.RequestIpAddress != null)
                    addItem(OutgoingPaymentHeaderItemCode.RequestIpAddress, request.RequestIpAddress, true);
                if (request.RequestAuthenticationMethod != null)
                    addItem(OutgoingPaymentHeaderItemCode.RequestAuthenticationMethod, request.RequestAuthenticationMethod, false);
                if (request.RequestDate.HasValue)
                    addItem(OutgoingPaymentHeaderItemCode.RequestDate, request.RequestDate.Value.ToString("o", CultureInfo.InvariantCulture), false);
                if (request.RequestedByCustomerId.HasValue)
                    addItem(OutgoingPaymentHeaderItemCode.RequestedByCustomerId, request.RequestedByCustomerId.Value.ToString(), false);
                if (request.RequestedByHandlerUserId.HasValue)
                    addItem(OutgoingPaymentHeaderItemCode.RequestedByHandlerUserId, request.RequestedByHandlerUserId.Value.ToString(), false);
            }

            SetStatus(h, SavingsAccountStatusCode.Closed, evt, context);

            var comment = $"Account closed with balance before interest capitalization of {cd.CapitalBalanceBefore.ToString("C", CommentFormattingCulture)}.";
            if (ir.IsCapitalizationNeeded())
            {
                comment += $" Capitalized interest amount: {ir.TotalInterestAmount.ToString("C", CommentFormattingCulture)}. Withheld tax: {ir.ShouldBeWithheldForTaxAmount.ToString("C", CommentFormattingCulture)}";
            }

            AddComment(comment,
                BusinessEventType.AccountClosure,
                context,
                savingsAccountNr: request.SavingsAccountNr,
                attachmentArchiveKeys: calculationDetailsDocumentArchiveKey == null ? null : new List<string> { calculationDetailsDocumentArchiveKey });

            ////////////////////////////////////////////////
            //////////////// Handle encryption /////////////
            ////////////////////////////////////////////////
            if (op != null)
            {
                var itemsToEncrypt = op.Items.Where(x => x.IsEncrypted == true).ToArray();
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
        }

        private class ClosureData
        {
            public YearlyInterestCapitalizationBusinessEventManager.ResultModel CapitalizationResult { get; set; }
            public decimal CapitalBalanceBefore { get; set; }
            public SavingsAccountHeader Account { get; set; }
        }

        private bool TryComputeData(SavingsContext context, bool allowCheckpoint, string savingsAccountNr, out string failedMessage, out ClosureData result)
        {
            result = null;

            var h = context
                .SavingsAccountHeaders
                .Where(x => x.SavingsAccountNr == savingsAccountNr)
                .Select(x => new
                {
                    Account = x,
                    CapitalBalance = x.Transactions.Where(y => y.AccountCode == LedgerAccountTypeCode.Capital.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m,
                    x.MainCustomerId
                })
                .SingleOrDefault();

            if (h == null)
            {
                failedMessage = "No such account";
                return false;
            }

            if (h.Account.Status != SavingsAccountStatusCode.Active.ToString())
            {
                failedMessage = "Account is not active";
                return false;
            }

            if (!allowCheckpoint && WithdrawalBusinessEventManager.HasTransactionBlockCheckpoint(h.MainCustomerId))
            {
                failedMessage = "Withdrawals suspended";
                return false;
            }

            IDictionary<string, YearlyInterestCapitalizationBusinessEventManager.ResultModel> interestCapResult;
            if (!YearlyInterestCapitalizationBusinessEventManager.TryComputeAccumulatedInterestAssumingAccountIsClosedToday(context, Clock, new List<string> { savingsAccountNr }, true, out interestCapResult, out failedMessage))
            {
                return false;
            }

            result = new ClosureData
            {
                CapitalBalanceBefore = h.CapitalBalance,
                CapitalizationResult = interestCapResult[savingsAccountNr],
                Account = h.Account
            };

            failedMessage = null;
            return true;
        }
    }
}