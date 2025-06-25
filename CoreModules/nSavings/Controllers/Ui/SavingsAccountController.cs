using System.Linq;
using System.Web.Mvc;
using nSavings.Code;
using nSavings.DbModel;
using nSavings.DbModel.BusinessEvents;
using NTech.Services.Infrastructure;

namespace nSavings.Controllers.Ui;

[NTechAuthorizeSavingsMiddle]
public class SavingsAccountController : NController
{
    [Route("Ui/SavingsAccount")]
    public ActionResult Index(int? testUserId)
    {
        ViewBag.JsonInitialData = EncodeInitialData(new
        {
            today = Clock.Today,
            testUserId,
            searchUrl = Url.Action("Search", "ApiSavingsAccountSearch"),
            detailsUrl = Url.Action("Details", "ApiSavingsAccountDetails"),
            savingsAccountLedgerTransactionDetailsUrl = Url.Action("SavingsAccountLedgerTransactionDetails",
                "ApiSavingsAccountDetails"),
            customerUrl = Url.Action("CustomerDetails", "ApiSavingsAccountCustomerDetails"),
            interestHistoryUrl = Url.Action("InterestHistory", "ApiSavingsAccountInterestHistory"),
            getWithdrawalInitialDataUrl = Url.Action("WithdrawalInitialData", "ApiWithdrawal"),
            createWithdrawalUrl = Url.Action("NewWithdrawal", "ApiWithdrawal"),
            initialDataCloseAccountUrl = Url.Action("InitialDataCloseAccount", "ApiAccountClosure"),
            previewCloseAccountUrl = Url.Action("PreviewCloseAccount", "ApiAccountClosure"),
            closeAccountAccountUrl = Url.Action("CloseAccount", "ApiAccountClosure"),
            initialDataWithdrawalAccountUrl =
                Url.Action("InitialDataWithdrawalAccount", "ApiChangeWithdrawalAccount"),
            initialDataWithdrawalAccountChangeUrl =
                Url.Action("InitialDataWithdrawalAccountChange", "ApiChangeWithdrawalAccount"),
            previewWithdrawalAccountChangeUrl =
                Url.Action("PreviewWithdrawalAccountChange", "ApiChangeWithdrawalAccount"),
            initiateChangeWithdrawalAccountUrl =
                Url.Action("InitiateChangeWithdrawalAccount", "ApiChangeWithdrawalAccount"),
            fetchCustomerItemsUrl = Url.Action("FetchCustomerItems", "ApiSavingsAccountCustomerDetails"),
            commitChangeWithdrawalAccountUrl =
                Url.Action("CommitChangeWithdrawalAccount", "ApiChangeWithdrawalAccount"),
            cancelChangeWithdrawalAccountUrl =
                Url.Action("CancelChangeWithdrawalAccount", "ApiChangeWithdrawalAccount"),
            initialDataDocumentsUrl = Url.Action("DocumentsInitialData", "ApiDocuments")
        });
        return View();
    }

    [Route("Ui/SavingsAccounts/Goto")]
    public ActionResult GotoSavingsAccount(string savingsAccountNr)
    {
        return Redirect(Url.Action("Index", new { }) + $"/#!/Details/{savingsAccountNr}");
    }

    [Route("Ui/SavingsAccounts/ChangeExternalAccountManagement")]
    public ActionResult ChangeExternalAccountManagement(int? testUserId)
    {
        var currentUserId = NEnv.IsProduction ? CurrentUserId : (testUserId ?? CurrentUserId);
        using var context = new SavingsContext();
        var pendingChanges = ChangeWithdrawalAccountBusinessEventManager
            .GetWithdrawalAccountHistoryQuery(context)
            .OrderByDescending(x => x.InitiatedBusinessEventId)
            .Where(x => x.IsPending)
            .Select(x => new
            {
                x.SavingsAccountNr,
                x.InitiatedByUserId,
                x.InitiatedTransactionDate
            })
            .ToList()
            .Select(x => new
            {
                x.SavingsAccountNr,
                InitiatedByUserDisplayName = GetUserDisplayNameByUserId(x.InitiatedByUserId.ToString()),
                x.InitiatedTransactionDate,
                WasInitiatedByCurrentUser = x.InitiatedByUserId == currentUserId,
                SavingsAccountUrl = GetSavingsAccountLink(x.SavingsAccountNr)
            })
            .ToList();

        ViewBag.JsonInitialData = EncodeInitialData(new
        {
            today = Clock.Today,
            testUserId,
            pendingChanges
        });
        return View();
    }
}