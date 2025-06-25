using System.Linq;
using System.Net;
using System.Web.Mvc;
using nSavings.Code.Services;
using nSavings.DbModel;
using nSavings.DbModel.BusinessEvents;
using NTech.Services.Infrastructure;

namespace nSavings.Controllers.Api
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
                    AreWithdrawalsSuspended =
                        WithdrawalBusinessEventManager.HasTransactionBlockCheckpoint(h.MainCustomerId)
                });
            }
        }

        private AccountClosureBusinessEventManager CreateAccountClosureBusinessEventManager() =>
            new AccountClosureBusinessEventManager(this.CurrentUserId, this.InformationMetadata,
                ControllerServiceFactory.CustomerRelationsMerge);

        [HttpPost]
        [Route("Api/SavingsAccount/PreviewCloseAccount")]
        public ActionResult PreviewCloseAccount(string savingsAccountNr)
        {
            var mgr = CreateAccountClosureBusinessEventManager();

            if (!mgr.TryGetWithdrawalIban(savingsAccountNr, out var withdrawalIban, out var failedMessage))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            }

            if (mgr.TryPreviewCloseAccount(savingsAccountNr, allowCheckpoint: true, failedMessage: out failedMessage,
                    result: out var previewResult))
            {
                return Json2(new
                {
                    UniqueOperationToken = BusinessEventManagerBase.GenerateUniqueOperationKey(),
                    SavingsAccountNr = savingsAccountNr,
                    CloseDate = Clock.Today,
                    previewResult.CapitalizedInterest,
                    previewResult.CapitalBalanceBefore,
                    previewResult.WithdrawalAmount,
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
        public ActionResult CloseAccount(string savingsAccountNr, string uniqueOperationToken,
            bool? skipCalculationDetails)
        {
            var mgr = CreateAccountClosureBusinessEventManager();

            if (!mgr.TryGetWithdrawalIban(savingsAccountNr, out var withdrawalIban, out var failedMessage))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            }

            if (mgr.TryCloseAccount(new AccountClosureBusinessEventManager.AccountClosureRequest
                {
                    ToIban = withdrawalIban.NormalizedValue,
                    SavingsAccountNr = savingsAccountNr,
                    UniqueOperationToken = uniqueOperationToken,
                    IncludeCalculationDetails = !(skipCalculationDetails ?? false)
                }, false, true, out failedMessage, out var evt))
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