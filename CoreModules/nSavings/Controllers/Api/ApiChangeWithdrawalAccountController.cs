using System.Linq;
using System.Net;
using System.Web.Mvc;
using nSavings.Code;
using nSavings.DbModel;
using nSavings.DbModel.BusinessEvents;
using NTech.Banking.Shared.BankAccounts.Fi;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;
using NTech.Services.Infrastructure;

namespace nSavings.Controllers.Api
{
    [NTechApi, NTechAuthorizeSavingsMiddle]
    public class ApiChangeWithdrawalAccountController : NController
    {
        private object GetAccountsViewModel(SavingsContext context, string savingsAccountNr, int currentUserId)
        {
            return ChangeWithdrawalAccountBusinessEventManager
                .GetWithdrawalAccountHistoryQuery(context)
                .Where(x => x.SavingsAccountNr == savingsAccountNr)
                .ToList()
                .OrderByDescending(x => x.InitiatedBusinessEventId)
                .Select(x => FormatAccountForUi(x, currentUserId))
                .ToList();
        }

        [HttpPost, Route("Api/SavingsAccount/InitialDataWithdrawalAccount")]
        public ActionResult InitialDataWithdrawalAccount(string savingsAccountNr)
        {
            var currentUserId = GetCurrentUserIdWithTestSupport();

            using (var context = new SavingsContext())
            {
                var h = context
                    .SavingsAccountHeaders
                    .Where(x => x.SavingsAccountNr == savingsAccountNr)
                    .Select(x => new
                    {
                        x.Status,
                        x.SavingsAccountNr,
                        PendingWithdrawalAccountChangeId = x
                            .SavingsAccountWithdrawalAccountChanges
                            .Where(y => !y.CommitedOrCancelledByEventId.HasValue)
                            .OrderBy(y => y.Id)
                            .Select(y => (int?)y.Id)
                            .FirstOrDefault()
                    })
                    .SingleOrDefault();

                if (h == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such account");

                return Json2(new
                {
                    Status = h.Status,
                    SavingsAccountNr = h.SavingsAccountNr,
                    Today = Clock.Today,
                    HistoricalWithdrawalAccounts = GetAccountsViewModel(context, savingsAccountNr, currentUserId),
                    PendingWithdrawalAccountChangeId = h.PendingWithdrawalAccountChangeId
                });
            }
        }

        [HttpPost]
        [Route("Api/SavingsAccount/InitialDataWithdrawalAccountChange")]
        public ActionResult InitialDataWithdrawalAccountChange(string savingsAccountNr)
        {
            var currentUserId = GetCurrentUserIdWithTestSupport();

            using (var context = new SavingsContext())
            {
                var h = context
                    .SavingsAccountHeaders
                    .Where(x => x.SavingsAccountNr == savingsAccountNr)
                    .Select(x => new
                    {
                        x.Status,
                        x.SavingsAccountNr
                    })
                    .SingleOrDefault();

                if (h == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such account");

                var account = ChangeWithdrawalAccountBusinessEventManager
                    .GetWithdrawalAccountHistoryQuery(context)
                    .Where(x => x.SavingsAccountNr == savingsAccountNr && x.IsCommited)
                    .OrderByDescending(x => x.InitiatedBusinessEventId)
                    .FirstOrDefault();

                return Json2(new
                {
                    Status = h.Status,
                    SavingsAccountNr = h.SavingsAccountNr,
                    Today = Clock.Today,
                    CurrentWithdrawalAccount = account == null ? null : FormatAccountForUi(account, currentUserId),
                    HistoricalWithdrawalAccounts = GetAccountsViewModel(context, savingsAccountNr, currentUserId)
                });
            }
        }

        [HttpPost]
        [Route("Api/SavingsAccount/PreviewWithdrawalAccountChange")]
        public ActionResult PreviewWithdrawalAccountChange(string savingsAccountNr, string newWithdrawalIban)
        {
            using (var context = new SavingsContext())
            {
                var h = context
                    .SavingsAccountHeaders
                    .Where(x => x.SavingsAccountNr == savingsAccountNr)
                    .Select(x => new
                    {
                        x.Status,
                        x.SavingsAccountNr,
                        HasPendingChange =
                            x.SavingsAccountWithdrawalAccountChanges.Any(y => !y.CommitedOrCancelledByEventId.HasValue)
                    })
                    .SingleOrDefault();

                if (h == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such account");

                if (h.HasPendingChange)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "There is already a pending change");

                if (h.Status != SavingsAccountStatusCode.Active.ToString() &&
                    h.Status != SavingsAccountStatusCode.FrozenBeforeActive.ToString())
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid savings account status");

                if (string.IsNullOrWhiteSpace(newWithdrawalIban))
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing newWithdrawalIban");

                if (!IBANFi.TryParse(newWithdrawalIban, out var newIbanParsed))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid newWithdrawalIban");
                }

                var currentWithdrawalIban = ChangeWithdrawalAccountBusinessEventManager
                    .GetWithdrawalAccountHistoryQuery(context)
                    .Where(x => x.SavingsAccountNr == savingsAccountNr && x.IsCommited)
                    .OrderByDescending(x => x.InitiatedBusinessEventId)
                    .Select(x => x.WithdrawalAccount)
                    .FirstOrDefault();

                if (currentWithdrawalIban != null &&
                    IBANFi.TryParse(currentWithdrawalIban, out var currentIbanParsed) &&
                    newIbanParsed.NormalizedValue == currentIbanParsed.NormalizedValue)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest,
                        "New account is same as the current account");
                }

                return Json2(new
                {
                    SavingsAccountNr = h.SavingsAccountNr,
                    WithdrawalAccount = FormatWithdrawalAccountForUi(newIbanParsed.NormalizedValue),
                    InitiatedDate = Clock.Today
                });
            }
        }

