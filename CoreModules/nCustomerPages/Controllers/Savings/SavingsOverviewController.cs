﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using nCustomerPages.Code;
using Newtonsoft.Json;
using NTech.Banking.Shared.BankAccounts.Fi;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;
using Serilog;

namespace nCustomerPages.Controllers.Savings;

[RoutePrefix("savings")]
[CustomerPagesAuthorize(Roles = LoginProvider.SavingsCustomerRoleName)]
public class SavingsOverviewController : SavingsBaseController
{
    [Route("overview")]
    [PreventBackButton]
    public ActionResult Index(string newAccountNr = null)
    {
        ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
        {
            apiUrls = new
            {
                accountDetails = Url.Action("GetAccountDetails", "SavingsOverview"),
                accountTransactions = Url.Action("GetAccountTransactions", "SavingsOverview"),
                accountTransactionDetails = Url.Action("GetAccountTransactionDetails", "SavingsOverview"),
                accountInterestHistory = Url.Action("GetSavingsAccountInterestHistory", "SavingsOverview"),
                accounts = Url.Action("GetAccounts", "SavingsOverview"),
                withdrawals = Url.Action("GetWithdrawalsInitialData", "SavingsOverview"),
                deposits = Url.Action("GetDepositsInitialData", "SavingsOverview"),
                createWithdrawal = Url.Action("CreateWithdrawal", "SavingsOverview"),
                withdrawalaccounts = Url.Action("GetWithdrawalAccounts", "SavingsOverview"),
                withdrawalAccountChangeDocumentUrl =
                    Url.Action("GetWithdrawalAccountChangeDocument", "SavingsOverview"),
                accountdocuments = Url.Action("GetAccountDocuments", "SavingsOverview"),
                closeaccountpreviewdata = Url.Action("CloseAccountPreviewData", "SavingsOverview"),
                closeaccount = Url.Action("CloseAccount", "SavingsOverview")
            },
            productsOverviewUrl = Url.Action("Index", "ProductOverview"),
            translation = GetTranslations()
        })));

        AffiliateTrackingModel.SetupLandingPageOnNewAccountOpened(this, this.CustomerId, newAccountNr);

        return View();
    }

    [Route("overview/api/account/downloadwithdrawalaccountchangedocument")]
    [HttpGet()]
    public ActionResult GetWithdrawalAccountChangeDocument()
    {
        if (!System.IO.File.Exists(NEnv.SavingsAccountWithdrawalAccountChangeAgreementFilePath.FullName))
            return HttpNotFound();

        var r = new FileStreamResult(
            System.IO.File.OpenRead(NEnv.SavingsAccountWithdrawalAccountChangeAgreementFilePath.FullName),
            "application/pdf")
        {
            FileDownloadName = "SavingsAccountWithdrawalAccountChange.pdf"
        };
        return r;
    }

    [Route("overview/api/account/details")]
    [HttpPost]
    public ActionResult GetAccountDetails(string savingsAccountNr, int? maxTransactionsCount,
        int? startBeforeTransactionId)
    {
        var c = CreateCustomerLockedSavingsClient();
        var result = c.GetSavingsAccountDetails(savingsAccountNr, maxTransactionsCount, startBeforeTransactionId);
        return Json2(result);
    }

    [Route("overview/api/withdrawalaccounts")]
    [HttpPost]
    public ActionResult GetWithdrawalAccounts()
    {
        var c = CreateCustomerLockedSavingsClient();
        var result = c
            .GetWithdrawalAccounts()
            .SavingsAccounts
            .Where(x => x.Status == "Active")
            .Select(x =>
            {
                var withdrawalIbanReadable = x.WithdrawalIban;
                var withdrawalIbanBankName = "Unknown";

                if (IBANFi.TryParse(x.WithdrawalIban, out var f))
                {
                    withdrawalIbanReadable = f.GroupsOfFourValue;
                    withdrawalIbanBankName = InferBankNameFromIbanFi(f);
                }

                return new
                {
                    x.SavingsAccountNr,
                    x.AccountTypeCode,
                    WithdrawalIbanRaw = x.WithdrawalIban,
                    WithdrawalIbanReadable = withdrawalIbanReadable,
                    WithdrawalIbanBankName = withdrawalIbanBankName,
                    x.WithdrawalIbanDate
                };
            })
            .ToList();

        return Json2(new { SavingsAccounts = result });
    }

    [Route("overview/api/account/transactions")]
    [HttpPost]
    public ActionResult GetAccountTransactions(string savingsAccountNr, int? maxTransactionsCount,
        int? startBeforeTransactionId)
    {
        var c = CreateCustomerLockedSavingsClient();
        var transactions =
            c.GetEventOrderedSavingsAccountTransactions(savingsAccountNr, maxTransactionsCount,
                startBeforeTransactionId);
        return Json2(new { Transactions = transactions });
    }

    [Route("overview/api/account/transactiondetails")]
    [HttpPost]
    public ActionResult GetAccountTransactionDetails(int transactionId)
    {
        var c = CreateCustomerLockedSavingsClient();
        var d = c.GetSavingsAccountLedgerTransactionDetails(transactionId);
        return Json2(new { TransactionDetails = d });
    }

    [Route("overview/api/account/interesthistory")]
    [HttpPost]
    public ActionResult GetSavingsAccountInterestHistory(string savingsAccountNr)
    {
        var c = CreateCustomerLockedSavingsClient();
        var result = c.GetInterestHistory(savingsAccountNr);
        return Json2(result);
    }

    [Route("overview/api/accounts")]
    [HttpPost]
    public ActionResult GetAccounts()
    {
        var c = CreateCustomerLockedSavingsClient();
        var accountsResult = c.GetSavingsAccounts();
        return Json2(new
        {
            Accounts = accountsResult.Accounts,
            AreWithdrawalsSuspended = accountsResult.AreWithdrawalsSuspended
        });
    }

    [Route("overview/api/deposits")]
    [HttpPost]
    public ActionResult GetDepositsInitialData()
    {
        var c = CreateCustomerLockedSavingsClient();
        var result = c.GetDepositsInitialData();
        return Json2(new { Accounts = result?.Accounts });
    }

    [Route("overview/api/withdrawals")]
    [HttpPost]
    public ActionResult GetWithdrawalsInitialData()
    {
        var c = CreateCustomerLockedSavingsClient();
        var result = c.GetWithdrawalsInitialData();

        var accounts = result.Accounts.Select(x =>
        {
            var readableIban = x.ToIban;
            string bankName = null;

            if (x.ToIban != null && IBANFi.TryParse(x.ToIban, out var i))
            {
                readableIban = i.GroupsOfFourValue;
                bankName = InferBankNameFromIbanFi(i);
            }

            return new
            {
                x.SavingsAccountNr,
                x.AccountTypeCode,
                x.WithdrawableAmount,
                x.ToIban,
                x.MaturesAt,
                PenaltyFeesPercentage = (string.IsNullOrEmpty(Convert.ToString(x.MaturesAt)) ? 0 : ((x.AccountTypeCode == nameof(SavingsAccountTypeCode.FixedInterestAccount) &&
                                 Clock.Now.Date <= x.MaturesAt) ? Convert.ToDecimal(NEnv.PenaltyFees.Replace(".", ",")) : 0)),
                ToIbanFormatted = x.ToIban != null
                    ? new
                    {
                        ReadableIban = readableIban,
                        BankName = bankName
                    }
                    : null
            };
        }).ToList();

        return Json2(new
        {
            Accounts = accounts,
            result.UniqueOperationToken,
            result.AreWithdrawalsSuspended
        });
    }

    [Route("overview/api/createwithdrawal")]
    [HttpPost]
    public ActionResult CreateWithdrawal(string savingsAccountNr,
        string expectedToIban,
        decimal? withdrawalAmount,
        string uniqueOperationToken,
        string customCustomerMessageText,
        string customTransactionText,
        decimal? penaltyFees)
    {
        var requestAuthenticationMethod = this.User.Identity.AuthenticationType;
        var requestIpAddress = this.HttpContext?.GetOwinContext()?.Request?.RemoteIpAddress;

        var c = CreateCustomerLockedSavingsClient();
        if (c.TryCreateWithdrawal(
                savingsAccountNr, expectedToIban, withdrawalAmount,
                uniqueOperationToken, customCustomerMessageText, customTransactionText,
                requestAuthenticationMethod, requestIpAddress, penaltyFees, out var failedMessage, out var result))
        {
            return Json2(result);
        }

        Log.Warning("Withdrawal failed: {failedMessage}", failedMessage);
        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
    }

    [Route("overview/api/previewcloseaccount")]
    [HttpPost]
    public ActionResult CloseAccountPreviewData(string savingsAccountNr)
    {
        var c = CreateCustomerLockedSavingsClient();

        var result = c.GetCloseAccountPreviewData(savingsAccountNr);

        var readableIban = result.ToIban;
        string bankName = null;

        if (result.ToIban != null && IBANFi.TryParse(result.ToIban, out var i))
        {
            readableIban = i.GroupsOfFourValue;
            bankName = InferBankNameFromIbanFi(i);
        }

        return Json2(new
        {
            SavingsAccountNr = result.SavingsAccountNr,
            CapitalBalanceAmount = result.CapitalBalanceAmount,
            AccumulatedInterestAmount = result.AccumulatedInterestAmount,
            TaxAmount = result.TaxAmount,
            PaidOutAmount = result.PaidOutAmount,
            WithdrawalIbanFormatted = readableIban,
            WithdrawalIbanBankName = bankName,
            WithdrawalIbanRaw = result.ToIban,
            UniqueOperationToken = result.UniqueOperationToken
        });
    }

    [Route("overview/api/closeaccount")]
    [HttpPost]
    public ActionResult CloseAccount(
        string savingsAccountNr,
        string expectedToIban,
        string uniqueOperationToken,
        string customCustomerMessageText,
        string customTransactionText)
    {
        var requestAuthenticationMethod = this.User.Identity.AuthenticationType;
        var requestIpAddress = this.HttpContext?.GetOwinContext()?.Request?.RemoteIpAddress;

        var c = CreateCustomerLockedSavingsClient();
        if (c.TryCloseAccount(
                savingsAccountNr, expectedToIban,
                uniqueOperationToken, customCustomerMessageText, customTransactionText,
                requestAuthenticationMethod, requestIpAddress, out var failedMessage,
                out var newUniqueOperationToken))
        {
            return Json2(new
            {
                NewUniqueOperationToken = newUniqueOperationToken
            });
        }

        Log.Warning("Close account failed: {failedMessage}", failedMessage);
        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
    }

    [Route("overview/api/accountdocuments")]
    [HttpPost]
    public ActionResult GetAccountDocuments()
    {
        var c = CreateCustomerLockedSavingsClient();
        var yearlySummaries = c.GetYearlySummaries();
        var documents = c
            .GetSavingsAccountDocuments()
            ?.Documents
            ?.Select(x => new
            {
                DocumentId = $"R_{x.DocumentId}",
                DocumentType = $"R_{x.DocumentType}",
                x.SavingsAccountNr,
                DocumentDate = x.DocumentDate.DateTime,
                DocumentUrl = Url.Action("GetSavingsAccountDocument", "SavingsOverview",
                    new { documentId = x.DocumentId })
            })
            .Concat(yearlySummaries.Select(y => new
            {
                DocumentId = $"S_{y.Year}_{y.SavingsAccountNr}",
                DocumentType = "S_YearlySummary",
                SavingsAccountNr = y.SavingsAccountNr,
                DocumentDate = new DateTime(y.Year + 1, 1, 1).AddDays(-1),
                DocumentUrl = Url.Action("GetYearlySummary", "SavingsOverview",
                    new { savingsAccountNr = y.SavingsAccountNr, year = y.Year })
            }))
            .OrderByDescending(x => x.DocumentDate)
            .ThenByDescending(x => x.DocumentId)
            ?.ToList();

        return Json2(new { Documents = documents });
    }

    [Route("overview/api/accountdocument/{documentId}")]
    [HttpGet()]
    public ActionResult GetSavingsAccountDocument(int? documentId)
    {
        if (!documentId.HasValue)
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing documentId");
        var c = CreateCustomerLockedSavingsClient();
        if (!c.TryFetchSavingsAccountDocument(documentId.Value, out var contentType, out var fileName,
                out var content)) return HttpNotFound();
        var r = new FileStreamResult(new MemoryStream(content), contentType)
        {
            FileDownloadName = fileName
        };
        return r;
    }

    [Route("overview/api/yearlysummarydocument/{savingsAccountNr}/{year}")]
    [HttpGet()]
    public ActionResult GetYearlySummary(string savingsAccountNr, int? year)
    {
        if (string.IsNullOrWhiteSpace(savingsAccountNr))
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing savingsAccountNr");
        if (!year.HasValue)
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing year");
        var c = CreateCustomerLockedSavingsClient();
        if (!c.TryFetchYearlySummary(savingsAccountNr, year.Value, out var contentType, out var fileName,
                out var content)) return HttpNotFound();

        var r = new FileStreamResult(new MemoryStream(content), contentType)
        {
            FileDownloadName = fileName
        };
        return r;
    }
}