using nPreCredit.Code;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.LegacyUnsecuredLoans;
using NTech;
using NTech.Banking.LoanModel;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.PreCredit.Shared.Code.PetrusOnlyScoringService;
using NTech.Core.PreCredit.Shared.Services.UlLegacy;
using NTech.Services.Infrastructure;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    [NTechAuthorizeCreditMiddle]
    [RoutePrefix("CreditCheck")]
    public class CreditCheckController : NController
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!NEnv.IsUnsecuredLoansEnabled || NEnv.IsStandardUnsecuredLoansEnabled)
            {
                //Only used by unsecured loan legacy
                filterContext.Result = HttpNotFound();
            }
            base.OnActionExecuting(filterContext);
        }

        [Route("New")]
        public ActionResult New(string applicationNr)
        {

            var retryPolicy = Policy
                                .Handle<NTechCoreWebserviceException>(ex => !ex.IsUserFacing)
                                .Or<Exception>()
                                .WaitAndRetry(2, retryAttempt => TimeSpan.FromSeconds(2),
            onRetry: (exception, timeSpan, retryCount, context) =>
            {
                NLog.Warning(exception, $"AutomaticCreditCheck Retry {retryCount} due to: {exception.Message}");
            });

            var p2 = Service.Resolve<PetrusOnlyCreditCheckService>();

            try
            {
                retryPolicy.Execute(() => p2.AutomaticCreditCheck(applicationNr, true));
                return RedirectToAction("ApplicationGateway", "CreditApplicationLink", new { applicationNr });
            }
            catch (NTechCoreWebserviceException ex)
            {
                if (ex.IsUserFacing)
                {
                    if (ex.ErrorCode == "petrusError")
                    {
                        Service.Resolve<IApplicationCommentService>().TryAddComment(applicationNr, $"Petrus returned an error: {ex.Message}", "petrusError", null, out var _);
                    }
                }
                NLog.Error(ex, "NewCreditCheck failed");
                return RedirectToAction("CreditApplication", "CreditManagement", new
                {
                    applicationNr,
                    onLoadMessage = "Credit check failed"
                });
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "NewCreditCheck failed");
                return RedirectToAction("CreditApplication", "CreditManagement", new
                {
                    applicationNr,
                    onLoadMessage = "Credit check failed"
                });
            }
        }

        [Route("View")]
        public ActionResult View(int id)
        {
            using (var c = new PreCreditContext())
            {
                var result = c
                    .CreditDecisions
                    .Select(x => new
                    {
                        x.CreditApplication.ApplicationType,
                        Decision = x,
                        PauseItems = x.PauseItems.Select(y => new
                        {
                            y.RejectionReasonName,
                            y.Decision.DecisionDate,
                            y.PausedUntilDate
                        })
                    })
                    .Single(x => x.Decision.Id == id);
                var d = result.Decision;
                var ad = d as AcceptedCreditDecision;
                var rd = d as RejectedCreditDecision;

                if (!LoanCreditApplicationTypeHandlerFactory.IsUnsecuredLoanApplication(result.ApplicationType))
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Invalid applicationType: {result.ApplicationType}");

                var decisions = c
                    .CreditDecisions
                    .Where(x => x.ApplicationNr == d.ApplicationNr)
                    .ToList()
                    .Select(x => new
                    {
                        x.Id,
                        DecisionByName = GetUserDisplayNameByUserId(x.DecisionById.ToString()),
                        x.DecisionDate,
                        IsAccepted = (x as AcceptedCreditDecision) != null,
                        ViewUrl = Url.Action("View", "CreditCheck", new { id = x.Id })
                    })
                    .OrderByDescending(x => x.Id)
                    .ToList();

                var maxPauseItem = result
                    .PauseItems
                    .GroupBy(x => new { x.PausedUntilDate, x.DecisionDate })
                    .Select(x => new
                    {
                        nrOfDays = (int)x.Key.PausedUntilDate.Date.Subtract(x.Key.DecisionDate.Date).TotalDays,
                        pausedUntilDate = x.Key.PausedUntilDate,
                        rejectionReasonNames = x.Select(y => y.RejectionReasonName ?? "Other").Distinct().ToList()
                    })
                    .OrderByDescending(x => x.pausedUntilDate)
                    .FirstOrDefault();

                //Credit url pattern
                var creditUrlPattern = NEnv.ServiceRegistry.External.ServiceUrl("nCredit", "Ui/Credit", Tuple.Create("creditNr", "NNN")).ToString();
                var sc = NEnv.ScoringSetup;

                SetInitialData(new
                {
                    isAccepted = ad != null,
                    currentDecisionId = id,
                    decisions = decisions,
                    decisionDate = d.DecisionDate,
                    creditUrlPattern = creditUrlPattern,
                    decisionModelJson = (ad?.AcceptedDecisionModel) ?? rd.RejectedDecisionModel,
                    applicationNr = d.ApplicationNr,
                    fetchSatReportUrl = Url.Action("FetchSatReportForApplication", "ApiSatForApplication"),
                    unlockHasBusinessConnectionForViewUrl = Url.ActionStrict("UnlockHasBusinessConnectionForView", "CreditCheck"),
                    unlockImmigrationDateForViewUrl = Url.ActionStrict("UnlockImmigrationDateForView", "CreditCheck"),
                    translation = GetTranslations(),
                    changedCreditApplicationItems = GetChangedCreditApplicationItems(d.ApplicationNr),
                    urlToHere = Url.Action("View", new { id }),
                    scoringRuleToRejectionReasonMapping = sc.GetScoringRuleToRejectionReasonMapping(),
                    rejectionReasonToDisplayNameMapping = sc.GetRejectionReasonToDisplayNameMapping(),
                    manualControlReasonToDisplayTextMapping = sc.GetManualControlReasonToDisplayTextMapping(),
                    maxPauseItem,
                    listCreditReportProviders = NEnv.ListCreditReportProviders
                });

                return View();
            }
        }

        public static IList<Tuple<string, string>> GetChangedCreditApplicationItems(string applicationNr)
        {
            using (var context = new PreCreditContext())
            {
                return context
                    .CreditApplicationChangeLogItems
                    .Where(x => x.ApplicationNr == applicationNr)
                    .Select(x => new { x.GroupName, x.Name })
                    .Distinct()
                    .ToList()
                    .Select(x => Tuple.Create(x.GroupName, x.Name))
                    .ToList();
            }
        }

        [Route("UnlockHasBusinessConnectionForView")]
        [HttpPost]
        [NTechApi]
        public ActionResult UnlockHasBusinessConnectionForView(string applicationNr)
        {
            var hasBusinessConnection = false;
            var anyItemsFound = false;
            GetCreditReports(applicationNr, new List<string>() { "hasBusinessConnection" }, (applicantNr, value) =>
            {
                anyItemsFound = true;
                if (value == "true")
                {
                    hasBusinessConnection = true;
                    return false; //There is no point decrypting more reports in this case since it won't change the result
                }
                else
                    return true;
            });
            return Json2(new { hasBusinessConnection = anyItemsFound ? new bool?(hasBusinessConnection) : new bool?() });
        }

        [Route("UnlockImmigrationDateForView")]
        [HttpPost]
        [NTechApi]
        public ActionResult UnlockImmigrationDateForView(string applicationNr)
        {
            DateTime? mostRecentImmigrationDate = null;
            var anyItemsFound = false;
            GetCreditReports(applicationNr, new List<string>() { "immigrationDate" }, (applicantNr, value) =>
            {
                anyItemsFound = true;
                var d = Dates.ParseDateTimeExactOrNull(value, "yyyy-MM-dd");
                if (d.HasValue && (!mostRecentImmigrationDate.HasValue || mostRecentImmigrationDate.Value < d.Value))
                    mostRecentImmigrationDate = d;

                return true;
            });

            return Json2(new
            {
                immigrationDateText = anyItemsFound
                ? (mostRecentImmigrationDate.HasValue ? $"{mostRecentImmigrationDate.Value.ToString("yyyy-MM-dd")}" : "Never")
                : "Unknown"
            });
        }

        private void GetCreditReports(string applicationNr, List<string> itemsToFetch, Func<int, string, bool> onItem)
        {
            var c = Service.Resolve<CreditReportService>();
            using (var context = new PreCreditContext())
            {
                var d = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == applicationNr)
                    .Select(x => x.CurrentCreditDecision)
                    .Single();

                var model = ((d as AcceptedCreditDecision)?.AcceptedDecisionModel) ?? ((d as RejectedCreditDecision)?.RejectedDecisionModel);
                var creditReportsUsed = CreditDecisionModelParser.ParseCreditReportsUsed(model);

                if (creditReportsUsed != null)
                {
                    foreach (var cr in creditReportsUsed)
                    {
                        var creditReport = c.GetCreditReportById(cr.CreditReportId, itemsToFetch);
                        var item = creditReport.Items.SingleOrDefault();

                        if (item != null)
                        {
                            if (!onItem(cr.ApplicantNr, item.Value))
                            {
                                break; //Dont fetch any more
                            }
                        }
                    }
                }
            }
        }

        [Route("FetchAdditionalLoanInfo")]
        [HttpPost]
        public ActionResult FetchAdditionalLoanInfo(string creditNr, decimal additionalLoanAmount, decimal? newAnnuityAmount, decimal? newMarginInterestRatePercent, decimal? newNotificationFeeAmount)
        {
            var c = new CreditClient();
            var histories = c.GetCustomerCreditHistoryByCreditNrs(new List<string> { creditNr });
            var history = histories.Single(x => x.CreditNr == creditNr);

            Func<decimal?, int, decimal?> round = (x, n) => x.HasValue ? Math.Round(x.Value, n) : x;

            additionalLoanAmount = Math.Round(additionalLoanAmount, 2);
            newAnnuityAmount = round(newAnnuityAmount, 2);
            newMarginInterestRatePercent = round(newMarginInterestRatePercent, 4);
            newNotificationFeeAmount = newNotificationFeeAmount.HasValue ? round(newNotificationFeeAmount, 2) : history.NotificationFeeAmount;

            var annuityAmount = newAnnuityAmount ?? history.AnnuityAmount;
            var yearlyInterestRatePercent = (newMarginInterestRatePercent ?? history.MarginInterestRatePercent.Value) + (history.ReferenceInterestRatePercent ?? 0m);

            try
            {
                var m = PaymentPlanCalculation.BeginCreateWithAnnuity(additionalLoanAmount + history.CapitalBalance, annuityAmount.Value, yearlyInterestRatePercent, null, NEnv.CreditsUse360DayInterestYear);
                if (newNotificationFeeAmount.HasValue && newNotificationFeeAmount.Value > 0m)
                    m = m.WithMonthlyFee(newNotificationFeeAmount.Value);

                var p = m.EndCreate();

                var totalPaidAmount = p.Payments.Sum(x => x.TotalAmount);
                var effectiveInterestRatePercent = p.EffectiveInterestRatePercent;
                var repaymentTimeInMonths = p.Payments.Count;
                bool isOverHandlerLimit;
                bool? isAllowedToOverrideHandlerLimit;
                var e = DependancyInjection.Services.Resolve<HandlerLimitEngine>();
                e.CheckHandlerLimits(additionalLoanAmount, histories.Aggregate(0m, (x, y) => x + y.CapitalBalance), CurrentUserId, out isOverHandlerLimit, out isAllowedToOverrideHandlerLimit);

                return Json2(new
                {
                    creditNr = creditNr,
                    hasSolution = true,
                    isOverHandlerLimit = isOverHandlerLimit,
                    isAllowedToOverrideHandlerLimit = isAllowedToOverrideHandlerLimit,
                    effectiveInterestRatePercent = effectiveInterestRatePercent,
                    totalAmountPaid = totalPaidAmount,
                    repaymentTimeInMonths = repaymentTimeInMonths
                });
            }
            catch (PaymentPlanCalculationException)
            {
                return Json2(new
                {
                    creditNr = creditNr,
                    hasSolution = false
                });
            }
        }
    }
}