        [HttpPost]
        [Route("Api/SavingsAccount/InitiateChangeWithdrawalAccount")]
        public ActionResult InitiateChangeWithdrawalAccount(string savingsAccountNr, string newWithdrawalIban,
            string letterOfAttorneyFileName, string letterOfAttorneyFileAsDataUrl)
        {
            var currentUserId = GetCurrentUserIdWithTestSupport();

            if (string.IsNullOrWhiteSpace(savingsAccountNr))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing savingsAccountNr");
            }

            if (string.IsNullOrWhiteSpace(newWithdrawalIban))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing newWithdrawalIban");
            }

            var mgr = new ChangeWithdrawalAccountBusinessEventManager(currentUserId, InformationMetadata);

            if (!IBANFi.TryParse(newWithdrawalIban, out var ibanParsed))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid newWithdrawalIban");
            }

            string attachedFileArchiveDocumentKey = null;
            if (!string.IsNullOrWhiteSpace(letterOfAttorneyFileAsDataUrl) &&
                !string.IsNullOrWhiteSpace(letterOfAttorneyFileName))
            {
                if (!TryParseDataUrl(letterOfAttorneyFileAsDataUrl, out var mimeType, out var fileData))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid letter of attorney file");
                }

                var client = new DocumentClient();
                attachedFileArchiveDocumentKey = client.ArchiveStore(fileData, mimeType, letterOfAttorneyFileName);
            }

            if (!mgr.TryInitiateChangeWithdrawalIbanFi(savingsAccountNr, ibanParsed, attachedFileArchiveDocumentKey,
                    out var failedMessage, out var result))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            }

            using (var context = new SavingsContext())
            {
                return Json2(new
                {
                    Id = result.Id,
                    HistoricalWithdrawalAccounts = GetAccountsViewModel(context, savingsAccountNr, currentUserId)
                });
            }
        }

        [HttpPost]
        [Route("Api/SavingsAccount/CommitChangeWithdrawalAccount")]
        public ActionResult CommitChangeWithdrawalAccount(int? pendingChangeId)
        {
            var currentUserId = GetCurrentUserIdWithTestSupport();

            if (!pendingChangeId.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing pendingChangeId");

            var mgr = new ChangeWithdrawalAccountBusinessEventManager(currentUserId, InformationMetadata);
            if (!mgr.TryCommitChangeWithdrawalIbanFi(pendingChangeId.Value, out var failedMessage, out var change))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);

            using (var context = new SavingsContext())
            {
                var accounts = GetAccountsViewModel(context, change.SavingsAccountNr, currentUserId);
                return Json2(new
                {
                    HistoricalWithdrawalAccounts = accounts
                });
            }
        }

        [HttpPost]
        [Route("Api/SavingsAccount/CancelChangeWithdrawalAccount")]
        public ActionResult CancelChangeWithdrawalAccount(int? pendingChangeId)
        {
            var currentUserId = GetCurrentUserIdWithTestSupport();

            if (!pendingChangeId.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing pendingChangeId");

            var mgr = new ChangeWithdrawalAccountBusinessEventManager(currentUserId, InformationMetadata);
            if (!mgr.TryCancelChangeWithdrawalIbanFi(pendingChangeId.Value, out var failedMessage, out var change))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);

            using (var context = new SavingsContext())
            {
                var accounts = GetAccountsViewModel(context, change.SavingsAccountNr, currentUserId);
                return Json2(new
                {
                    HistoricalWithdrawalAccounts = accounts
                });
            }
        }

        private object FormatWithdrawalAccountForUi(string a)
        {
            var rawIban = a;
            var formattedIban = a;
            var bankName = "Unknown";
            if (IBANFi.TryParse(a, out var iban))
            {
                rawIban = iban.NormalizedValue;
                formattedIban = iban.GroupsOfFourValue;
                bankName = InferBankNameFromIbanFi(iban);
            }

            return new
            {
                Raw = rawIban,
                Formatted = formattedIban,
                BankName = bankName
            };
        }

        private object FormatAccountForUi(ChangeWithdrawalAccountBusinessEventManager.AccountChangeHistoryModel x,
            int currentUserId)
        {
            return new
            {
                x.IsPending,
                x.PendingChangeId,
                x.IsCommited,
                x.IsCancelled,
                InitiatedTransactionDate = x.InitiatedTransactionDate,
                InitiatedByUserDisplayName = GetUserDisplayNameByUserId(x.InitiatedByUserId.ToString()),
                CancelledTransactionDate = x.IsCancelled ? x.CommittedOrCancelledDate : null,
                CancelledByUserDisplayName = x.IsCancelled
                    ? GetUserDisplayNameByUserId(x.CommittedOrCancelledByUserId?.ToString())
                    : null,
                CommitedTransactionDate = x.IsCommited ? x.CommittedOrCancelledDate : null,
                CommitedByUserDisplayName = x.IsCommited
                    ? GetUserDisplayNameByUserId(x.CommittedOrCancelledByUserId?.ToString())
                    : null,
                WithdrawalAccount = FormatWithdrawalAccountForUi(x.WithdrawalAccount),
                x.PowerOfAttorneyDocumentArchiveKey,
                PowerOfAttorneyDocumentArchiveLink = x.PowerOfAttorneyDocumentArchiveKey == null
                    ? null
                    : Url.Action("ArchiveDocument", "ApiArchiveDocument",
                        new { key = x.PowerOfAttorneyDocumentArchiveKey, setFileDownloadName = true }),
                WasInitiatedByCurrentUser = x.InitiatedByUserId == currentUserId
            };
        }
    }
}