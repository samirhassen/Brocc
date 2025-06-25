using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using nSavings.Code;
using nSavings.DbModel;
using nSavings.DbModel.BusinessEvents;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;
using NTech.Services.Infrastructure;

namespace nSavings.Controllers.Api;

[NTechApi]
public class ApiSavingsAccountDetailsController : NController
{
    public class SavingsAccountDetailsModel
    {
        public string SavingsAccountNr { get; set; }
        public int MainCustomerId { get; set; }
        public string InitialAgreementArchiveKey { get; set; }
        public string OcrDepositReference { get; set; }
        public DateTime? StatusDate { get; set; }
        public string Status { get; set; }
        public int? StatusBusinessEventId { get; set; }
        public string AccountTypeCode { get; set; }
        public DateTime CreatedTransactionDate { get; set; }
        public decimal? InterestRatePercent { get; set; }
        public int CreatedByBusinessEventId { get; set; }
        public int? PendingWithdrawalAccountChangeId { get; set; }
        public string ExternalVariablesKey { get; set; }
    }

    public static IQueryable<SavingsAccountDetailsModel> GetSavingsAccountDetailsQueryable(SavingsContext context,
        DateTime today)
    {
        var rates = ChangeInterestRateBusinessEventManager.GetPerAccountActiveInterestRates(context);
        var fRates = FixedAccountProductBusinessEventManager.GetActiveFixedRatesQueryable(context);

        return context
            .SavingsAccountHeaders
            .Select(acc => new SavingsAccountDetailsModel
            {
                SavingsAccountNr = acc.SavingsAccountNr,
                AccountTypeCode = acc.AccountTypeCode.ToString(),
                MainCustomerId = acc.MainCustomerId,
                CreatedByBusinessEventId = acc.CreatedByBusinessEventId,
                PendingWithdrawalAccountChangeId = acc
                    .SavingsAccountWithdrawalAccountChanges
                    .Where(y => !y.CommitedOrCancelledByEventId.HasValue)
                    .OrderByDescending(y => y.Id)
                    .Select(y => (int?)y.Id)
                    .FirstOrDefault(),
                InitialAgreementArchiveKey = acc
                    .DatedStrings
                    .Where(y => y.Name == nameof(DatedSavingsAccountStringCode.SignedInitialAgreementArchiveKey))
                    .OrderByDescending(y => y.BusinessEventId).Select(y => y.Value)
                    .FirstOrDefault(),
                OcrDepositReference = acc
                    .DatedStrings
                    .Where(y => y.Name == nameof(DatedSavingsAccountStringCode.OcrDepositReference))
                    .OrderByDescending(y => y.BusinessEventId).Select(y => y.Value)
                    .FirstOrDefault(),
                Status = acc.Status,
                StatusDate = acc
                    .DatedStrings
                    .Where(y => y.Name == nameof(DatedSavingsAccountStringCode.SavingsAccountStatus))
                    .OrderByDescending(y => y.BusinessEventId)
                    .Select(y => (DateTime?)y.TransactionDate)
                    .FirstOrDefault(),
                StatusBusinessEventId = acc
                    .DatedStrings
                    .Where(y => y.Name == nameof(DatedSavingsAccountStringCode.SavingsAccountStatus))
                    .OrderByDescending(y => y.BusinessEventId)
                    .Select(y => (int?)y.BusinessEventId)
                    .FirstOrDefault(),
                CreatedTransactionDate = acc.CreatedByEvent.TransactionDate,
                InterestRatePercent = acc.AccountTypeCode == nameof(SavingsAccountTypeCode.FixedInterestAccount)
                    ? fRates.Where(r => r.Id == acc.FixedInterestProduct)
                        .Select(r => r.InterestRatePercent)
                        .FirstOrDefault()
                    : rates
                        .Where(y => y.SavingsAccountNr == acc.SavingsAccountNr && y.ValidFromDate <= today)
                        .OrderByDescending(y => y.ValidFromDate)
                        .Select(y => (decimal?)y.InterestRatePercent)
                        .FirstOrDefault(),
                ExternalVariablesKey = acc
                    .DatedStrings
                    .Where(y => y.Name == nameof(DatedSavingsAccountStringCode.ExternalVariablesKey))
                    .OrderByDescending(y => y.BusinessEventId)
                    .Select(y => y.Value)
                    .FirstOrDefault(),
            });
    }

