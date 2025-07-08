using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using NTech.Banking.IncomingPaymentFiles;
using NTech.Banking.Shared.BankAccounts.Fi;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Core.Savings.Shared.Database;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;

namespace NTech.Core.Savings.Shared.BusinessEvents
{
    public class NewIncomingPaymentFileBusinessEventManager : BusinessEventManagerBaseCore
    {
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly ISavingsEnvSettings envSettings;
        private readonly EncryptionService encryptionService;
        private readonly SavingsContextFactory contextFactory;
        private readonly ICustomerClient customerClient;

        public NewIncomingPaymentFileBusinessEventManager(INTechCurrentUserMetadata currentUser, ICoreClock clock, IClientConfigurationCore clientConfiguration,
            ISavingsEnvSettings envSettings, EncryptionService encryptionService, SavingsContextFactory contextFactory, ICustomerClient customerClient) : base(currentUser, clock, clientConfiguration)
        {
            this.clientConfiguration = clientConfiguration;
            this.envSettings = envSettings;
            this.encryptionService = encryptionService;
            this.contextFactory = contextFactory;
            this.customerClient = customerClient;
        }

        public IncomingPaymentFileHeader ImportIncomingPaymentFile(ISavingsContext context, IncomingPaymentFileWithOriginal paymentfile, IDocumentClient documentClient, out string placementMessage, bool skipAutoPlace = false)
        {
            if (!context.HasCurrentTransaction)
                throw new Exception("Needs an ambient transaction");

            var futureBookKeepingDateDates = paymentfile
                .Accounts
                .SelectMany(x => x.DateBatches.Select(y => y.BookKeepingDate))
                .Where(x => x > Clock.Today)
                .Distinct()
                .ToList();
            if (futureBookKeepingDateDates.Any())
            {
                throw new Exception($"Invalid payment file. There are payments for future dates: {string.Join(",", futureBookKeepingDateDates.Select(x => x.ToString("yyyy-MM-dd")))}");
            }

            var evt = AddBusinessEvent(BusinessEventType.IncomingPaymentFileImport, context);

            var h = new IncomingPaymentFileHeader
            {
                TransactionDate = Now.ToLocalTime().Date,
                ExternalId = paymentfile.ExternalId,
                CreatedByEvent = evt,
                Payments = new List<IncomingPaymentHeader>()
            };
            FillInInfrastructureFields(h);
            context.AddIncomingPaymentFileHeaders(h);

            var allOcrsInPaymentFile = paymentfile.Accounts.SelectMany(account => account.DateBatches.SelectMany(date => date.Payments.Where(x => x.OcrReference != null).Select(payment => payment.OcrReference))).Distinct().ToList();
            var savingsAccountNrByOcr = context
                .SavingsAccountHeadersQueryable
                .Select(x => new
                {
                    x.SavingsAccountNr,
                    x.MainCustomerId,
                    x.CreatedByEvent,
                    x.AccountTypeCode,
                    x.Status,
                    OcrNr = x
                            .DatedStrings
                            .Where(y => y.Name == DatedSavingsAccountStringCode.OcrDepositReference.ToString())
                            .OrderByDescending(y => y.BusinessEventId)
                            .Select(y => y.Value)
                            .FirstOrDefault()
                })
                .Where(x => allOcrsInPaymentFile.Contains(x.OcrNr))
                .ToList()
                .ToDictionary(x => x.OcrNr, x => new { x.SavingsAccountNr, x.MainCustomerId, x.Status, x.CreatedByEvent.EventDate, x.AccountTypeCode });

            var customerIds = savingsAccountNrByOcr.Values.Select(x => x.MainCustomerId).Distinct().ToList();
            var accountBalanceByCustomerId = GetCurrentBalanceByCustomerId(context, customerIds);
            var hasTransactionCheckpointByCustomerId = HasTransactionBlockCheckpoint(customerIds.ToHashSetShared(), customerClient);

            var pms = paymentfile.Accounts.SelectMany(account => account.DateBatches.SelectMany(date => date.Payments.Select(payment =>
                new
                {
                    ExternalId = payment.ExternalId,
                    Amount = payment.Amount,
                    BookKeepingDate = date.BookKeepingDate,
                    OcrReference = payment.OcrReference,
                    Items = new[]
                    {
                            string.IsNullOrWhiteSpace(payment.ExternalId) ? null : new { Code = IncomingPaymentHeaderItemCode.ExternalId, Value = payment.ExternalId, IsSensitive = false  },
                            string.IsNullOrWhiteSpace(payment.OcrReference) ? null : new { Code = IncomingPaymentHeaderItemCode.OcrReference, Value = payment.OcrReference, IsSensitive = false  },
                            account.AccountNr == null ? null : new { Code = IncomingPaymentHeaderItemCode.ClientAccountIban, Value = account.AccountNr.NormalizedValue, IsSensitive = false  },
                            string.IsNullOrWhiteSpace(payment.CustomerName) ? null : new { Code = IncomingPaymentHeaderItemCode.CustomerName, Value = payment.CustomerName, IsSensitive = true  },
                            string.IsNullOrWhiteSpace(payment.CustomerAddressTownName) ? null : new { Code = IncomingPaymentHeaderItemCode.CustomerAddressTownName, Value = payment.CustomerAddressTownName, IsSensitive = true  },
                            string.IsNullOrWhiteSpace(payment.CustomerAddressStreetName) ? null : new { Code = IncomingPaymentHeaderItemCode.CustomerAddressStreetName, Value = payment.CustomerAddressStreetName, IsSensitive = true  },
                            string.IsNullOrWhiteSpace(payment.CustomerAddressBuildingNumber) ? null : new { Code = IncomingPaymentHeaderItemCode.CustomerAddressBuildingNumber, Value = payment.CustomerAddressBuildingNumber, IsSensitive = true  },
                            string.IsNullOrWhiteSpace(payment.CustomerAddressCountry) ? null : new { Code = IncomingPaymentHeaderItemCode.CustomerAddressCountry, Value = payment.CustomerAddressCountry, IsSensitive = true  },
                            string.IsNullOrWhiteSpace(payment.CustomerAddressPostalCode) ? null : new { Code = IncomingPaymentHeaderItemCode.CustomerAddressPostalCode, Value = payment.CustomerAddressPostalCode, IsSensitive = true  },
                            (payment.CustomerAddressLines == null || payment.CustomerAddressLines.Count == 0) ? null : new { Code = IncomingPaymentHeaderItemCode.CustomerAddressLines, Value = string.Join(", ", payment.CustomerAddressLines), IsSensitive = true }
                    }.Where(x => x != null).ToArray(),
                    Match = savingsAccountNrByOcr.ContainsKey(payment.OcrReference) ? savingsAccountNrByOcr[payment.OcrReference] : null
                })));

            int placedCount = 0;
            int unplacedCount = 0;
            var maxAllowedSavingsCustomerBalance = envSettings.MaxAllowedSavingsCustomerBalance;
            var incomingPaymentDepositeGracePeriodInDays = envSettings.IncomingPaymentDepositeGracePeriodInDays;

            foreach (var externalPayment in pms)
            {
                var pmt = new IncomingPaymentHeader
                {
                    BookKeepingDate = externalPayment.BookKeepingDate,
                    TransactionDate = Now.ToLocalTime().Date,
                    IsFullyPlaced = false,
                    IncomingPaymentFile = h
                };
                FillInInfrastructureFields(pmt);
                h.Payments.Add(pmt);
                context.AddIncomingPaymentHeaders(pmt);

                foreach (var item in externalPayment.Items)
                {
                    if (pmt.Items == null)
                        pmt.Items = new List<IncomingPaymentHeaderItem>();
                    var pmtItem = new IncomingPaymentHeaderItem
                    {
                        Payment = pmt,
                        IsEncrypted = item.IsSensitive,
                        Name = item.Code.ToString(),
                        Value = item.Value
                    };
                    FillInInfrastructureFields(pmtItem);
                    pmt.Items.Add(pmtItem);
                    context.AddIncomingPaymentHeaderItems(pmtItem);
                }

                bool wasPlaced = false;
                string notPlacedReasonsMessage = null;

                if (!skipAutoPlace && externalPayment.Match != null)
                {
                    var currentCustomerBalance = accountBalanceByCustomerId[externalPayment.Match.MainCustomerId];

                    int gracePeriodDaysDiff = (pmt.BookKeepingDate - externalPayment.Match.EventDate.Date).Days;
                    if (gracePeriodDaysDiff > incomingPaymentDepositeGracePeriodInDays && externalPayment.Match.AccountTypeCode == nameof(SavingsAccountTypeCode.FixedInterestAccount))
                    {
                        notPlacedReasonsMessage = "Customer fixed rate account has passed grace period";
                    }
                    else if (externalPayment.Match.Status != SavingsAccountStatusCode.Active.ToString())
                    {
                        notPlacedReasonsMessage = "Savings account is not active";
                    }
                    else if (currentCustomerBalance + externalPayment.Amount > maxAllowedSavingsCustomerBalance)
                    {
                        notPlacedReasonsMessage = "Would cause the customers balance to go above the max allowed balance";
                    }
                    else if (hasTransactionCheckpointByCustomerId[externalPayment.Match.MainCustomerId])
                    {
                        notPlacedReasonsMessage = "Customer checkpoint blocks transactions";
                    }
                    else
                    {
                        accountBalanceByCustomerId[externalPayment.Match.MainCustomerId] += externalPayment.Amount;
                        AddTransaction(context, LedgerAccountTypeCode.Capital, externalPayment.Amount, evt, externalPayment.BookKeepingDate,
                            savingsAccountNr: externalPayment.Match.SavingsAccountNr,
                            incomingPayment: pmt,
                            interestFromDate: externalPayment.BookKeepingDate.AddDays(1));
                        AddComment(
                            $"Deposit of {externalPayment.Amount.ToString("C", CommentFormattingCulture)} placed directly",
                            BusinessEventType.IncomingPaymentFileImport,
                            context, savingsAccountNr: externalPayment.Match.SavingsAccountNr);
                        wasPlaced = true;
                        placedCount += 1;
                    }
                    if (!wasPlaced && !string.IsNullOrWhiteSpace(notPlacedReasonsMessage) && !string.IsNullOrWhiteSpace(externalPayment.Match.SavingsAccountNr))
                    {
                        AddComment(
                            $"Deposit left unplaced because: {notPlacedReasonsMessage}",
                            BusinessEventType.IncomingPaymentFileImport,
                            context, savingsAccountNr: externalPayment.Match.SavingsAccountNr);
                    }
                }

                if (!wasPlaced)
                {
                    unplacedCount += 1;
                    AddTransaction(context, LedgerAccountTypeCode.UnplacedPayment, externalPayment.Amount, evt, externalPayment.BookKeepingDate,
                        incomingPayment: pmt);

                    if (notPlacedReasonsMessage == null)
                    {
                        notPlacedReasonsMessage = "No match on reference";
                    }

                    if (notPlacedReasonsMessage != null)
                    {
                        var notPlacedReasonsMessageItem = new IncomingPaymentHeaderItem
                        {
                            Payment = pmt,
                            IsEncrypted = false,
                            Name = IncomingPaymentHeaderItemCode.NotAutoPlacedReasonMessage.ToString(),
                            Value = notPlacedReasonsMessage
                        };
                        FillInInfrastructureFields(notPlacedReasonsMessageItem);
                        pmt.Items.Add(notPlacedReasonsMessageItem);
                        context.AddIncomingPaymentHeaderItems(notPlacedReasonsMessageItem);
                    }
                }

                pmt.IsFullyPlaced = wasPlaced;
            }

            var itemsToEncrypt = h.Payments.SelectMany(x => x.Items).Where(x => x.IsEncrypted).ToArray();

            encryptionService.SaveEncryptItems(itemsToEncrypt, x => x.Value, (x, encVal) => x.Value = encVal.ToString(), context);

            h.FileArchiveKey = documentClient.ArchiveStore(paymentfile.OriginalFileData, "application/xml", paymentfile.OriginalFileName);

            placementMessage = $"Placed: {placedCount}, Left unplaced: {unplacedCount}";

            return h;
        }

