using nCredit.Code;
using nCredit.Code.Email;
using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using nCredit.DomainModel;
using Newtonsoft.Json;
using NTech.Banking.LoanModel;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Legacy.Module.Shared.Services;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    [NTechAuthorizeCreditMiddle(ValidateAccessToken = true)]
    [RoutePrefix("Api/Credit/ChangeTerms")]
    public class ApiCreateCreditChangeTermsController : NController
    {
        [HttpPost]
        [Route("FetchInitialData")]
        public ActionResult FetchInitialData(string creditNr)
        {
            HistoricalCreditModel model;
            using (var context = Service.ContextFactory.CreateContext())
            {
                model = AmortizationPlan.GetHistoricalCreditModel(creditNr, context, NEnv.IsMortgageLoansEnabled);
            }
            if (model.IsMortgageLoan)
                throw new Exception("Mortage loans not supported");

            if (model.Status != CreditStatus.Normal.ToString() || model.SinglePaymentLoanRepaymentDays.HasValue)
            {
                return Json2(new
                {
                    creditStatus = model.Status,
                    isSingleRepaymentLoan = model.SinglePaymentLoanRepaymentDays.HasValue
                });
            }

            string amortizationPlanFailedMessage;

            AmortizationPlan p;
            bool hasPlan;
            if (NEnv.HasPerLoanDueDay)
            {
                throw new NotImplementedException();
            }
            else
            {
                hasPlan = FixedDueDayAmortizationPlanCalculator.TryGetAmortizationPlan(
                        model,
                        NEnv.NotificationProcessSettings.GetByCreditType(model.GetCreditType()),
                        out p,
                        out amortizationPlanFailedMessage,
                        new CoreClock(),
                        CreditDomainModel.GetInterestDividerOverrideByCode(NEnv.ClientInterestModel));
            }

            CreditTermsChangeBusinessEventManager.PendingChangeData pd;
            string failedMessage;
            if (!TryFetchPendingTermsChangeDataByCreditNr(creditNr, out pd, out failedMessage))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);

            CreditAmortizationModel m;
            if (hasPlan)
            {
                m = p.AmortizationModel;
            }
            else
            {
                using (var context = CreateCreditContext())
                {
                    var cm = CreditDomainModel.PreFetchForSingleCredit(creditNr, context, NEnv.EnvSettings);
                    m = cm.GetAmortizationModel(Clock.Today);
                }
            }

            if (m.AmortizationExceptionUntilDate.HasValue || m.AmortizationFreeUntilDate.HasValue)
                throw new Exception("Not suppoted for change terms");

            decimal? annuityAmount = null;
            decimal? monthlyFixedCapitalAmount = null;
            p.AmortizationModel.UsingActualAnnuityOrFixedMonthlyCapital<object>(a =>
            {
                annuityAmount = a;
                monthlyFixedCapitalAmount = new decimal?();
                return null;
            }, c =>
            {
                annuityAmount = new decimal?();
                monthlyFixedCapitalAmount = new decimal?(c);
                return null;
            });


            var minAndMaxAllowedMarginInterestRate = NEnv.MinAndMaxAllowedMarginInterestRate;

            return Json2(new
            {
                creditStatus = model.Status,
                minAllowedMarginInterestRate = minAndMaxAllowedMarginInterestRate?.Item1,
                maxAllowedMarginInterestRate = minAndMaxAllowedMarginInterestRate?.Item2,
                currentTerms = new
                {
                    annuityAmount = annuityAmount,
                    monthlyFixedCapitalAmount = monthlyFixedCapitalAmount,
                    nrOfRemainingPayments = p?.NrOfRemainingPayments,
                    amortizationPlanFailedMessage = amortizationPlanFailedMessage,
                    marginInterestRatePercent = model.MarginInterestRatePercent
                },
                pendingTerms = pd
            });
        }

        private CreditTermsChangeBusinessEventManager CreateChangeTermsManager() => new CreditTermsChangeBusinessEventManager(GetCurrentUserMetadata(), Service.LegalInterestCeiling,
                CoreClock.SharedInstance, NEnv.ClientCfgCore, Service.ContextFactory, NEnv.EnvSettings, EmailServiceFactory.SharedInstance, 
                LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry), 
                new SerilogLoggingService(),
                Service.ServiceRegistry, x => NEnv.GetAffiliateModel(x));

        [HttpPost]
        [Route("ComputeNewTerms")]
        public ActionResult ComputeNewTerms(string creditNr, int newRepaymentTimeInMonths, decimal newMarginInterestRatePercent)
        {
            var m = CreateChangeTermsManager();
            CreditTermsChangeBusinessEventManager.TermsChangeData tc;
            string failedMessage;

            if (!m.TryComputeTermsChangeData(creditNr, newRepaymentTimeInMonths, newMarginInterestRatePercent, true, out tc, out failedMessage))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);

            return Json2(new
            {
                newTerms = tc
            });
        }

        [HttpPost]
        [Route("SendNewTerms")]
        public ActionResult SendNewTerms(string creditNr, int newRepaymentTimeInMonths, decimal newMarginInterestRatePercent)
        {
            var m = CreateChangeTermsManager();

            string failedMessage;
            Dictionary<string, object> userMessage = null;
            Action<string, int> observeSignatureLinkAndApplicantNr = (signatureUrl, applicantNr) =>
            {
                if (NEnv.IsProduction)
                    return;
                if (applicantNr == 1)
                {
                    userMessage = new Dictionary<string, object>
                    {
                        { "title", "Credit terms change" },
                        { "text", $"Sign agreement for applicant {applicantNr}" },
                        { "link", new Uri(signatureUrl).ToString() },
                    };
                }
            };

            Func<IDocumentRenderer> createDocumentRenderer = () => Service.GetDocumentRenderer(false);

            var termChangeResult = m.StartCreditTermsChange(creditNr, newRepaymentTimeInMonths, newMarginInterestRatePercent,
                createDocumentRenderer,
                observeSignatureLinkAndApplicantNr: observeSignatureLinkAndApplicantNr);
            if (termChangeResult.IsSuccess)
            {
                var changeId = termChangeResult.TermChange.Id;

                CreditTermsChangeBusinessEventManager.PendingChangeData pd;
                if (!m.TryFetchPendingTermsChangeData(changeId, true, out pd, out failedMessage))
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
                else
                    return Json2(new
                    {
                        pendingTerms = pd,
                        userMessage = userMessage,
                        userWarningMessage = termChangeResult.WarningMessage
                    });
            }
            else
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, termChangeResult.WarningMessage);
        }

        [HttpPost]
        [Route("FetchPendingTerms")]
        public ActionResult FetchPendingTerms(string creditNr)
        {
            CreditTermsChangeBusinessEventManager.PendingChangeData pd;
            string failedMessage;
            if (!TryFetchPendingTermsChangeDataByCreditNr(creditNr, out pd, out failedMessage))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            else
                return Json2(new
                {
                    pendingTerms = pd
                });
        }

        [HttpPost]
        [Route("CancelPendingTermsChange")]
        public ActionResult CancelPendingTermsChange(int id)
        {
            var m = CreateChangeTermsManager();

            string failedMessage;
            if (!m.TryCancelCreditTermsChange(id, true, out failedMessage))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            else
                return Json2(new
                {
                });
        }

        [HttpPost]
        [Route("AcceptPendingTermsChange")]
        public ActionResult AcceptPendingTermsChange(int id)
        {
            var m = CreateChangeTermsManager();

            string failedMessage;
            if (!m.TryAcceptCreditTermsChange(id, out failedMessage))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            else
                return Json2(new
                {
                });
        }

        [Route("HandleSignatureEvent")]
        [HttpPost]
        public ActionResult UpdateTermsChangeOnSignatureEvent(string token, string eventName, string errorMessage)
        {
            var m = CreateChangeTermsManager();

            string failedMessage;
            if (!m.TryUpdateTermsChangeOnSignatureEvent(token, eventName, errorMessage, out failedMessage))
            {
                Log.Warning("Failed to update credit terms change on signature callback because '{failedMessage}' on '{signatureCallbackToken}'", failedMessage, token);
                return Json2(new { isOk = false, failedMessage = failedMessage });
            }
            else
                return Json2(new { isOk = true, failedMessage = (string)null });
        }

        [Route("SignaturePostback/{token}")]
        [HttpPost]
        [AllowAnonymous]
        public ActionResult ReceivePostback(string token, string eventName, string errorMessage)
        {
            //This is basically a roundabout call to UpdateTermsChangeOnSignatureEvent above using the eventing system to make sure
            //the change is done by an actual user
            if (string.IsNullOrWhiteSpace(token))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing token");

            Log.Information("Credit Change terms ReceivePostback - {token} - {eventName} - {errorMessage}", token, eventName, errorMessage);

            NTech.Services.Infrastructure.Eventing.NTechEventHandler.PublishEvent(
                CreditEventCode.CreditChangeTermsAgreementSigned.ToString(),
                JsonConvert.SerializeObject(new
                {
                    token = token,
                    eventName = eventName,
                    errorMessage = errorMessage
                }));

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        private bool TryFetchPendingTermsChangeDataByCreditNr(string creditNr, out CreditTermsChangeBusinessEventManager.PendingChangeData pd, out string failedMessage)
        {
            var m = CreateChangeTermsManager();

            if (string.IsNullOrWhiteSpace(creditNr))
            {
                failedMessage = "Missing creditNr";
                pd = null;
                return false;
            }
            int? id;
            using (var context = new CreditContext())
            {
                id = context
                    .CreditTermsChangeHeaders
                    .Where(x => x.CreditNr == creditNr && !x.CommitedByEventId.HasValue && !x.CancelledByEventId.HasValue)
                    .OrderByDescending(x => x.CreatedByEvent.Timestamp)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefault();
            }

            if (!id.HasValue)
            {
                pd = null;
                failedMessage = null;
                return true;
            }
            else
                return m.TryFetchPendingTermsChangeData(id.Value, true, out pd, out failedMessage);
        }
    }
}