    [HttpPost]
    [Route("Api/SavingsAccount/Details")]
    public ActionResult Details(string savingsAccountNr)
    {
        if (string.IsNullOrWhiteSpace(savingsAccountNr))
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing savingsAccountNr");

        using var context = new SavingsContext();
        var today = Clock.Today;

        var accountData = GetSavingsAccountDetailsQueryable(context, today)
            .Where(x => x.SavingsAccountNr == savingsAccountNr)
            .Select(x => new
            {
                x.SavingsAccountNr,
                x.MainCustomerId,
                x.InitialAgreementArchiveKey,
                x.OcrDepositReference,
                x.Status,
                x.StatusDate,
                x.CreatedTransactionDate,
                x.InterestRatePercent,
                CapitalTransactions = context
                    .LedgerAccountTransactions
                    .Where(y => y.AccountCode == nameof(LedgerAccountTypeCode.Capital) &&
                                y.SavingsAccountNr == x.SavingsAccountNr)
                    .Select(y => new
                    {
                        y.BusinessEventId,
                        y.Id,
                        y.BusinessEvent.EventType,
                        y.BusinessEventRoleCode,
                        y.TransactionDate,
                        y.BookKeepingDate,
                        y.InterestFromDate,
                        y.Amount
                    })
                    .AsEnumerable(),
                x.PendingWithdrawalAccountChangeId
            }).SingleOrDefault();

        if (accountData == null)
        {
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such account");
        }

        var rt = accountData.CapitalTransactions.Sum(y => (decimal?)y.Amount) ?? 0m;
        var capitalTransactions = accountData
            .CapitalTransactions
            .OrderByDescending(x => x.BusinessEventId)
            .ThenByDescending(x => x.Id)
            .Select(x =>
            {
                var result = new
                {
                    id = x.Id,
                    businessEventId = x.BusinessEventId,
                    bookKeepingDate = x.BookKeepingDate,
                    transactionDate = x.TransactionDate,
                    interestFromDate = x.InterestFromDate,
                    amount = x.Amount,
                    eventType = x.EventType,
                    eventRoleCode = x.BusinessEventRoleCode,
                    balanceAfter = rt
                };
                rt = rt - x.Amount;
                return result;
            })
            .ToList();

        var details = new
        {
            savingsAccountNr = accountData.SavingsAccountNr,
            mainCustomerId = accountData.MainCustomerId,
            createdTransactionDate = accountData.CreatedTransactionDate,
            initialAgreementArchiveKey = accountData.InitialAgreementArchiveKey,
            initialAgreementArchiveLink = accountData.InitialAgreementArchiveKey == null
                ? null
                : Url.Action("ArchiveDocument", "ApiArchiveDocument",
                    new { key = accountData.InitialAgreementArchiveKey }),
            status = accountData.Status,
            interestRatePercent = accountData.InterestRatePercent,
            capitalBalance = capitalTransactions.Sum(x => (decimal?)x.amount) ?? 0m,
            statusDate = accountData.StatusDate,
            ocrDepositReference = accountData.OcrDepositReference,
            depositsIban = NEnv.DepositsIban,
            pendingWithdrawalAccountChangeId = accountData.PendingWithdrawalAccountChangeId,
            areWithdrawalsSuspended =
                WithdrawalBusinessEventManager.HasTransactionBlockCheckpoint(accountData.MainCustomerId)
        };

        decimal? accumulatedInterestAmount;
        if (YearlyInterestCapitalizationBusinessEventManager
            .TryComputeAccumulatedInterestAssumingAccountIsClosedToday(context, Clock,
                new List<string> { savingsAccountNr }, false,
                out var accInterestResult,
                out var accFailedMessage))
        {
            accumulatedInterestAmount = accInterestResult.Single().Value.TotalInterestAmount;
        }
        else
        {
            accumulatedInterestAmount = null;
        }

        var accumulatedInterest = new
        {
            accumulatedAmount = accumulatedInterestAmount,
            failedMessage = accFailedMessage,
            detailsExcelLink = Url.Action("CreateReport", "ApiSavingsAccountFutureInterestReport",
                new { savingsAccountNr = savingsAccountNr, format = "excel", toDate = today.AddDays(-1) })
        };

        return Json2(new
        {
            details = details,
            capitalTransactions,
            accumulatedInterest
        });
    }