        private Dictionary<int, decimal> GetCurrentBalanceByCustomerId(ISavingsContext context, IList<int> customerIds)
        {
            var accountBalanceByCustomerId = new Dictionary<int, decimal>();
            foreach (var c in customerIds)
            {
                accountBalanceByCustomerId[c] = 0m;
            }

            var balances = context
                .SavingsAccountHeadersQueryable
                .Where(x => customerIds.Contains(x.MainCustomerId))
                .Select(x => new
                {
                    x.MainCustomerId,
                    Balance = x.Transactions.Where(y => y.AccountCode == LedgerAccountTypeCode.Capital.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m
                })
                .ToList();
            foreach (var b in balances)
            {
                accountBalanceByCustomerId[b.MainCustomerId] += b.Balance;
            }
            return accountBalanceByCustomerId;
        }

        public bool TryPlaceFromUnplaced(int paymentId, string savingsAccountNr, decimal placeAmount, decimal leaveUnplacedAmount, out string failedMessage)
        {
            var today = Clock.Today;

            placeAmount = Math.Round(placeAmount, 2);
            leaveUnplacedAmount = Math.Round(leaveUnplacedAmount, 2);
            using (var context = contextFactory.CreateContext())
            {
                var p = PaymentDomainModel.CreateForSinglePayment(paymentId, context, encryptionService);
                var currentUnplacedAmount = p.GetUnplacedAmount();

                if (currentUnplacedAmount - placeAmount != leaveUnplacedAmount)
                {
                    failedMessage = "Something has changed since the suggestion was created. Please retry";
                    return false;
                }

                if (placeAmount > currentUnplacedAmount)
                {
                    failedMessage = "place amount > current unplaced amount";
                    return false;
                }

                var a = context.SavingsAccountHeadersQueryable.SingleOrDefault(x => x.SavingsAccountNr == savingsAccountNr);
                if (a == null)
                {
                    failedMessage = "No such savings account";
                    return false;
                }
                if (a.Status != SavingsAccountStatusCode.Active.ToString())
                {
                    failedMessage = "Account is not active";
                    return false;
                }

                var ph = context.IncomingPaymentHeadersQueryable.Single(x => x.Id == paymentId);

                var evt = AddBusinessEvent(BusinessEventType.PlacementOfUnplacedPayment, context);

                AddTransaction(context, LedgerAccountTypeCode.UnplacedPayment, -placeAmount, evt, today,
                    incomingPayment: ph);

                AddTransaction(context, LedgerAccountTypeCode.Capital, placeAmount, evt, today,
                    savingsAccount: a,
                    incomingPayment: ph,
                    interestFromDate: ph.BookKeepingDate.AddDays(1));

                var statusAfterText = "";
                if (currentUnplacedAmount - placeAmount <= 0m)
                {
                    ph.IsFullyPlaced = true;
                    ph.ChangedById = UserId;
                    ph.InformationMetaData = InformationMetadata;
                    ph.ChangedDate = Clock.Now;
                }
                else
                {
                    var leftAmount = currentUnplacedAmount - placeAmount;
                    statusAfterText = $" Note that this was not the full deposit. {leftAmount.ToString("C", CommentFormattingCulture)} was left unplaced.";
                }

                AddComment(
                    $"Deposit of {placeAmount.ToString("C", CommentFormattingCulture)} placed from unplaced.{statusAfterText}",
                    BusinessEventType.PlacementOfUnplacedPayment,
                    context, savingsAccount: a);

                context.SaveChanges();

                failedMessage = null;
                return true;
            }
        }

        public bool TryRepayFromUnplaced(
                int paymentId,
                decimal repaymentAmount,
                decimal leaveUnplacedAmount,
                string customerName,
                string iban,
                out OutgoingPaymentHeader outgoingPayment,
                out string failedMessage)
        {
            repaymentAmount = Math.Round(repaymentAmount, 2);
            leaveUnplacedAmount = Math.Round(leaveUnplacedAmount, 2);

            using (var context = contextFactory.CreateContext())
            {
                context.BeginTransaction();
                try
                {
                    outgoingPayment = null;

                    if (repaymentAmount <= 0m)
                    {
                        failedMessage = "repaymentAmount must be > 0";
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(customerName))
                    {
                        failedMessage = "customerName is required";
                        return false;
                    }

                    if (leaveUnplacedAmount < 0m)
                    {
                        failedMessage = "leaveUnplacedAmount negative";
                        return false;
                    }

                    if (paymentId <= 0)
                    {
                        failedMessage = "Missing paymentId";
                        return false;
                    }

                    var today = Clock.Today;

                    //////////////////////////////////////////////////
                    // Fetch historical readonly data ////////////////
                    ///////////////////////////////////////////////////
                    PaymentDomainModel payment;
                    payment = PaymentDomainModel.CreateForSinglePayment(paymentId, context, encryptionService);

                    var unplacedAmount = payment.GetUnplacedAmount();

                    var balanceAfter = unplacedAmount - repaymentAmount;

                    if (balanceAfter < 0m || balanceAfter != leaveUnplacedAmount)
                    {
                        failedMessage = "Invalid repayment amount. Payment has balance: " + unplacedAmount.ToString(CommentFormattingCulture);
                        return false;
                    }

                    var evt = AddBusinessEvent(BusinessEventType.RepaymentOfUnplacedPayment, context);

                    var r = new OutgoingPaymentHeader
                    {
                        CreatedByEvent = evt,
                        TransactionDate = evt.TransactionDate
                    };
                    FillInInfrastructureFields(r);
                    context.AddOutgoingPaymentHeaders(r);

                    Action<OutgoingPaymentHeaderItemCode, string, bool> addItem = (name, value, isEncrypted) =>
                    {
                        var item = new OutgoingPaymentHeaderItem
                        {
                            ChangedById = UserId,
                            ChangedDate = Clock.Now,
                            IsEncrypted = isEncrypted,
                            InformationMetaData = InformationMetadata,
                            OutgoingPayment = r,
                            Name = name.ToString(),
                            Value = value
                        };
                        if (r.Items == null)
                            r.Items = new List<OutgoingPaymentHeaderItem>();
                        r.Items.Add(item);
                        context.AddOutgoingPaymentHeaderItems(item);
                    };
                    addItem(OutgoingPaymentHeaderItemCode.CustomerMessage, GetOutgoingPaymentFileCustomerMessage(envSettings, eventName: "Repayment"), false);
                    addItem(OutgoingPaymentHeaderItemCode.CustomerName, customerName, true);

                    var c = clientConfiguration.Country.BaseCountry;
                    if (c == "FI")
                    {
                        IBANFi ibanParsed;
                        if (!IBANFi.TryParse(iban, out ibanParsed))
                        {
                            failedMessage = "Invalid iban";
                            return false;
                        }
                        addItem(OutgoingPaymentHeaderItemCode.FromIban, envSettings.OutgoingPaymentIban.NormalizedValue, false);
                        addItem(OutgoingPaymentHeaderItemCode.ToIban, ibanParsed.NormalizedValue, false);
                    }
                    else
                        throw new NotImplementedException();

                    List<LedgerAccountTransaction> trs = new List<LedgerAccountTransaction>();

                    AddTransaction(context, LedgerAccountTypeCode.UnplacedPayment, -repaymentAmount, evt, today,
                        incomingPaymentId: paymentId);
                    AddTransaction(context, LedgerAccountTypeCode.ShouldBePaidToCustomer, repaymentAmount, evt, today,
                        outgoingPayment: r);

                    if (balanceAfter == 0m)
                    {
                        var p = context.IncomingPaymentHeadersQueryable.Single(x => x.Id == paymentId);
                        p.IsFullyPlaced = true;
                    }

                    var itemsToEncrypt = r.Items.Where(x => x.IsEncrypted == true).ToArray();
                    if (itemsToEncrypt.Length > 0)
                    {
                        encryptionService.SaveEncryptItems(
                            itemsToEncrypt,
                            x => x.Value,
                            (x, id) => x.Value = id.ToString(),
                            context);
                    }

                    failedMessage = null;
                    outgoingPayment = r;

                    context.SaveChanges();
                    context.CommitTransaction();

                    return true;
                }
                catch
                {
                    context.RollbackTransaction();
                    throw;
                }
            }
        }

        public static bool HasTransactionBlockCheckpoint(int customerId, ICustomerClient customerClient)
        {
            return HasTransactionBlockCheckpoint(new HashSet<int> { customerId }, customerClient)[customerId];
        }

        public static Dictionary<int, bool> HasTransactionBlockCheckpoint(HashSet<int> customerIds, ICustomerClient customerClient)
        {
            var checkPointByCustomerId = customerClient.GetActiveCheckpointIdsOnCustomerIds(
                customerIds,
                new List<string> { "SavingsAccountBlockTransactions" })?.CheckPointByCustomerId;
            return customerIds.ToDictionary(x => x, x => checkPointByCustomerId.ContainsKey(x));
        }

        public static string GetOutgoingPaymentFileCustomerMessage(ISavingsEnvSettings envSettings, string eventName = null, string contextNumber = null)
        {
            var pattern = envSettings.OutgoingPaymentFileCustomerMessagePattern ?? "{eventName} {contextNumber}";

            pattern = pattern.Replace("{eventName}", eventName ?? "");
            pattern = pattern.Replace("{contextNumber}", contextNumber ?? "");

            if (string.IsNullOrWhiteSpace(pattern))
                return null;
            else
                return pattern.Trim();
        }
    }
}