using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    public partial class CreditManagementController
    {
        [Route("CreditApplications")]
        public ActionResult CreditApplications(
            int? workListDataWaitDays,
            int? creditApplicationWorkListIsNewMinutes,
            bool? showCategoryCodes = false)
        {
            if (NEnv.IsStandardUnsecuredLoansEnabled)
                return HttpNotFound();

            var repo = CreateRepo();
            SetInitialData(new
            {
                showCategoryCodes = showCategoryCodes,
                providers = repo.GetAllProviders(),
                categoryCodes = repo.GetAllCategoryCodes(),
                filterUrl = Url.Action("Filter", new { includeCategoryCodes = showCategoryCodes }),
                workListDataWaitDays = workListDataWaitDays,
                creditApplicationWorkListIsNewMinutes = creditApplicationWorkListIsNewMinutes,
                testFindRandomApplicationUrl = Url.Action("FindRandomApplication", "CreditManagement"),
                isTest = !NEnv.IsProduction
            });
            return View();
        }

        [Route("Filter")]
        [HttpPost]
        public ActionResult Filter(
            string providerName,
            string creditApplicationCategoryCode,
            int pageSize,
            int pageNr,
            string omniSearchValue,
            bool? includeCategoryCodes = false)
        {
            if (NEnv.IsStandardUnsecuredLoansEnabled)
                return HttpNotFound();

            var creditApplicationsUrl = Url.Action("CreditApplications", new { });
            Func<string, string> getAppUrlFromAppNr = nr => Url.Action("CreditApplication", new { applicationNr = nr });
            var now = Clock.Now;

            var repo = CreateRepo();

            if (!string.IsNullOrWhiteSpace(omniSearchValue))
            {
                return Json2(repo.GetOmniSearchPage(omniSearchValue, getAppUrlFromAppNr, includeCategoryCodes: includeCategoryCodes.GetValueOrDefault()));
            }
            else
            {
                var filter = new CreditManagementWorkListService.CreditManagementFilter
                {
                    CategoryCode = creditApplicationCategoryCode,
                    ProviderName = providerName
                };
                return Json2(repo.GetFilteredPage(filter, pageSize, pageNr, getAppUrlFromAppNr, includeCategoryCodes: includeCategoryCodes.GetValueOrDefault()));
            }
        }

        [Route("FindRandomApplication")]
        [HttpPost]
        public ActionResult FindRandomApplication(string applicationType)
        {
            if (NEnv.IsProduction)
                return HttpNotFound();

            if (NEnv.IsStandardUnsecuredLoansEnabled)
                return HttpNotFound();

            Func<bool, bool, string> fetchRandomApplicationWithAcceptedOrRejectedCreditDecision = (isAccepted, wasAutomated) =>
                {
                    using (var context = new PreCreditContext())
                    {
                        return context
                            .Database
                            .SqlQuery<string>(
@"with Tmp
as
(
	select	ApplicationNr,
			Discriminator,
			WasAutomated,
			ROW_NUMBER() OVER (PARTITION BY ApplicationNr Order by Id desc) as RankNr
	from	CreditDecision
)
select	top 1 t.ApplicationNr
from	Tmp t
where	t.RankNr = 1
and		t.WasAutomated = @p0
and		t.Discriminator = @p1
order by newid()", wasAutomated, isAccepted ? "AcceptedCreditDecision" : "RejectedCreditDecision")
                            .SingleOrDefault();
                    }
                };

            Func<bool, string> fetchRandomCancelledApplication = wasAutomated =>
            {
                using (var context = new PreCreditContext())
                {
                    return context
                        .Database
                        .SqlQuery<string>(
@"with Tmp
as
(
	select	ApplicationNr,
			WasAutomated,
			ROW_NUMBER() OVER (PARTITION BY ApplicationNr Order by Id desc) as RankNr
	from	CreditApplicationCancellation
)
select	top 1 t.ApplicationNr
from	Tmp t
where	t.RankNr = 1
and		t.WasAutomated = @p0
order by newid()", wasAutomated)
                        .SingleOrDefault();
                }
            };

            Func<string, string> createRedirectToUrl = an => Url.Action("CreditApplication", "CreditManagement", new { applicationNr = an });

            if (applicationType.IsOneOf("autorejected", "manuallyrejected", "autoapproved", "manuallyapproved"))
            {
                var isAccepted = applicationType.EndsWith("approved");
                var wasAutomated = applicationType.StartsWith("auto");
                var applicationNr = fetchRandomApplicationWithAcceptedOrRejectedCreditDecision(isAccepted, wasAutomated);
                return Json2(new { redirectToUrl = applicationNr == null ? null : createRedirectToUrl(applicationNr) });
            }
            else if (applicationType.IsOneOf("autocancelled", "manuallycancelled"))
            {
                var wasAutomated = applicationType.StartsWith("auto");
                var applicationNr = fetchRandomCancelledApplication(wasAutomated);
                return Json2(new { redirectToUrl = applicationNr == null ? null : createRedirectToUrl(applicationNr) });
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such applicationType");
            }
        }

        private CreditManagementWorkListService CreateRepo()
        {
            return Service.Resolve<CreditManagementWorkListService>();
        }
    }
}