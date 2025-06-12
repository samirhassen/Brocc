using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using nSavings.DbModel;
using nSavings.DbModel.BusinessEvents;
using NTech.Banking.Shared.BankAccounts.Fi;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Services.Infrastructure;

namespace nSavings.Controllers.Api
{
    [NTechApi]
    [NTechAuthorizeSavingsMiddle]
    public class ApiWithdrawalController : NController
    {
        [HttpPost]
        [Route("Api/SavingsAccount/WithdrawalInitialData")]
        public ActionResult WithdrawalInitialData(string savingsAccountNr)
        {
            using (var context = new SavingsContext())
            {
                var ac = context
                    .SavingsAccountHeaders
                    .Where(x => x.SavingsAccountNr == savingsAccountNr)
                    .Select(x => new
                    {
                        x.SavingsAccountNr,
                        x.Status,
                        WithdrawalIban = x
                            .DatedStrings
                            .Where(y => y.Name == DatedSavingsAccountStringCode.WithdrawalIban.ToString())
                            .OrderByDescending(y => y.BusinessEventId)
                            .Select(y => y.Value)
                            .FirstOrDefault(),
                        WithdrawableBalance =
                            x.Transactions.Where(y => y.AccountCode == LedgerAccountTypeCode.Capital.ToString())
                                .Sum(y => (decimal?)y.Amount) ?? 0m,
                        x.MainCustomerId
                    })
                    .FirstOrDefault();
                if (ac == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such acocunt");
                if (ac.WithdrawalIban == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest,
                        "Account has no associated withdrawal account");

                var iban = IBANFi.Parse(ac.WithdrawalIban);

                return Json2(new
                {
                    savingsAccountNr = ac.SavingsAccountNr,
                    status = ac.Status,
                    uniqueOperationToken = BusinessEventManagerBase.GenerateUniqueOperationKey(),
                    withdrawalIban = iban.NormalizedValue,
                    withdrawalIbanFormatted = iban.GroupsOfFourValue,
                    withdrawalIbanBankName = InferBankNameFromIbanFi(iban),
                    withdrawableBalance = ac.WithdrawableBalance,
                    areWithdrawalsSuspended =
                        WithdrawalBusinessEventManager.HasTransactionBlockCheckpoint(ac.MainCustomerId)
                });
            }
        }

        [HttpPost]
        [Route("Api/SavingsAccount/NewWithdrawal")]
        public ActionResult NewWithdrawal(string savingsAccountNr, decimal? amount, string uniqueOperationToken,
            string customCustomerMessageText, string customTransactionText)
        {
            var mgr = new WithdrawalBusinessEventManager(CurrentUserId, InformationMetadata);

            string withdrawalIban;
            using (var context = new SavingsContext())
            {
                var ac = context
                    .SavingsAccountHeaders
                    .Where(x => x.SavingsAccountNr == savingsAccountNr)
                    .Select(x => new
                    {
                        WithdrawalIban = x
                            .DatedStrings
                            .Where(y => y.Name == DatedSavingsAccountStringCode.WithdrawalIban.ToString())
                            .OrderByDescending(y => y.BusinessEventId)
                            .Select(y => y.Value)
                            .FirstOrDefault()
                    })
                    .FirstOrDefault();
                if (ac == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such acocunt");
                if (ac.WithdrawalIban == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest,
                        "Account has no associated withdrawal account");
                withdrawalIban = ac.WithdrawalIban;
            }

            if (mgr.TryCreateNew(new WithdrawalBusinessEventManager.WithdrawalRequest
                {
                    ToIban = withdrawalIban,
                    SavingsAccountNr = savingsAccountNr,
                    WithdrawalAmount = amount,
                    UniqueOperationToken = uniqueOperationToken,
                    CustomCustomerMessageText = customCustomerMessageText,
                    CustomTransactionText = customTransactionText,
                    RequestDate = Clock.Now,
                    RequestAuthenticationMethod = this.User.Identity.AuthenticationType,
                    RequestedByHandlerUserId = CurrentUserId,
                    RequestIpAddress = this.HttpContext?.GetOwinContext()?.Request?.RemoteIpAddress,
                    RequestedByCustomerId = null
                }, false, true, out var failedMessage, out var evt))
            {
                return Json2(new
                {
                    businessEventId = evt.Id,
                    newUniqueOperationToken = BusinessEventManagerBase.GenerateUniqueOperationKey()
                });
            }

            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
        }
    }
}