    [HttpPost]
    [Route("Api/SavingsAccount/LedgerTransactionDetails")]
    public ActionResult SavingsAccountLedgerTransactionDetails(int transactionId)
    {
        var outgoingPaymentItemsToFetch = new[]
        {
            OutgoingPaymentHeaderItemCode.CustomTransactionMessage,
            OutgoingPaymentHeaderItemCode.CustomerMessage,
            OutgoingPaymentHeaderItemCode.RequestIpAddress,
            OutgoingPaymentHeaderItemCode.RequestAuthenticationMethod,
            OutgoingPaymentHeaderItemCode.RequestDate,
            OutgoingPaymentHeaderItemCode.RequestedByCustomerId,
            OutgoingPaymentHeaderItemCode.RequestedByHandlerUserId
        }.Select(x => x.ToString()).ToList();

        var incomingPaymentItemsToFetch = new[]
        {
            IncomingPaymentHeaderItemCode.NoteText,
            IncomingPaymentHeaderItemCode.OcrReference,
            IncomingPaymentHeaderItemCode.ExternalId,
            IncomingPaymentHeaderItemCode.ClientAccountIban,
            IncomingPaymentHeaderItemCode.CustomerName,
            IncomingPaymentHeaderItemCode.CustomerAddressCountry,
            IncomingPaymentHeaderItemCode.CustomerAddressStreetName,
            IncomingPaymentHeaderItemCode.CustomerAddressBuildingNumber,
            IncomingPaymentHeaderItemCode.CustomerAddressPostalCode,
            IncomingPaymentHeaderItemCode.CustomerAddressTownName,
            IncomingPaymentHeaderItemCode.CustomerAddressLines
        }.Select(x => x.ToString()).ToList();

        using var context = new SavingsContext();
        var tr = context
            .LedgerAccountTransactions
            .Where(x => x.Id == transactionId)
            .Select(x => new
            {
                x.Id,
                x.SavingsAccountNr,
                SavingsAccountMainCustomerId = (int?)x.SavingsAccount.MainCustomerId,
                x.TransactionDate,
                x.BookKeepingDate,
                x.InterestFromDate,
                x.Amount,
                x.AccountCode,
                x.BusinessEventRoleCode,
                x.BusinessEvent.EventType,
                IsConnectedToOutgoingPayment = x.OutgoingPaymentId.HasValue,
                IsConnectedToIncomingPayment = x.IncomingPaymentId.HasValue,
                OutgoingPaymentItems =
                    x.OutgoingPayment.Items.Where(y => outgoingPaymentItemsToFetch.Contains(y.Name)),
                IncomingPaymentItems =
                    x.IncomingPayment.Items.Where(y => incomingPaymentItemsToFetch.Contains(y.Name))
            })
            .SingleOrDefault();

        if (tr == null)
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such transaction");

        var encryptedIds = new HashSet<long>();

        var outgoingPaymentItems = tr.OutgoingPaymentItems?.ToList() ?? [];
        outgoingPaymentItems.Where(x => x.IsEncrypted).ToList()
            .ForEach(x => encryptedIds.Add(long.Parse(x.Value)));

        var incomingPaymentItems = tr.IncomingPaymentItems?.ToList() ?? [];
        incomingPaymentItems.Where(x => x.IsEncrypted).ToList()
            .ForEach(x => encryptedIds.Add(long.Parse(x.Value)));

        IDictionary<long, string> decryptedValues;
        if (encryptedIds.Any())
        {
            decryptedValues = EncryptionContext.Load(context, encryptedIds.ToArray(),
                NEnv.EncryptionKeys.AsDictionary());
        }
        else
        {
            decryptedValues = new Dictionary<long, string>();
        }

        var outgoingPaymentDetails = tr.IsConnectedToOutgoingPayment
            ? outgoingPaymentItems.ToDictionary(x => x.Name,
                x => x.IsEncrypted ? decryptedValues[long.Parse(x.Value)] : x.Value)
            : null;

        var incomingPaymentDetails = tr.IsConnectedToIncomingPayment
            ? incomingPaymentItems.ToDictionary(x => x.Name,
                x => x.IsEncrypted ? decryptedValues[long.Parse(x.Value)] : x.Value)
            : null;

        if (outgoingPaymentDetails?.ContainsKey(
                nameof(OutgoingPaymentHeaderItemCode.RequestedByHandlerUserId)) ?? false)
        {
            outgoingPaymentDetails["RequestedByHandlerUserName"] = GetUserDisplayNameByUserId(
                outgoingPaymentDetails[nameof(OutgoingPaymentHeaderItemCode.RequestedByHandlerUserId)]);
            outgoingPaymentDetails.Remove(nameof(OutgoingPaymentHeaderItemCode.RequestedByHandlerUserId));
        }

        return Json2(new
        {
            tr.Id,
            tr.SavingsAccountNr,
            tr.TransactionDate,
            tr.BookKeepingDate,
            tr.InterestFromDate,
            tr.Amount,
            tr.AccountCode,
            tr.BusinessEventRoleCode,
            BusinessEventType = tr.EventType,
            tr.IsConnectedToOutgoingPayment,
            tr.IsConnectedToIncomingPayment,
            OutgoingPaymentDetailsItems = outgoingPaymentDetails
                ?.Select(x => new { Name = x.Key, x.Value })
                .ToList(),
            IncomingPaymentDetailsItems = incomingPaymentDetails
                ?.Select(x => new { Name = x.Key, x.Value })
                .ToList()
        });
    }
}