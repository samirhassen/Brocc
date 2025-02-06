using Newtonsoft.Json;
using nSavings.Code;
using nSavings.Code.Services;
using nSavings.DbModel.BusinessEvents;
using NTech.Banking.BankAccounts.Fi;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nSavings.Controllers
{
    [NTechApi]
    [NTechAuthorize()]
    public class ApiCustomerPagesController : NController
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            //If there are methods that are allowed to run without a customerid, put those in another controller or use a whitelist. Dont remove this check
            var customerId = filterContext.ActionParameters.FirstOrDefault(x => x.Key == "customerId").Value;
            if (customerId == null)
                filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");
            base.OnActionExecuting(filterContext);
        }

        [Route("Api/CustomerPages/FetchCustomerAddressFromTrustedSource")]
        [HttpPost]
        public ActionResult FetchCustomerAddressFromTrustedSource(string civicRegNumber, int customerId, List<string> itemNames)
        {
            var cc = new CreditReportClient();

            var result = cc.FetchNameAndAddress(
                NEnv.AddressProviderName,
                NEnv.BaseCivicRegNumberParser.Parse(civicRegNumber),
                itemNames,
                customerId);

            return Json2(new
            {
                IsSuccess = result.Success,
                Items = result.Success ? result.Items.ToDictionary(x => x.Name, x => x.Value) : null
            });
        }

        [HttpPost]
        [Route("Api/CustomerPages/WithdrawalAccounts")]
        public ActionResult WithdrawalAccounts(int? customerId)
        {
            if (!customerId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");
            }
            using (var context = new SavingsContext())
            {
                var savingsAccounts = context
                    .SavingsAccountHeaders
                    .Where(x => x.MainCustomerId == customerId.Value)
                    .OrderByDescending(x => x.CreatedByBusinessEventId)
                    .Select(x => new
                    {
                        x.SavingsAccountNr,
                        x.AccountTypeCode,
                        x.Status,
                        WithdrawalIbanItem = x
                            .DatedStrings
                            .Where(y => y.Name == DatedSavingsAccountStringCode.WithdrawalIban.ToString())
                            .OrderByDescending(y => y.BusinessEventId)
                            .FirstOrDefault()
                    })
                    .Select(x => new
                    {
                        x.SavingsAccountNr,
                        x.AccountTypeCode,
                        x.Status,
                        WithdrawalIban = x.WithdrawalIbanItem.Value,
                        WithdrawalIbanDate = (DateTime?)x.WithdrawalIbanItem.BusinessEvent.TransactionDate
                    })
                    .ToList();

                return Json2(new
                {
                    AreWithdrawalsSuspended = WithdrawalBusinessEventManager.HasTransactionBlockCheckpoint(customerId.Value),
                    SavingsAccounts = savingsAccounts
                });
            }
        }

        [HttpPost]
        [Route("Api/CustomerPages/YearlySummaries")]
        public ActionResult YearlySummaries(int? customerId)
        {
            if (!customerId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");
            }
            var summaries = this
                .Service
                .YearlySummary
                .GetAllYearsWithSummariesForCustomerAccounts(customerId.Value)
                .SelectMany(x => x.Value.Select(y => new
                {
                    SavingsAccountNr = x.Key,
                    Year = y
                }))
                .OrderByDescending(x => x.Year)
                .ThenBy(x => x.SavingsAccountNr)
                .ToList();

            return Json2(summaries);
        }

        [HttpPost]
        [Route("Api/CustomerPages/EventOrderedSavingsAccountTransactions")]
        public ActionResult EventOrderedSavingsAccountTransactions(int? customerId, string savingsAccountNr, int? maxCountTransactions, int? startBeforeTransactionId)
        {
            if (string.IsNullOrWhiteSpace(savingsAccountNr))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing savingsAccountNr");
            }
            if (!customerId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");
            }
            using (var context = new SavingsContext())
            {
                var transactions = GetCapitalTransactionsOrderedByEvent(context, customerId.Value, savingsAccountNr, maxCountTransactions, startBeforeTransactionId);
                return Json2(new
                {
                    Transactions = transactions
                });
            }
        }

        [HttpPost]
        [Route("Api/CustomerPages/LatestActiveSavingsAccountDetails")]
        public ActionResult LatestActiveSavingsAccountDetails(int? customerId, string accountTypeCode, int? maxTransactionsCount, int? startBeforeTransactionId)
        {
            return SavingsAccountDetailsI(customerId, null, accountTypeCode, true, maxTransactionsCount, startBeforeTransactionId);
        }

        [HttpPost]
        [Route("Api/CustomerPages/SavingsAccountDetails")]
        public ActionResult SavingsAccountDetails(int? customerId, string savingsAccountNr, int? maxTransactionsCount, int? startBeforeTransactionId)
        {
            return SavingsAccountDetailsI(customerId, savingsAccountNr, null, null, maxTransactionsCount, startBeforeTransactionId);
        }

        private class ExternalVariableItem
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        [HttpPost]
        [Route("Api/CustomerPages/SavingsAccountExternalVariables")]
        public ActionResult SavingsAccountExternalVariables(int? customerId, string savingsAccountNr)
        {
            if (string.IsNullOrWhiteSpace(savingsAccountNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing savingsAccountNr");

            using (var context = new SavingsContext())
            {
                var today = Clock.Today;
                var externalVariablesKey = ApiSavingsAccountDetailsController.GetSavingsAccountDetailsQueryable(context, today)
                    .Where(x => x.SavingsAccountNr == savingsAccountNr && x.MainCustomerId == customerId.Value)
                    .Select(x => x.ExternalVariablesKey).SingleOrDefault();

                List<ExternalVariableItem> externalVariables = null;

                if (externalVariablesKey != null)
                {
                    var kv = this.Service.KeyValueStore(GetCurrentUserMetadata());
                    var eternalVariablesRaw = kv.GetValue(externalVariablesKey, KeyValueStoreKeySpaceCode.SavingsExternalVariablesV1.ToString());
                    externalVariables = eternalVariablesRaw == null ? null : JsonConvert.DeserializeObject<List<ExternalVariableItem>>(eternalVariablesRaw);
                }

                return Json2(new
                {
                    savingsAccountNr = savingsAccountNr,
                    externalVariables = externalVariables
                });
            }
        }

        [HttpPost]
        [Route("Api/CustomerPages/SavingsAccounts")]
        public ActionResult SavingsAccounts(int? customerId)
        {
            if (!customerId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");
            }
            using (var context = new SavingsContext())
            {
                var accounts = ApiSavingsAccountDetailsController
                    .GetSavingsAccountDetailsQueryable(context, Clock.Today)
                    .Where(x => x.MainCustomerId == customerId.Value)
                    .Select(x => new
                    {
                        D = x,
                        CapitalBalance = (context
                        .SavingsAccountHeaders
                        .Where(y => y.SavingsAccountNr == x.SavingsAccountNr)
                        .SelectMany(y => y.Transactions)
                        .Where(y => y.AccountCode == LedgerAccountTypeCode.Capital.ToString())
                        .Sum(y => (decimal?)y.Amount) ?? 0m)
                    })
                    .OrderByDescending(x => x.D.CreatedByBusinessEventId)
                    .ToList()
                    .Select(x => new
                    {
                        x.D.SavingsAccountNr,
                        x.D.Status,
                        x.D.StatusDate,
                        CurrentInterestRatePercent = x.D.InterestRatePercent,
                        CapitalBalanceAmount = x.CapitalBalance,
                        AccountOpenedDate = x.D.CreatedTransactionDate,
                        x.D.AccountTypeCode
                    })
                    .ToList();
                return Json2(new
                {
                    AreWithdrawalsSuspended = WithdrawalBusinessEventManager.HasTransactionBlockCheckpoint(customerId.Value),
                    SavingsAccounts = accounts
                });
            }
        }

        private class WithdrawalSavingsAccountModel
        {
            public string SavingsAccountNr { get; set; }
            public string AccountTypeCode { get; set; }
            public string Status { get; set; }
            public string ToIban { get; set; }
            public int MainCustomerId { get; set; }
            public decimal WithdrawableAmount { get; set; }
        }

        private IQueryable<WithdrawalSavingsAccountModel> GetWithdrawalSavingsAccountModels(SavingsContext context)
        {
            return context
                .SavingsAccountHeaders
                .Select(x => new WithdrawalSavingsAccountModel
                {
                    SavingsAccountNr = x.SavingsAccountNr,
                    AccountTypeCode = x.AccountTypeCode,
                    MainCustomerId = x.MainCustomerId,
                    Status = x.Status,
                    ToIban = x
                        .DatedStrings
                        .Where(y => y.Name == DatedSavingsAccountStringCode.WithdrawalIban.ToString())
                        .OrderByDescending(y => y.BusinessEventId)
                        .Select(y => y.Value)
                        .FirstOrDefault(),
                    WithdrawableAmount = (x.Transactions.Where(y => y.AccountCode == LedgerAccountTypeCode.Capital.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m)
                });
        }

        [HttpPost]
        [Route("Api/CustomerPages/WithdrawalsInitialData")]
        public ActionResult WithdrawalsInitialData(int? customerId)
        {
            if (!customerId.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");
            using (var context = new SavingsContext())
            {
                var accounts = GetWithdrawalSavingsAccountModels(context)
                    .Where(x => x.MainCustomerId == customerId.Value && x.Status == SavingsAccountStatusCode.Active.ToString())
                    .Select(x => new
                    {
                        x.SavingsAccountNr,
                        x.AccountTypeCode,
                        x.ToIban,
                        x.WithdrawableAmount
                    })
                    .ToList();
                return Json2(new
                {
                    Accounts = accounts,
                    UniqueOperationToken = BusinessEventManagerBase.GenerateUniqueOperationKey(),
                    AreWithdrawalsSuspended = WithdrawalBusinessEventManager.HasTransactionBlockCheckpoint(customerId.Value)
                });
            }
        }

        [HttpPost]
        [Route("Api/CustomerPages/DepositsInitialData")]
        public ActionResult DepositsInitialData(int? customerId)
        {
            if (!customerId.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");
            using (var context = new SavingsContext())
            {
                var accounts =
                    ApiSavingsAccountDetailsController
                        .GetSavingsAccountDetailsQueryable(context, Clock.Today)
                        .Where(x => x.MainCustomerId == customerId.Value && x.Status == SavingsAccountStatusCode.Active.ToString())
                        .OrderByDescending(x => x.CreatedByBusinessEventId)
                        .Select(x => new
                        {
                            x.SavingsAccountNr,
                            x.AccountTypeCode,
                            x.OcrDepositReference,
                        })
                        .ToList()
                        .Select(x => new
                        {
                            x.SavingsAccountNr,
                            x.AccountTypeCode,
                            AccountDepositIban = NEnv.DepositsIban.NormalizedValue,
                            AccountDepositOcrReferenceNr = x.OcrDepositReference
                        })
                        .ToList();
                return Json2(new { Accounts = accounts });
            }
        }

        [HttpPost]
        [Route("Api/CustomerPages/SavingsAccountLedgerTransactionDetails")]
        public ActionResult SavingsAccountLedgerTransactionDetails(int? customerId, int transactionId)
        {
            if (!customerId.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");
            using (var context = new SavingsContext())
            {
                var tr = context
                    .LedgerAccountTransactions
                    .Where(x => x.SavingsAccount.MainCustomerId == customerId.Value && x.Id == transactionId)
                    .Select(x => new
                    {
                        x.Id,
                        x.SavingsAccountNr,
                        x.TransactionDate,
                        x.BookKeepingDate,
                        x.InterestFromDate,
                        x.Amount,
                        x.AccountCode,
                        x.BusinessEventRoleCode,
                        x.BusinessEvent.EventType,
                        IsConnectedToOutgoingPayment = x.OutgoingPaymentId.HasValue,
                        IsConnectedToIncomingPayment = x.IncomingPaymentId.HasValue,
                        OutgoingPaymentCustomTransactionMessage = x.OutgoingPayment
                            .Items
                            .Where(y => y.Name == OutgoingPaymentHeaderItemCode.CustomTransactionMessage.ToString() && !y.IsEncrypted)
                            .Select(y => y.Value)
                            .FirstOrDefault()
                    })
                    .SingleOrDefault();
                if (tr == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such transaction");
                return Json2(tr);
            }
        }

        [HttpPost]
        [Route("Api/CustomerPages/CreateWithdrawal")]
        public ActionResult CreateWithdrawal(
            int? customerId,
            string savingsAccountNr,
            string expectedToIban,
            decimal? withdrawalAmount,
            string uniqueOperationToken,
            string customCustomerMessageText,
            string customTransactionText,
            string requestAuthenticationMethod,
            string requestIpAddress)
        {
            if (string.IsNullOrWhiteSpace(savingsAccountNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing savingsAccountNr");
            if (!customerId.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");

            Func<WithdrawalSavingsAccountModel> fetchModel = () =>
               {
                   using (var context = new SavingsContext())
                   {
                       return GetWithdrawalSavingsAccountModels(context).SingleOrDefault(x => x.MainCustomerId == customerId.Value && x.SavingsAccountNr == savingsAccountNr);
                   }
               };

            var model = fetchModel();
            if (model == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such account");
            if (model.ToIban != expectedToIban)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "The default iban appears to have changed. Please relog and try again.");

            var mgr = new WithdrawalBusinessEventManager(CurrentUserId, InformationMetadata);
            BusinessEvent evt;
            string failedMessage;
            if (!mgr.TryCreateNew(new WithdrawalBusinessEventManager.WithdrawalRequest
            {
                SavingsAccountNr = savingsAccountNr,
                WithdrawalAmount = withdrawalAmount,
                CustomCustomerMessageText = customCustomerMessageText,
                CustomTransactionText = customTransactionText,
                ToIban = model.ToIban, //Never change this to expectedToIban since the user could potentially influence the expectedToIban
                UniqueOperationToken = uniqueOperationToken,
                RequestAuthenticationMethod = requestAuthenticationMethod,
                RequestIpAddress = requestIpAddress,
                RequestDate = Clock.Now,
                RequestedByCustomerId = customerId.Value,
                RequestedByHandlerUserId = null
            }, false, false, out failedMessage, out evt))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            }
            else
            {
                model = fetchModel();
                return Json2(new
                {
                    WithdrawableAmountAfter = model.WithdrawableAmount,
                    NewUniqueOperationToken = BusinessEventManagerBase.GenerateUniqueOperationKey()
                });
            }
        }

        [HttpPost]
        [Route("Api/CustomerPages/SavingsAccountInterestHistory")]
        public ActionResult SavingsAccountInterestHistory(int? customerId, string savingsAccountNr)
        {
            if (!customerId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");
            }
            using (var context = new SavingsContext())
            {
                var a = ApiSavingsAccountDetailsController
                     .GetSavingsAccountDetailsQueryable(context, Clock.Today)
                     .Where(x => x.MainCustomerId == customerId.Value && x.SavingsAccountNr == savingsAccountNr)
                     .Select(x => new { x.Status, x.CreatedByBusinessEventId, x.CreatedTransactionDate, x.StatusBusinessEventId, x.AccountTypeCode })
                     .SingleOrDefault();

                if (a == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such account");

                var closedBusinessEventId = a.Status == SavingsAccountStatusCode.Closed.ToString() ? a.StatusBusinessEventId : null;

                var interestRates = ChangeInterestRateBusinessEventManager
                    .GetSavingsAccountFilteredActiveInterestRates(context, savingsAccountNr, a.CreatedTransactionDate, closedBusinessEventId)
                    .Select(x => new
                    {
                        x.Id,
                        x.TransactionDate,
                        x.ValidFromDate,
                        x.InterestRatePercent,
                    })
                    .ToList();

                return Json2(new { InterestRates = interestRates });
            }
        }

        [HttpGet]
        [Route("Api/CustomerPages/SavingsAccountDocument")]
        public ActionResult FetchSavingsAccountDocument(int? documentId, int? customerId)
        {
            if (!documentId.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing documentId");
            if (!customerId.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");

            using (var context = new SavingsContext())
            {
                var archiveKey = context
                    .SavingsAccountDocuments
                    .Where(x => x.Id == documentId.Value && x.SavingsAccount.MainCustomerId == customerId.Value)
                    .Select(x => x.DocumentArchiveKey)
                    .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(archiveKey))
                    return HttpNotFound();

                var dc = new DocumentClient();
                byte[] content;
                string contentType;
                string fileName;
                if (dc.TryFetchRaw(archiveKey, out contentType, out fileName, out content))
                {
                    var r = new FileStreamResult(new MemoryStream(content), contentType);
                    r.FileDownloadName = fileName;
                    return r;
                }
                else
                    return HttpNotFound();
            }
        }

        [HttpGet]
        [Route("Api/CustomerPages/SavingsAccountYearlySummaryDocument")]
        public ActionResult SavingsAccountYearlySummaryDocument(int? customerId, string savingsAccountNr, int? year)
        {
            if (!customerId.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");
            if (string.IsNullOrWhiteSpace(savingsAccountNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing savingsAccountNr");
            if (!year.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing year");

            var s = this.Service.YearlySummary.CreateSummaryPdfWithOwnerCheck(savingsAccountNr, year.Value, customerId.Value);
            if (s == null)
                return HttpNotFound();

            return new FileStreamResult(s, "application/pdf")
            {
                FileDownloadName = $"YearlySummary_{savingsAccountNr}_{year.Value}.pdf"
            };
        }

        [HttpPost]
        [Route("Api/CustomerPages/SavingsAccountDocuments")]
        public ActionResult SavingsAccountDocuments(int? customerId)
        {
            if (!customerId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");
            }
            using (var context = new SavingsContext())
            {
                //NOTE: We dont use archive key here since we want to check the customerid when the user tries to download to prevent getting other users documents
                var result = context
                    .SavingsAccountDocuments
                    .Where(x => x.SavingsAccount.MainCustomerId == customerId.Value)
                    .OrderByDescending(x => x.Id).Select(x => new
                    {
                        DocumentId = x.Id,
                        x.SavingsAccountNr,
                        x.DocumentType,
                        x.DocumentDate
                    })
                    .ToList();

                return Json2(new
                {
                    documents = result
                });
            }
        }

        private AccountClosureBusinessEventManager CreateAccountClosureBusinessEventManager() =>
             new AccountClosureBusinessEventManager(this.CurrentUserId, this.InformationMetadata, Service.CustomerRelationsMerge);
        
        [HttpPost]
        [Route("Api/CustomerPages/CloseAccountPreviewData")]
        public ActionResult CloseAccountPreviewData(int? customerId, string savingsAccountNr)
        {
            if (!customerId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");
            }
            if (string.IsNullOrWhiteSpace(savingsAccountNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing savingsAccountNr");

            using (var context = new SavingsContext())
            {
                var a = ApiSavingsAccountDetailsController
                     .GetSavingsAccountDetailsQueryable(context, Clock.Today)
                     .Where(x => x.MainCustomerId == customerId.Value && x.SavingsAccountNr == savingsAccountNr)
                     .Select(x => new { x.Status })
                     .SingleOrDefault();

                if (a == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such account");

                if (a.Status != "Active")
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Account is not active");
            }

            var mgr = CreateAccountClosureBusinessEventManager();
            string failedMessage;

            AccountClosureBusinessEventManager.AccountClosurePreviewResult result;
            if (!mgr.TryPreviewCloseAccount(savingsAccountNr, false, out failedMessage, out result))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            }

            IBANFi iban;
            if (!mgr.TryGetWithdrawalIban(savingsAccountNr, out iban, out failedMessage))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            }

            return Json2(new
            {
                UniqueOperationToken = BusinessEventManagerBase.GenerateUniqueOperationKey(),
                SavingsAccountNr = savingsAccountNr,
                ToIban = iban.NormalizedValue,
                CapitalBalanceAmount = result.CapitalBalanceBefore,
                AccumulatedInterestAmount = result.CapitalizedInterest?.InterestAmount ?? 0m,
                TaxAmount = result.CapitalizedInterest?.ShouldBeWithheldForTaxAmount ?? 0m,
                PaidOutAmount = result.WithdrawalAmount
            });
        }

        [HttpPost]
        [Route("Api/CustomerPages/CloseAccount")]
        public ActionResult CloseAccount(
            int? customerId, string savingsAccountNr,
            string expectedToIban,
            string uniqueOperationToken, string customCustomerMessageText,
            string customTransactionText, string requestAuthenticationMethod,
            string requestIpAddress)
        {
            if (!customerId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");
            }
            if (string.IsNullOrWhiteSpace(savingsAccountNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing savingsAccountNr");

            using (var context = new SavingsContext())
            {
                var a = ApiSavingsAccountDetailsController
                     .GetSavingsAccountDetailsQueryable(context, Clock.Today)
                     .Where(x => x.MainCustomerId == customerId.Value && x.SavingsAccountNr == savingsAccountNr)
                     .Select(x => new { x.Status })
                     .SingleOrDefault();

                if (a == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such account");

                if (a.Status != "Active")
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Account is not active");
            }

            var mgr = CreateAccountClosureBusinessEventManager();

            IBANFi iban;
            string failedMessage;
            if (!mgr.TryGetWithdrawalIban(savingsAccountNr, out iban, out failedMessage))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            }

            if (iban.NormalizedValue != expectedToIban)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "The default iban appears to have changed. Please relog and try again.");

            BusinessEvent evt;

            var isOk = mgr.TryCloseAccount(new AccountClosureBusinessEventManager.AccountClosureRequest
            {
                UniqueOperationToken = uniqueOperationToken,
                SavingsAccountNr = savingsAccountNr,
                IncludeCalculationDetails = true,
                ToIban = iban.NormalizedValue,  //Never change this to expectedToIban since the user could potentially influence the expectedToIban
                CustomCustomerMessageText = customCustomerMessageText,
                CustomTransactionText = customTransactionText,
                RequestAuthenticationMethod = requestAuthenticationMethod,
                RequestIpAddress = requestIpAddress,
                RequestDate = Clock.Now,
                RequestedByCustomerId = customerId.Value,
                RequestedByHandlerUserId = null
            }, false, false, out failedMessage, out evt);

            if (!isOk)
            {
                Log.Warning($"Savings account {savingsAccountNr} could not be closed due to {failedMessage}");
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Account could not be closed.");
            }

            return Json2(new { NewUniqueOperationToken = BusinessEventManagerBase.GenerateUniqueOperationKey() });
        }

        private ActionResult SavingsAccountDetailsI(int? customerId, string savingsAccountNr, string accountTypeCode, bool? latestActive, int? maxTransactionsCount, int? startBeforeTransactionId)
        {
            if (!customerId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");
            }
            if (latestActive.GetValueOrDefault() && !string.IsNullOrWhiteSpace(savingsAccountNr))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Cannot specify both a savingsAccountNr and latestActive");
            }
            if (!latestActive.GetValueOrDefault() && string.IsNullOrWhiteSpace(savingsAccountNr))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Must specify one of savingsAccountNr and latestActive");
            }
            if (latestActive.GetValueOrDefault() && string.IsNullOrWhiteSpace(accountTypeCode))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Must specify accountTypeCode when using latestActive");
            }
            using (var context = new SavingsContext())
            {
                var accountDetailsPre = ApiSavingsAccountDetailsController
                    .GetSavingsAccountDetailsQueryable(context, Clock.Today)
                    .Where(x => x.MainCustomerId == customerId.Value)
                    .Select(x => new
                    {
                        D = x,
                        CapitalBalance = (context
                            .SavingsAccountHeaders
                            .Where(y => y.SavingsAccountNr == x.SavingsAccountNr)
                            .SelectMany(y => y.Transactions)
                            .Where(y => y.AccountCode == LedgerAccountTypeCode.Capital.ToString())
                            .Sum(y => (decimal?)y.Amount) ?? 0m)
                    });

                if (latestActive.GetValueOrDefault())
                {
                    accountDetailsPre = accountDetailsPre
                        .Where(x => x.D.Status == SavingsAccountStatusCode.Active.ToString() && x.D.AccountTypeCode == accountTypeCode)
                        .OrderByDescending(y => y.D.CreatedByBusinessEventId);
                }
                else
                {
                    accountDetailsPre = accountDetailsPre
                        .Where(x => x.D.SavingsAccountNr == savingsAccountNr);
                }

                var accountDetails = accountDetailsPre.FirstOrDefault();

                if (accountDetails == null)
                {
                    if (string.IsNullOrWhiteSpace(savingsAccountNr))
                    {
                        return Json2(new { Exists = false });
                    }
                    else
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such account");
                    }
                }

                IDictionary<string, YearlyInterestCapitalizationBusinessEventManager.ResultModel> accInterestResult;
                string accFailedMessage;
                decimal? accumulatedInterestAmount;
                if (YearlyInterestCapitalizationBusinessEventManager.TryComputeAccumulatedInterestAssumingAccountIsClosedToday(context, Clock, new List<string> { savingsAccountNr }, false, out accInterestResult, out accFailedMessage))
                {
                    accumulatedInterestAmount = accInterestResult.Single().Value.TotalInterestAmount;
                }
                else
                {
                    accumulatedInterestAmount = null;
                }

                var transactions = GetCapitalTransactionsOrderedByEvent(context, customerId.Value, savingsAccountNr, maxTransactionsCount, startBeforeTransactionId);

                return Json2(new
                {
                    Exists = true,
                    Details = new
                    {
                        SavingsAccountNr = accountDetails.D.SavingsAccountNr,
                        Status = accountDetails.D.Status,
                        StatusDate = accountDetails.D.StatusDate,
                        CapitalBalanceAmount = accountDetails.CapitalBalance,
                        CurrentInterestRatePercent = accountDetails.D.InterestRatePercent,
                        AccumulatedInterestAmount = accumulatedInterestAmount,
                        AccountOpenedDate = accountDetails.D.CreatedTransactionDate,
                        AccountDepositIban = NEnv.DepositsIban.NormalizedValue,
                        AccountDepositOcrReferenceNr = accountDetails.D.OcrDepositReference,
                    },
                    Transactions = transactions
                });
            }
        }

        public class SavingsAccountTransactionCustomerPagesModel
        {
            public long Id { get; set; }
            public int CreatedByEventId { get; set; }
            public string BusinessEventType { get; set; }
            public string BusinessEventRoleCode { get; set; }
            public DateTime? TransactionDate { get; set; }
            public DateTime? BookkeepingDate { get; set; }
            public DateTime? InterestFromDate { get; set; }
            public decimal? Amount { get; set; }
            public decimal? BalanceAfterAmount { get; set; }
        }

        private IList<SavingsAccountTransactionCustomerPagesModel> GetCapitalTransactionsOrderedByEvent(SavingsContext context, int customerId, string savingsAccountNr, int? maxCountTransactions, int? startBeforeTransactionId)
        {
            if (maxCountTransactions.HasValue && maxCountTransactions.Value <= 0)
                return new List<SavingsAccountTransactionCustomerPagesModel>(); //Allows callers to opt out of fetching transactions

            var transactionsBase = context
                .LedgerAccountTransactions
                .Where(x => x.SavingsAccount.MainCustomerId == customerId && x.SavingsAccountNr == savingsAccountNr && x.AccountCode == LedgerAccountTypeCode.Capital.ToString());

            IQueryable<LedgerAccountTransaction> transactionsPre;
            if (startBeforeTransactionId.HasValue)
                transactionsPre = transactionsBase.Where(x => x.Id < startBeforeTransactionId.Value);
            else
                transactionsPre = transactionsBase;

            var transactions = transactionsPre
                .OrderByDescending(y => y.BusinessEventId)
                .ThenByDescending(y => y.Id)
                .Select(x => new SavingsAccountTransactionCustomerPagesModel
                {
                    Id = x.Id,
                    CreatedByEventId = x.BusinessEvent.Id,
                    BusinessEventType = x.BusinessEvent.EventType,
                    BusinessEventRoleCode = x.BusinessEventRoleCode,
                    Amount = x.Amount,
                    InterestFromDate = x.InterestFromDate,
                    BookkeepingDate = x.BookKeepingDate,
                    TransactionDate = x.TransactionDate,
                    BalanceAfterAmount = (transactionsBase
                        .Where(y => y.BusinessEventId <= x.BusinessEventId && y.Id <= x.Id)
                        .Sum(y => (decimal?)y.Amount) ?? 0m)
                });

            if (maxCountTransactions.HasValue)
                transactions = transactions.Take(maxCountTransactions.Value);

            return transactions.ToList();
        }
    }
}