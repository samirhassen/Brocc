using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using nSavings.Code;
using nSavings.Code.Services;
using nSavings.DbModel;
using NTech.Banking.BankAccounts.Se;
using NTech.Banking.Shared.BankAccounts.Fi;
using NTech.Core.Savings.Shared.BusinessEvents;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using Serilog;

namespace nSavings.Controllers.Api
{
    [NTechApi]
    public class ApiUnplacedPaymentsController : NController
    {
        [HttpPost]
        [Route("Api/UnplacedPayments/FindByReferenceNrOrSavingsAccountNr")]
        public ActionResult FindByReferenceNrOrSavingsAccountNr(string searchString)
        {
            searchString = searchString?.Trim();
            using (var context = new SavingsContext())
            {
                var ocrCode = DatedSavingsAccountStringCode.OcrDepositReference.ToString();
                var savingsAccountNrs = context
                    .SavingsAccountHeaders.Where(x =>
                        x.SavingsAccountNr == searchString ||
                        x.DatedStrings.Any(y => y.Name == ocrCode && y.Value == searchString))
                    .Select(x => x.SavingsAccountNr)
                    .ToList();

                if (savingsAccountNrs.Count == 0)
                {
                    return Json2(new { isOk = false, failedMessage = "No hits" });
                }

                if (savingsAccountNrs.Count > 1)
                {
                    return Json2(new
                    {
                        isOk = false,
                        failedMessage =
                            "These savings accounts share the same reference nr. Select one using the savingsAccountNr instead: " +
                            string.Join(", ", savingsAccountNrs)
                    });
                }

                var savingsAccountNr = savingsAccountNrs.Single();

                return Json2(new
                {
                    isOk = true,
                    savingsAccountNr = savingsAccountNr
                });
            }
        }

