using nCredit.Code;
using nCredit.Code.Email;
using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using nCredit.DomainModel;
using Newtonsoft.Json;
using NTech.Banking.LoanModel;
using NTech.Core.Module.Shared.Clients;
using NTech.Legacy.Module.Shared;
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
    [RoutePrefix("Api/MortgageLoans/ChangeTerms")]
    public class MortgageLoansCreditChangeTermsController : NController
    {
        private MortgageLoansCreditTermsChangeBusinessEventManager CreateChangeTermsManager() => new MortgageLoansCreditTermsChangeBusinessEventManager(
            GetCurrentUserMetadata(),
            this.Service.LegalInterestCeiling,
            CoreClock.SharedInstance,
            NEnv.ClientCfgCore,
            Service.ContextFactory,
            NEnv.EnvSettings,
            LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry),
            new SerilogLoggingService(),
            new ServiceRegistryLegacy(NEnv.ServiceRegistry),
            EmailServiceFactory.SharedInstance,
            Service.CachedSettings);

        [HttpPost]
        [Route("FetchInitialData")]
        public ActionResult FetchInitialData(string creditNr)
        {
            HistoricalCreditModel creditModel;
            using (var context = Service.ContextFactory.CreateContext())
            {
                creditModel = AmortizationPlan.GetHistoricalCreditModel(creditNr, context, NEnv.IsMortgageLoansEnabled, Clock.Now.DateTime);
            }

            if (creditModel.Status != CreditStatus.Normal.ToString())
            {
                return Json2(new
                {
                    creditStatus = creditModel.Status
                });
            }

            AmortizationPlan amortizationPlan;
            bool hasPlan;
            if (NEnv.HasPerLoanDueDay)
            {
                throw new NotImplementedException();
            }
            else
            {
                hasPlan = FixedDueDayAmortizationPlanCalculator.TryGetAmortizationPlan(
                        creditModel,
                        NEnv.NotificationProcessSettings.GetByCreditType(creditModel.GetCreditType()),
                        out amortizationPlan,
                        out string amortizationPlanFailedMessage,
                        new CoreClock(),
                        CreditDomainModel.GetInterestDividerOverrideByCode(NEnv.ClientInterestModel));
            }

            if (!TryFetchPendingTermsChangeDataByCreditNr(creditNr, out PendingChangeData pendingChangeData, out string failedMessage))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);

            CreditAmortizationModel m;
            if (hasPlan)
            {
                m = amortizationPlan.AmortizationModel;
            }
            else
            {
                using (var context = CreateCreditContext())
                {
                    var cm = CreditDomainModel.PreFetchForSingleCredit(creditNr, context, NEnv.EnvSettings);
                    m = cm.GetAmortizationModel(Clock.Today);
                }
            }

            decimal? annuityAmount = null;
            decimal? monthlyFixedCapitalAmount = null;
            amortizationPlan.AmortizationModel.UsingActualAnnuityOrFixedMonthlyCapital<object>(a =>
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
                creditStatus = creditModel.Status,
                minAllowedMarginInterestRate = minAndMaxAllowedMarginInterestRate?.Item1,
                maxAllowedMarginInterestRate = minAndMaxAllowedMarginInterestRate?.Item2,
                currentTerms = new
                {
                    interestRebindMonthCount = creditModel.InterestRebindMonthCount,
                    interestBoundUntil = creditModel.NextInterestRebindDate,
                    daysLeft = ((creditModel.NextInterestRebindDate ?? Clock.Today) - Clock.Today).Days,
                    referenceInterest = creditModel.ReferenceInterestRatePercent,
                    marginInterest = creditModel.MarginInterestRatePercent,
                    customerTotalInterest = creditModel.ReferenceInterestRatePercent + creditModel.MarginInterestRatePercent
                },
                pendingTerms = pendingChangeData
            });
        }

        [HttpPost]
        [Route("ComputeNewTerms")]
        public ActionResult ComputeNewTerms(string creditNr, MlNewChangeTerms newChangeTerms)
        {
            var manager = CreateChangeTermsManager();

            if (!manager.TryComputeMlTermsChangeData(creditNr, newChangeTerms, out MlTermsChangeData changeData, out string failedMessage))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);

            return Json2(new
            {
                newTerms = changeData
            });
        }

        [HttpPost]
        [Route("StartCreditTermsChange")]
        public ActionResult StartCreditTermsChange(string creditNr, MlNewChangeTerms newTerms)
        {
            var changeTermsManager = CreateChangeTermsManager();

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

            var (IsSuccess, WarningMessage, TermChange) = changeTermsManager.MlStartCreditTermsChange(creditNr, newTerms, createDocumentRenderer, new CreditCustomerClient());
            if (IsSuccess)
            {
                var changeId = TermChange.Id;

                if (!changeTermsManager.TryFetchPendingTermsChangeData(changeId, out PendingChangeData pendingChangeData, out string failedMessage))
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
                else
                    return Json2(new
                    {
                        pendingTerms = pendingChangeData,
                        userMessage,
                        userWarningMessage = WarningMessage
                    });
            }
            else
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, WarningMessage);
        }

        [HttpPost]
        [Route("AttachAgreement")]
        public ActionResult AttachAgreement(int id, string dataUrl, string fileName)
        {
            var changeTermsManager = CreateChangeTermsManager();

            if (!string.IsNullOrWhiteSpace(dataUrl) && !string.IsNullOrWhiteSpace(fileName))
            {
                if (!TryParseDataUrl(dataUrl, out var mimeType, out byte[] fileData))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid attached file.");
                }

                var client = Service.DocumentClientHttpContext;
                var archiveKey = client.ArchiveStore(fileData, mimeType, fileName);

                if (!changeTermsManager.AttachSignedAgreement(id, archiveKey))
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Could not attach file.");
            }

            return Json2(new { });
        }

        [HttpPost]
        [Route("RemoveAgreement")]
        public ActionResult RemoveAgreement(int id, string archiveKey)
        {
            var m = CreateChangeTermsManager();

            if (!m.RemoveSignedAgreeement(id, archiveKey))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Could not remove file.");

            return Json2(new
            {
            });
        }


        [HttpPost]
        [Route("SchedulePendingTermsChange")]
        public ActionResult SchedulePendingTermsChange(int id)
        {
            var changeTermsManager = CreateChangeTermsManager();

            if (!changeTermsManager.TryScheduleCreditTermsChange(id, out string failedMessage))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            else
                return Json2(new { });
        }


        [HttpPost]
        [Route("CancelPendingTermsChange")]
        public ActionResult CancelPendingTermsChange(int id)
        {
            var changeTermsManager = CreateChangeTermsManager();

            if (!changeTermsManager.TryCancelCreditTermsChange(id, isManuallyCancelled: true, out string failedMessage))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            else
                return Json2(new { });
        }

        private bool TryFetchPendingTermsChangeDataByCreditNr(string creditNr, out PendingChangeData pendingChangeData, out string failedMessage)
        {
            var changeTermsManager = CreateChangeTermsManager();

            if (string.IsNullOrWhiteSpace(creditNr))
            {
                failedMessage = "Missing creditNr";
                pendingChangeData = null;
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
                pendingChangeData = null;
                failedMessage = null;
                return true;
            }
            else
                return changeTermsManager.TryFetchPendingTermsChangeData(id.Value, out pendingChangeData, out failedMessage);
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
                    errorMessage = errorMessage,
                    isMortgageLoan = true
                }));

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
    }
}