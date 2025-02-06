using nSavings.Code.Services;
using nSavings.DbModel.BusinessEvents;
using NTech.Banking.BankAccounts.Fi;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nSavings.Controllers
{
    [NTechApi]
    [NTechAuthorizeSavingsMiddle]
    public class ApiAccountClosureController : NController
    {
        [HttpPost]
        [Route("Api/SavingsAccount/InitialDataCloseAccount")]
        public ActionResult InitialDataCloseAccount(string savingsAccountNr)
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
                        x.MainCustomerId
                    })
                    .SingleOrDefault();
                if (h == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such account");

                return Json2(new
                {
                    Status = h.Status,
                    SavingsAccountNr = h.SavingsAccountNr,
                    Today = Clock.Today,
                    AreWithdrawalsSuspended = WithdrawalBusinessEventManager.HasTransactionBlockCheckpoint(h.MainCustomerId)
                });
            }
        }
        private AccountClosureBusinessEventManager CreateAccountClosureBusinessEventManager() =>
             new AccountClosureBusinessEventManager(this.CurrentUserId, this.InformationMetadata, Service.CustomerRelationsMerge);

        [HttpPost]
        [Route("Api/SavingsAccount/PreviewCloseAccount")]
        public ActionResult PreviewCloseAccount(string savingsAccountNr)
        {
            var mgr = CreateAccountClosureBusinessEventManager();

            IBANFi withdrawalIban;
            string failedMessage;
            if (!mgr.TryGetWithdrawalIban(savingsAccountNr, out withdrawalIban, out failedMessage))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            }

            AccountClosureBusinessEventManager.AccountClosurePreviewResult pr;
            if (mgr.TryPreviewCloseAccount(savingsAccountNr, allowCheckpoint: true, failedMessage: out failedMessage, result: out pr))
            {
                return Json2(new
                {
                    UniqueOperationToken = BusinessEventManagerBase.GenerateUniqueOperationKey(),
                    SavingsAccountNr = savingsAccountNr,
                    CloseDate = Clock.Today,
                    pr.CapitalizedInterest,
                    pr.CapitalBalanceBefore,
                    pr.WithdrawalAmount,
                    WithdrawalIban = new
                    {
                        Raw = withdrawalIban.NormalizedValue,
                        Formatted = withdrawalIban.GroupsOfFourValue,
                        BankName = InferBankNameFromIbanFi(withdrawalIban)
                    }
                });
            }
            else
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
        }

        [HttpPost]
        [Route("Api/SavingsAccount/CloseAccount")]
        public ActionResult CloseAccount(string savingsAccountNr, string uniqueOperationToken, bool? skipCalculationDetails)
        {
            var mgr = CreateAccountClosureBusinessEventManager();

            IBANFi withdrawalIban;
            string failedMessage;
            if (!mgr.TryGetWithdrawalIban(savingsAccountNr, out withdrawalIban, out failedMessage))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            }

            BusinessEvent evt;
            if (mgr.TryCloseAccount(new AccountClosureBusinessEventManager.AccountClosureRequest
            {
                ToIban = withdrawalIban.NormalizedValue,
                SavingsAccountNr = savingsAccountNr,
                UniqueOperationToken = uniqueOperationToken,
                IncludeCalculationDetails = !(skipCalculationDetails ?? false)
            }, false, true, out failedMessage, out evt))
            {
                return Json2(new
                {
                    businessEventId = evt.Id,
                    newUniqueOperationToken = BusinessEventManagerBase.GenerateUniqueOperationKey()
                });
            }
            else
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
        }
    }
}