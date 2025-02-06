using nSavings.DbModel.BusinessEvents;
using NTech;
using NTech.Services.Infrastructure;
using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nSavings.Controllers
{
    [NTechApi]
    [RoutePrefix("Api/InterestRateChange")]
    public class ApiInterestRateChangeController : NController
    {
        public static InMemoryInterestChangeManager ChangeHandler = new InMemoryInterestChangeManager();

        public static object GetChangeStateViewModel(InMemoryInterestChangeManager.ChangeState state, int currentUserId, Func<string, string> getUserDisplayNameByUserId, IClock clock)
        {
            if (state == null)
                return null;

            Func<DateTime?, string> formatDate = d => d.HasValue ? d.Value.ToString("yyyy-MM-dd") : (string)null;
            Func<int?, string> getDisplayName = uid => uid.HasValue ? getUserDisplayNameByUserId(uid.Value.ToString()) : null;

            //In finland you need to wait two months before lowering the interest rate on existing accounts
            var isViolatingTwoMonthLoweringRule = (NEnv.ClientCfg.Country.BaseCountry == "FI"
                    && state.OldInterestRatePercent.HasValue
                    && state.NewInterestRatePercent < state.OldInterestRatePercent.Value && state.AllAccountsValidFromDate < clock.Today.AddMonths(2));

            return new
            {
                ChangeToken = state.ChangeToken,
                OldInterestRatePercent = state.OldInterestRatePercent,
                NewInterestRatePercent = state.NewInterestRatePercent,
                AllAccountsValidFromDate = formatDate(state.AllAccountsValidFromDate),
                NewAccountsValidFromDate = formatDate(state.NewAccountsValidFromDate),
                CurrentUserId = currentUserId.ToString(),
                CurrentUserDisplayName = getUserDisplayNameByUserId(currentUserId.ToString()),
                InitiatedByUserId = state.InitiatedByUserId.ToString(),
                InitiatedByUserDisplayName = getUserDisplayNameByUserId(state.InitiatedByUserId.ToString()),
                InitiatedDate = formatDate(state.InitiatedDate.DateTime),
                VerifiedByUserId = state.VerifiedByUserId?.ToString(),
                VerifiedByUserDisplayName = getDisplayName(state.VerifiedByUserId),
                RejectedByUserDisplayName = getDisplayName(state.RejectedByUserId),
                RejectedByUserId = state.RejectedByUserId?.ToString(),
                VerifiedOrRejectedDate = formatDate(state.VerifiedOrRejectedDate?.DateTime),
                IsViolatingTwoMonthLoweringRule = isViolatingTwoMonthLoweringRule
            };
        }

        public static object GetUpcomingChangesViewModel(SavingsContext context, Func<string, string> getUserDisplayNameByUserId, IClock clock)
        {
            return ChangeInterestRateBusinessEventManager
                .GetUpcomingChangesModels(context, clock)
                .OrderByDescending(x => x.Id)
                .ToList()
                .Select(x => new
                {
                    x.Id,
                    x.HadNewAccountsOnlyRate,
                    NewInterestRatePercent = x.NewAccountsOnlyRate?.NewInterestRatePercent ?? x.AllAccountsRate?.NewInterestRatePercent,
                    NewAccountsOnlyRateValidFromDate = (x.NewAccountsOnlyRate?.ValidFromDate)?.ToString("yyyy-MM-dd"),
                    NewAccountsOnlyRateIsPending = x.NewAccountsOnlyRate?.IsPending,
                    AllAccountsRateValidFromDate = (x.AllAccountsRate?.ValidFromDate)?.ToString("yyyy-MM-dd"),
                    AllAccountsRateIsPending = x.AllAccountsRate?.IsPending,
                    x.InitiatedAndCreatedByUserId,
                    InitiatedAndCreatedByUserDisplayName = getUserDisplayNameByUserId(x.InitiatedAndCreatedByUserId.ToString()),
                    InitiatedDate = x.InitiatedDate.ToString("yyyy-MM-dd"),
                    CreatedDate = x.CreatedDate.ToString("yyyy-MM-dd"),
                    x.VerifiedByUserId,
                    VerifiedByUserDisplayName = getUserDisplayNameByUserId(x.VerifiedByUserId.ToString()),
                    VerifiedDate = x.VerifiedDate.ToString("yyyy-MM-dd")
                })
                .ToList();
        }

        public static object GetHistoryItemsViewModel(SavingsContext context, Func<string, string> getUserDisplayNameByUserId, IClock clock)
        {
            return ChangeInterestRateBusinessEventManager
                .GetHistoryModelItems(context, clock)
                .OrderByDescending(x => x.Id)
                .ToList()
                .Select(x => new
                {
                    x.Id,
                    x.AccountTypeCode,
                    x.InterestRatePercent,
                    ValidFromDate = x.ValidFromDate.ToString("yyyy-MM-dd"),
                    x.RemovedByBusinessEventId,
                    x.AppliesToAccountsSinceBusinessEventId,
                    x.IsPartOfSplitChange,
                    InitiatedDate = (x.InitiatedDate?.ToString("yyyy-MM-dd") ?? x.FallbackInitiatedDate.ToString("yyyy-MM-dd")),
                    CreatedByUserId = x.CreatedByUserId ?? x.FallbackCreatedByUserId,
                    CreatedByUserDisplayName = getUserDisplayNameByUserId((x.CreatedByUserId ?? x.FallbackCreatedByUserId).ToString()),
                    VerifiedByUserId = x.VerifiedByUserId ?? x.FallbackCreatedByUserId,
                    VerifiedByUserDisplayName = getUserDisplayNameByUserId((x.VerifiedByUserId ?? x.FallbackCreatedByUserId).ToString()),
                    x.RemovedByUserId,
                    RemovedByUserDisplayName = x.RemovedByUserId.HasValue ? getUserDisplayNameByUserId(x.RemovedByUserId.Value.ToString()) : null,
                    RemovedDate = x.RemovedDate?.ToString("yyyy-MM-dd")
                })
                .ToList();
        }

        [HttpPost]
        [Route("FetchHistoricalChangeItems")]
        public ActionResult FetchHistoricalChangeItems()
        {
            using (var context = new SavingsContext())
            {
                return Json2(new
                {
                    historicalChangeItems = GetHistoryItemsViewModel(context, this.GetUserDisplayNameByUserId, this.Clock)
                });
            }
        }

        [HttpPost]
        [Route("InitiateChange")]
        public ActionResult InitiateChange(decimal? newInterestRatePercent, string allAccountsValidFromDate, string newAccountsValidFromDate)
        {
            var currentUserId = GetCurrentUserIdWithTestSupport();

            if (!newInterestRatePercent.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing newInterestRatePercent");

            if (string.IsNullOrWhiteSpace(allAccountsValidFromDate))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing allAccountsValidFromDate");
            var allAccountsValidFromDateD = DateTimeUtilities.ParseExact(allAccountsValidFromDate, "yyyy-MM-dd");
            if (!allAccountsValidFromDateD.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid allAccountsValidFromDate");

            var newAccountsValidFromDateD = DateTimeUtilities.ParseExact(newAccountsValidFromDate, "yyyy-MM-dd");
            if (!string.IsNullOrWhiteSpace(newAccountsValidFromDate) && !newAccountsValidFromDateD.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid newAccountsValidFromDate");

            using (var context = new SavingsContext())
            {
                var currentInterestRate = ChangeInterestRateBusinessEventManager.GetCurrentInterestRateForNewAccounts(context, SavingsAccountTypeCode.StandardAccount, Clock.Today);

                string failedMessage;
                InMemoryInterestChangeManager.ChangeState state;
                if (!ChangeHandler.TryInitiateChange(this.Clock,
                    SavingsAccountTypeCode.StandardAccount.ToString(), currentInterestRate?.InterestRatePercent,
                    newInterestRatePercent.Value, allAccountsValidFromDateD.Value, newAccountsValidFromDateD,
                    currentUserId, out failedMessage, out state))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
                }
                else
                {
                    return Json2(new
                    {
                        currentChangeState = GetChangeStateViewModel(state, currentUserId, this.GetUserDisplayNameByUserId, this.Clock)
                    });
                }
            }
        }

        [HttpPost]
        [Route("GetCurrentChangeState")]
        public ActionResult GetCurrentChangeState()
        {
            var currentUserId = GetCurrentUserIdWithTestSupport();

            var state = ChangeHandler.GetCurrentChangeState();
            return Json2(new
            {
                currentChangeState = GetChangeStateViewModel(state, currentUserId, this.GetUserDisplayNameByUserId, this.Clock)
            });
        }

        [HttpPost]
        [Route("CancelChange")]
        public ActionResult CancelChange(string changeToken)
        {
            var currentUserId = GetCurrentUserIdWithTestSupport();

            if (string.IsNullOrWhiteSpace(changeToken))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing changeToken");

            string failedMessage;
            if (!ChangeHandler.TryCancelCurrentChange(this.Clock, currentUserId, changeToken, out failedMessage))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);

            var state = ChangeHandler.GetCurrentChangeState();
            return Json2(new
            {
                currentChangeState = GetChangeStateViewModel(state, currentUserId, this.GetUserDisplayNameByUserId, this.Clock)
            });
        }

        [HttpPost]
        [Route("VerifyChange")]
        public ActionResult VerifyChange(string changeToken)
        {
            var currentUserId = GetCurrentUserIdWithTestSupport();

            if (string.IsNullOrWhiteSpace(changeToken))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing changeToken");

            string failedMessage;
            if (!ChangeHandler.TryVerifyCurrentChange(this.Clock, currentUserId, changeToken, out failedMessage))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);

            var state = ChangeHandler.GetCurrentChangeState();
            return Json2(new
            {
                currentChangeState = GetChangeStateViewModel(state, currentUserId, this.GetUserDisplayNameByUserId, this.Clock)
            });
        }

        [HttpPost]
        [Route("RejectChange")]
        public ActionResult RejectChange(string changeToken)
        {
            var currentUserId = GetCurrentUserIdWithTestSupport();

            if (string.IsNullOrWhiteSpace(changeToken))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing changeToken");

            string failedMessage;
            if (!ChangeHandler.TryRejectCurrentChange(this.Clock, currentUserId, changeToken, out failedMessage))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);

            var state = ChangeHandler.GetCurrentChangeState();
            return Json2(new
            {
                currentChangeState = GetChangeStateViewModel(state, currentUserId, this.GetUserDisplayNameByUserId, this.Clock)
            });
        }

        [HttpPost]
        [Route("CarryOutChange")]
        public ActionResult CarryOutChange(string changeToken, bool? returnUpcomingChanges)
        {
            var currentUserId = GetCurrentUserIdWithTestSupport();

            if (string.IsNullOrWhiteSpace(changeToken))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing changeToken");

            string failedMessage;
            SharedSavingsInterestRateChangeHeader result = null;
            if (!ChangeHandler.TryCarryOutCurrentChange(this.Clock, currentUserId, changeToken, change =>
            {
                var mgr = new ChangeInterestRateBusinessEventManager(currentUserId, InformationMetadata, Clock);
                string fm;
                SharedSavingsInterestRateChangeHeader h;
                if (!mgr.TryChangeInterestRate(change, out fm, out h))
                {
                    return Tuple.Create(false, fm);
                }
                else
                {
                    result = h;
                    return Tuple.Create(true, (string)null);
                }
            }, out failedMessage))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            }

            object upcomingChanges = null;
            if (returnUpcomingChanges ?? false)
            {
                using (var context = new SavingsContext())
                {
                    upcomingChanges = GetUpcomingChangesViewModel(context, this.GetUserDisplayNameByUserId, this.Clock);
                }
            }

            return Json2(new
            {
                upcomingChanges = upcomingChanges,
                currentChangeState = GetChangeStateViewModel(ChangeHandler.GetCurrentChangeState(), currentUserId, this.GetUserDisplayNameByUserId, this.Clock)
            });
        }

        [HttpPost]
        [Route("CancelUpcomingChange")]
        public ActionResult CancelUpcomingChange(int? rateChangeHeaderId, int? testUserId)
        {
            var currentUserId = GetCurrentUserIdWithTestSupport();

            if (!rateChangeHeaderId.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing rateChangeHeaderId");

            var mgr = new ChangeInterestRateBusinessEventManager(currentUserId, InformationMetadata, Clock);
            string failedMessage;
            if (!mgr.TryRemovePendingInterestRateChange(rateChangeHeaderId.Value, out failedMessage))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            }

            using (var context = new SavingsContext())
            {
                return Json2(new
                {
                    upcomingChanges = GetUpcomingChangesViewModel(context, this.GetUserDisplayNameByUserId, this.Clock),
                    currentChangeState = GetChangeStateViewModel(ChangeHandler.GetCurrentChangeState(), currentUserId, this.GetUserDisplayNameByUserId, this.Clock)
                });
            }
        }

        [HttpPost]
        [Route("DirectlyChangeInterestRate")]
        public ActionResult DirectlyChangeInterestRate(decimal? newInterestRatePercent, string allAccountsValidFromDate, string newAccountsValidFromDate)
        {
            if (NEnv.IsProduction && !NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.savings.disabledualityforinterestratechange"))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "The system is configured to not allow this without duality. Use ntech.feature.savings.disabledualityforinterestratechange to change this");

            if (!newInterestRatePercent.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing newInterestRatePercent");

            if (string.IsNullOrWhiteSpace(allAccountsValidFromDate))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing allAccountsValidFromDate");
            var allAccountsValidFromDateD = DateTimeUtilities.ParseExact(allAccountsValidFromDate, "yyyy-MM-dd");
            if (!allAccountsValidFromDateD.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid allAccountsValidFromDate");

            var newAccountsValidFromDateD = DateTimeUtilities.ParseExact(newAccountsValidFromDate, "yyyy-MM-dd");
            if (!string.IsNullOrWhiteSpace(newAccountsValidFromDate) && !newAccountsValidFromDateD.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid newAccountsValidFromDate");

            using (var context = new SavingsContext())
            {
                var currentInterestRate = ChangeInterestRateBusinessEventManager.GetCurrentInterestRateForNewAccounts(context, SavingsAccountTypeCode.StandardAccount, Clock.Today);

                string failedMessage;
                InMemoryInterestChangeManager.ChangeState state;
                if (!ChangeHandler.TryInitiateChange(this.Clock,
                    SavingsAccountTypeCode.StandardAccount.ToString(), currentInterestRate?.InterestRatePercent,
                    newInterestRatePercent.Value, allAccountsValidFromDateD.Value, newAccountsValidFromDateD,
                    this.CurrentUserId, out failedMessage, out state))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
                }

                if (!ChangeHandler.TryVerifyCurrentChange(this.Clock, CurrentUserId, state.ChangeToken, out failedMessage, dontEnforceDuality: true))
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);

                SharedSavingsInterestRateChangeHeader result = null;
                if (!ChangeHandler.TryCarryOutCurrentChange(this.Clock, CurrentUserId, state.ChangeToken, change =>
                {
                    var mgr = new ChangeInterestRateBusinessEventManager(CurrentUserId, InformationMetadata, Clock);
                    string fm;
                    SharedSavingsInterestRateChangeHeader h;
                    if (!mgr.TryChangeInterestRate(change, out fm, out h))
                    {
                        return Tuple.Create(false, fm);
                    }
                    else
                    {
                        result = h;
                        return Tuple.Create(true, (string)null);
                    }
                }, out failedMessage))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
                }

                return Json2(new
                {

                });
            }
        }
    }
}