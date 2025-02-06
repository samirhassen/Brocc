using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using nCredit.DomainModel;
using NTech.Core.Credit.Shared.Services.Aml.Cm1;
using NTech.Legacy.Module.Shared;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechAuthorizeCreditHigh]
    public class ScheduledTasksController : NController
    {
        /// <summary>
        /// This is exposed so menu items can be moved one a time to the new angular backoffice
        /// </summary>
        [HttpPost]
        [Route("Api/ScheduledTasks/Fetch-AllTasks")]
        public ActionResult FetchAllTasks(string crossModuleNavigationTargetCode)
        {
            var hasValidCode = (crossModuleNavigationTargetCode != null && NTechNavigationTarget.IsValidCrossModuleNavigationTargetCode(crossModuleNavigationTargetCode));
            var backTarget = hasValidCode ? NTechNavigationTarget.CreateFromTargetCode(crossModuleNavigationTargetCode) : null;

            var service = Service;
            return Json2(new ScheduledTasksService(service.ServiceRegistry, NEnv.ClientCfgCore, NEnv.EnvSettings)
                .GetScheduledTaskMenuItems(backTarget)
                .Where(x => x.IsEnabled)
                .ToList());
        }

        [HttpPost]
        [Route("Api/Credit/CreateNotificationsInitialData")]
        public ActionResult CreateNotificationsInitialData()
        {
            return Json2(GetNotificationsInitialData());
        }

        private NotificationsInitialDataModel GetNotificationsInitialData()
        {
            var d = Clock.Today;
            var month = d.Month;
            var year = d.Year;

            int nrOfEnabledCreditTypes = 0;

            Action<CreditType, Action<NotificationsInitialDataProductModel>> handleType = (creditType, setModel) =>
            {
                using (var context = Service.ContextFactory.CreateContext())
                {
                    var creditsPre = context.CreditHeadersQueryable;

                    string notificationApiUrl;
                    if (creditType == CreditType.UnsecuredLoan)
                    {
                        creditsPre = creditsPre.Where(x => x.CreditType == null || x.CreditType == CreditType.UnsecuredLoan.ToString());
                        notificationApiUrl = Url.Action("CreateNotifications", "ApiCreateCreditNotifications");
                    }
                    else if (creditType == CreditType.CompanyLoan)
                    {
                        creditsPre = creditsPre.Where(x => x.CreditType == creditType.ToString());
                        notificationApiUrl = Service.WsUrl.CreatePostUrl("CompanyCredit/Notify");
                    }
                    else if (creditType == CreditType.MortgageLoan)
                    {
                        creditsPre = creditsPre.Where(x => x.CreditType == creditType.ToString());
                        notificationApiUrl = Service.WsUrl.CreatePostUrl("MortgageLoans/Notify");
                    }
                    else
                        throw new Exception($"Unhandled loan type: {creditType}");

                    var allNotifications = creditsPre.SelectMany(x => x.Notifications);

                    int? countDeliveredThisPeriod;
                    int? countCreatedByNotDeliveredCurrently;
                    int? countNotNotifiedCurrently;
                    Dictionary<string, string> skipReasonsByCreditNr;
                    List<List<string>> notificationGroups;

                    var today = Clock.Today;

                    skipReasonsByCreditNr = new Dictionary<string, string>();

                    var creditGroupsToNotify = Service.GetNotificationService(false).GetCreditGroupsToNotifyComposable(context, observeSkipReasonsByCreditNr: x => skipReasonsByCreditNr = x);

                    notificationGroups = creditGroupsToNotify.Select(x => Enumerables.Singleton(x.Common.CreditNr).Concat(x.CoNotifiedCredits.Select(y => y.CreditNr)).ToList()).ToList();

                    //Only today for per loan due dates
                    var deliveredThisPeriod = allNotifications
                        .Where(x => x.DeliveryFile.TransactionDate == today);

                    var coNotifiedCount = allNotifications
                        .Count(x =>
                        !x.OutgoingCreditNotificationDeliveryFileHeaderId.HasValue
                        && x.IsCoNotificationMaster == false
                        && x.CoNotificationId != null
                        && deliveredThisPeriod.Any(y => y.CoNotificationId == x.CoNotificationId));

                    countDeliveredThisPeriod = deliveredThisPeriod.Count() + coNotifiedCount;

                    var twoWeeksAgo = today.Subtract(TimeSpan.FromDays(14));
                    countCreatedByNotDeliveredCurrently = allNotifications
                        .Count(x =>
                                //Show like 14 days back to allow this to catch up over a weekend or similar
                                x.TransactionDate > twoWeeksAgo
                                //Not already delivered
                                && !x.OutgoingCreditNotificationDeliveryFileHeaderId.HasValue
                                // Co-notification slaves are not delivered on their own at all
                                && !(x.IsCoNotificationMaster == false && x.CoNotificationId != null));

                    countNotNotifiedCurrently = notificationGroups.Sum(x => x.Count);


                    nrOfEnabledCreditTypes += 1;

                    setModel(new NotificationsInitialDataProductModel
                    {
                        countDeliveredThisPeriod = countDeliveredThisPeriod,
                        countCreatedByNotDeliveredCurrently = countCreatedByNotDeliveredCurrently,
                        countNotNotifiedCurrently = countNotNotifiedCurrently,
                        notificationApiUrl = notificationApiUrl,
                        skipReasonsByCreditNr = skipReasonsByCreditNr,
                        notificationGroups = notificationGroups
                    });
                }
            };

            var m = new NotificationsInitialDataModel
            {

            };

            if (NEnv.IsUnsecuredLoansEnabled)
                handleType(CreditType.UnsecuredLoan, x => m.unsecuredLoans = x);
            if (NEnv.IsCompanyLoansEnabled)
                handleType(CreditType.CompanyLoan, x => m.companyLoans = x);
            if (NEnv.IsMortgageLoansEnabled)
                handleType(CreditType.MortgageLoan, x => m.mortgageLoans = x);

            m.getNotificationFilesPageUrl = Url.Action("GetNotificationFilesPage", "ApiCreateCreditNotifications");
            m.nrOfEnabledCreditTypes = nrOfEnabledCreditTypes.ToString(); //NOTE: This was a string so we kept that. Seems just plain wrong though. Can we not just get rid of this whole product thing?

            return m;
        }

        private class NotificationsInitialDataModel
        {
            public NotificationsInitialDataProductModel unsecuredLoans { get; set; }
            public NotificationsInitialDataProductModel companyLoans { get; set; }
            public NotificationsInitialDataProductModel mortgageLoans { get; set; }
            public string getNotificationFilesPageUrl { get; set; }
            public string nrOfEnabledCreditTypes { get; set; }
        }

        private class NotificationsInitialDataProductModel
        {
            public int? countDeliveredThisPeriod { get; set; }
            public int? countCreatedByNotDeliveredCurrently { get; set; }
            public int? countNotNotifiedCurrently { get; set; }
            public string notificationApiUrl { get; set; }
            public Dictionary<string, string> skipReasonsByCreditNr { get; set; }
            public List<List<string>> notificationGroups { get; set; }
        }

        [HttpPost]
        [Route("Api/ScheduledTasks/Fetch-SendCreditsToDebtCollection-InitialData")]
        public ActionResult FetchSendCreditsToDebtCollectionInitialData()
        {
            int eligableForDebtCollectionCount;
            using (var context = CreateCreditContext())
            {
                eligableForDebtCollectionCount = Service.DebtCollectionCandidate.GetEligibleForDebtCollectionCount(context);
                return Json2(new
                {
                    eligableForDebtCollectionCount
                });
            }
        }

        [Route("Api/SatExport/ExportStatus")]
        [HttpPost]
        public ActionResult GetSatExportStatus()
        {
            if (!NEnv.IsUnsecuredLoansEnabled)
                throw new NotImplementedException();

            using (var context = new CreditContext())
            {
                var nrOfActiveCredits = context
                    .CreditHeaders
                    .Where(x => x.Status == CreditStatus.Normal.ToString())
                    .Count();

                return Json2(new
                {
                    nrOfActiveCredits,
                    exportProfileName = NEnv.SatExportProfileName,
                });
            }
        }

        [HttpPost]
        [Route("Api/TrapetsAmlExport/ExportStatus")]
        public ActionResult GetTrapetsAmlExportExportStatus()
        {
            if (!NEnv.IsUnsecuredLoansEnabled)
                throw new NotImplementedException();

            using (var context = new CreditContext())
            {
                return Json2(new
                {
                    exportProfileName = NEnv.TrapetsAmlExportProfileName
                });
            }
        }

        [HttpPost]
        [Route("Api/Cm1AmlExport/ExportStatus")]
        public ActionResult GetCm1AmlExportExportStatus()
        {
            var profiles = CreditCm1AmlExportService.GetExportProfiles(NTechEnvironmentLegacy.SharedInstance);
            return Json2(new
            {
                TransactionsExportProfile = profiles.TransactionsExportProfile,
                CustomerExportProfile = profiles.CustomerExportProfile
            });
        }

        [HttpPost]
        [Route("Api/BookkeepingFiles/ExportStatus")]
        public ActionResult GetBookkeepingExportStatus()
        {
            using (var context = new CreditContextExtended(GetCurrentUserMetadata(), Clock))
            {
                return Json2(new
                {
                    Dates = BookKeepingFileManager.GetDatesToHandle(context),
                    ExportProfileName = NEnv.BookKeepingFileExportProfileName,
                });
            }
        }

        [HttpPost]
        [Route("Api/DailyKycScreen/Status")]
        public ActionResult GetDailyKycScreenStatus()
        {
            var screenDate = Clock.Today;
            var kycService = this.Service.Kyc;

            using (var context = new CreditContextExtended(GetCurrentUserMetadata(), Clock))
            {
                ISet<int> activeCustomerIds;
                if (NEnv.IsCompanyLoansEnabled)
                    activeCustomerIds = kycService.CompanyLoanFetchAllActiveCustomerIds(context);
                else
                    activeCustomerIds = kycService.FetchAllActiveCustomerIds(context);

                var dailySummary = this.Service.Kyc.FetchCustomerKycStatusChanges(activeCustomerIds, screenDate);

                return Json2(new
                {
                    UnscreenedCount = dailySummary.TotalScreenedCount > activeCustomerIds.Count ? 0 : (activeCustomerIds.Count - dailySummary.TotalScreenedCount)
                });
            }
        }

        [HttpPost]
        [Route("Api/CreditAnnualStatements/Fetch-Initial-Data")]
        public ActionResult FetchCreditAnnualStatementsInitialData()
        {
            if (!LoanStandardAnnualSummaryService.IsAnnualStatementFeatureEnabled(NEnv.ClientCfgCore, NEnv.EnvSettings))
                return HttpNotFound();

            var lastYear = Clock.Today.Year - 1;
            var s = Service.LoanStandardAnnualSummary;
            return Json2(new
            {
                IsExportAllowed = !s.IsExportCreatedForYear(lastYear)
            });
        }
    }
}