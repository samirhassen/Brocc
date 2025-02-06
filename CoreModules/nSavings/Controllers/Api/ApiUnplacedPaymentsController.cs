using nSavings.DbModel.BusinessEvents;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.BankAccounts.Se;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nSavings.Controllers
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
                    .SavingsAccountHeaders.Where(x => x.SavingsAccountNr == searchString || x.DatedStrings.Any(y => y.Name == ocrCode && y.Value == searchString))
                    .Select(x => x.SavingsAccountNr)
                    .ToList();

                if (savingsAccountNrs.Count == 0)
                {
                    return Json2(new { isOk = false, failedMessage = "No hits" });
                }
                else if (savingsAccountNrs.Count > 1)
                {
                    return Json2(new { isOk = false, failedMessage = "These savings accounts share the same reference nr. Select one using the savingsAccountNr instead: " + string.Join(", ", savingsAccountNrs) });
                }
                else
                {
                    var savingsAccountNr = savingsAccountNrs.Single();

                    return Json2(new
                    {
                        isOk = true,
                        savingsAccountNr = savingsAccountNr
                    });
                }
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
                var customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceHttpContextUser.SharedInstance, NEnv.ServiceRegistry);
                var mgr = new NewIncomingPaymentFileBusinessEventManager(user, CoreClock.SharedInstance, NEnv.ClientCfgCore,
                    NEnv.EnvSettings, resolver.GetEncryptionService(user), resolver.ContextFactory, customerClient);
                string msg;
                OutgoingPaymentHeader h;
                if (!mgr.TryRepayFromUnplaced(paymentId, repaymentAmount, leaveUnplacedAmount, customerName, iban, out h, out msg))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, msg);
                }
                else
                {
                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
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
                string msg;
                var user = GetCurrentUserMetadata();
                var resolver = Service;
                var customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceHttpContextUser.SharedInstance, NEnv.ServiceRegistry);
                var mgr = new NewIncomingPaymentFileBusinessEventManager(user, CoreClock.SharedInstance, NEnv.ClientCfgCore,
                    NEnv.EnvSettings, resolver.GetEncryptionService(user), resolver.ContextFactory, customerClient);
                if (!mgr.TryPlaceFromUnplaced(paymentId, savingsAccountNr, placeAmount, leaveUnplacedAmount, out msg))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, msg);
                }
                else
                {
                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Error placing payment");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        [Route("Api/UnplacedPayments/PaymentPlacementSuggestion")]
        public ActionResult PaymentPlacementSuggestion(string savingsAccountNr, decimal placeAmount, int paymentId, bool? allowOverMaxAllowedSavingsCustomerBalance)
        {
            if (string.IsNullOrWhiteSpace(savingsAccountNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing savingsAccountNr");

            Func<string, ActionResult> fail = s => Json2(new
            {
                FailedMessage = s
            });

            using (var context = new SavingsContext())
            {
                var p = PaymentDomainModel.CreateForSinglePayment(paymentId, context, Service.GetEncryptionService(GetCurrentUserMetadata()));

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

                if (a == null)
                    return fail("Savings account does not exist");
                if (a.Status != SavingsAccountStatusCode.Active.ToString())
                    return fail($"Cannot place payments against a savings account with status: {a.Status}");

                placeAmount = Math.Round(placeAmount, 2);
                if (placeAmount <= 0m)
                {
                    return fail("Must place > 0");
                }
                var unplacedAmount = p.GetUnplacedAmount();
                if (placeAmount > p.GetUnplacedAmount())
                {
                    return fail("Cannot place more than the unplaced amount");
                }

                var maxAllowedSavingsCustomerBalance = NEnv.MaxAllowedSavingsCustomerBalance;
                var suggestedPlaceAmount = allowOverMaxAllowedSavingsCustomerBalance.GetValueOrDefault()
                    ? placeAmount
                    : Math.Max(Math.Min(placeAmount, maxAllowedSavingsCustomerBalance - a.CustomerBalance), 0m);

                var today = Clock.Today;
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
                    isOverMaxAllowedSavingsCustomerBalance = a.CustomerBalance + suggestedPlaceAmount > maxAllowedSavingsCustomerBalance,
                    maxAllowedSavingsCustomerBalance = maxAllowedSavingsCustomerBalance
                });
            }
        }

        [HttpPost]
        [Route("Api/UnplacedPayments/FetchEncryptedPaymentItemValue")]
        public ActionResult FetchEncryptedPaymentItemValue(int paymentItemId)
        {
            using (var context = new SavingsContext())
            {
                var id = context.IncomingPaymentHeaderItems.Where(x => x.Id == paymentItemId && x.IsEncrypted).Select(x => x.Value).FirstOrDefault();
                if (id == null)
                    return HttpNotFound();

                var result = EncryptionContext.Load(context, new[] { long.Parse(id) }, NEnv.EncryptionKeys.AsDictionary());

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

            if (NEnv.ClientCfg.Country.BaseCountry == "FI")
            {
                IBANFi b;
                if (!IBANFi.TryParse(value, out b))
                    return Json2(new { isValid = false, message = "Invalid iban" });

                var bankName = NEnv.IBANToBICTranslatorInstance.InferBankName(b);

                return Json2(new { isValid = true, displayValue = $"{b.GroupsOfFourValue} ({bankName})", ibanFormatted = new { nr = b.GroupsOfFourValue, bankName = bankName } });
            }
            else if (NEnv.ClientCfg.Country.BaseCountry == "SE")
            {
                BankAccountNumberSe b; string message;
                if (!BankAccountNumberSe.TryParse(value, out b, out message))
                    return Json2(new { isValid = false, message = message });

                return Json2(new { isValid = true, displayValue = $"{b.ClearingNr} {b.AccountNr} ({b.BankName})" });
            }
            else
                throw new NotImplementedException();
        }
    }
}