        [HttpPost]
        [Route("Api/UnplacedPayments/RepayPayment")]
        public ActionResult RepayPayment(
            int paymentId,
            string customerName,
            decimal repaymentAmount,
            decimal leaveUnplacedAmount,
            string iban)
        {
            try
            {
                var user = GetCurrentUserMetadata();
                var resolver = Service;
                var customerClient =
                    LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceHttpContextUser.SharedInstance,
                        NEnv.ServiceRegistry);
                var mgr = new NewIncomingPaymentFileBusinessEventManager(user, CoreClock.SharedInstance,
                    NEnv.ClientCfgCore,
                    NEnv.EnvSettings, ControllerServiceFactory.GetEncryptionService(user), resolver.ContextFactory,
                    customerClient);
                return !mgr.TryRepayFromUnplaced(paymentId, repaymentAmount, leaveUnplacedAmount, customerName, iban,
                    out _, out var msg)
                    ? new HttpStatusCodeResult(HttpStatusCode.BadRequest, msg)
                    : new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Error repaying payment");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        [Route("Api/UnplacedPayments/PlacePayment")]
        public ActionResult PlacePayment(
            int paymentId,
            string savingsAccountNr,
            decimal placeAmount,
            decimal leaveUnplacedAmount)
        {
            try
            {
                var user = GetCurrentUserMetadata();
                var resolver = Service;
                var customerClient =
                    LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceHttpContextUser.SharedInstance,
                        NEnv.ServiceRegistry);
                var mgr = new NewIncomingPaymentFileBusinessEventManager(user, CoreClock.SharedInstance,
                    NEnv.ClientCfgCore,
                    NEnv.EnvSettings, ControllerServiceFactory.GetEncryptionService(user), resolver.ContextFactory,
                    customerClient);
                return !mgr.TryPlaceFromUnplaced(paymentId, savingsAccountNr, placeAmount, leaveUnplacedAmount,
                    out var msg)
                    ? new HttpStatusCodeResult(HttpStatusCode.BadRequest, msg)
                    : new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Error placing payment");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        [Route("Api/UnplacedPayments/PaymentPlacementSuggestion")]
        public ActionResult PaymentPlacementSuggestion(string savingsAccountNr, decimal placeAmount, int paymentId,
            bool? allowOverMaxAllowedSavingsCustomerBalance)
        {
            if (string.IsNullOrWhiteSpace(savingsAccountNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing savingsAccountNr");

            using (var context = new SavingsContext())
            {
                var p = PaymentDomainModel.CreateForSinglePayment(paymentId, context,
                    ControllerServiceFactory.GetEncryptionService(GetCurrentUserMetadata()));

                var a = context
                    .SavingsAccountHeaders
                    .Where(x => x.SavingsAccountNr == savingsAccountNr)
                    .Select(x => new
                    {
                        x.Status,
                        x.SavingsAccountNr,
                        CustomerBalance = context
                            .SavingsAccountHeaders
                            .Where(y => y.MainCustomerId == x.MainCustomerId)
                            .SelectMany(y => y.Transactions)
                            .Where(y => y.AccountCode == LedgerAccountTypeCode.Capital.ToString())
                            .Sum(y => (decimal?)y.Amount) ?? 0m
                    })
                    .SingleOrDefault();

                if (a == null) return Fail("Savings account does not exist");
                if (a.Status != SavingsAccountStatusCode.Active.ToString())
                    return Fail($"Cannot place payments against a savings account with status: {a.Status}");

                placeAmount = Math.Round(placeAmount, 2);
                if (placeAmount <= 0m) return Fail("Must place > 0");

                var unplacedAmount = p.GetUnplacedAmount();
                if (placeAmount > p.GetUnplacedAmount()) return Fail("Cannot place more than the unplaced amount");

                var maxAllowedSavingsCustomerBalance = NEnv.MaxAllowedSavingsCustomerBalance;
                var suggestedPlaceAmount = allowOverMaxAllowedSavingsCustomerBalance.GetValueOrDefault()
                    ? placeAmount
                    : Math.Max(Math.Min(placeAmount, maxAllowedSavingsCustomerBalance - a.CustomerBalance), 0m);

                return Json2(new
                {
                    paymentId = p.PaymentId,
                    savingsAccountNr = a.SavingsAccountNr,
                    requestedPlaceAmount = placeAmount,
                    placeAmount = suggestedPlaceAmount,
                    unplacedAmountBefore = unplacedAmount,
                    unplacedAmountAfter = unplacedAmount - suggestedPlaceAmount,
                    customerBalanceBefore = a.CustomerBalance,
                    customerBalanceAfter = a.CustomerBalance + suggestedPlaceAmount,
                    isOverMaxAllowedSavingsCustomerBalance =
                        a.CustomerBalance + suggestedPlaceAmount > maxAllowedSavingsCustomerBalance,
                    maxAllowedSavingsCustomerBalance = maxAllowedSavingsCustomerBalance
                });
            }

            ActionResult Fail(string s) => Json2(new { FailedMessage = s });
        }

        [HttpPost]
        [Route("Api/UnplacedPayments/FetchEncryptedPaymentItemValue")]
        public ActionResult FetchEncryptedPaymentItemValue(int paymentItemId)
        {
            using (var context = new SavingsContext())
            {
                var id = context.IncomingPaymentHeaderItems.Where(x => x.Id == paymentItemId && x.IsEncrypted)
                    .Select(x => x.Value).FirstOrDefault();
                if (id == null)
                    return HttpNotFound();

                var result = EncryptionContext.Load(context, new[] { long.Parse(id) },
                    NEnv.EncryptionKeys.AsDictionary());

                return Json2(new
                {
                    Value = result.Single().Value
                });
            }
        }

        [NTechApi]
        [AllowAnonymous]
        [HttpPost]
        [Route("Api/UnplacedPayments/IsValidAccountNr")]
        public ActionResult ValidateBankAccount(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Json2(new { isValid = false, message = "Empty" });

            switch (NEnv.ClientCfg.Country.BaseCountry)
            {
                case "FI":
                {
                    if (!IBANFi.TryParse(value, out var iban))
                        return Json2(new { isValid = false, message = "Invalid iban" });

                    var bankName = NEnv.IBANToBICTranslatorInstance.InferBankName(iban);

                    return Json2(new
                    {
                        isValid = true, displayValue = $"{iban.GroupsOfFourValue} ({bankName})",
                        ibanFormatted = new { nr = iban.GroupsOfFourValue, bankName = bankName }
                    });
                }
                case "SE":
                {
                    if (!BankAccountNumberSe.TryParse(value, out var iban, out var message))
                        return Json2(new { isValid = false, message = message });

                    return Json2(new
                        { isValid = true, displayValue = $"{iban.ClearingNr} {iban.AccountNr} ({iban.BankName})" });
                }
                default:
                    throw new NotImplementedException();
            }
        }
    }
}