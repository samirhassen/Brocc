using NTech.Core.Module.Shared.Clients;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Mvc;

namespace nCustomer.Controllers
{
    [NTechAuthorize]
    public class KycManagementApiController : NController
    {
        [HttpPost]
        [Route("Api/KycManagement/FetchLocalDecisionCurrentData")]
        public ActionResult FetchLocalDecisionData(int customerId)
        {
            return Json2(this.Service.KycManagement.FetchLocalDecisionCurrentData(customerId));
        }

        [HttpPost]
        [Route("Api/KycManagement/FetchCustomerOnboardingStatuses")]
        public ActionResult FetchCustomerOnboardingStatuses(List<int> customerIds, string kycQuestionsSourceType, string kycQuestionsSourceId, bool? includeLatestQuestionSets)
        {
            if (customerIds == null)
                customerIds = new List<int>();
            (string SourceType, string SourceId)? kycQuestionsSource = null;
            if (!string.IsNullOrWhiteSpace(kycQuestionsSourceType) && !string.IsNullOrWhiteSpace(kycQuestionsSourceId))
            {
                kycQuestionsSource = (SourceType: kycQuestionsSourceType, SourceId: kycQuestionsSourceId);
            }
            return Json2(this.Service.KycManagement.FetchKycCustomerOnboardingStatuses(new HashSet<int>(customerIds), kycQuestionsSource, includeLatestQuestionSets ?? false));
        }

        [HttpPost]
        [Route("Api/KycManagement/SetLocalDecision")]
        public ActionResult SetLocalDecision(int customerId, bool isModellingPep, bool currentValue, bool includeNewCurrentData)
        {
            this.Service.KycManagement.SetCurrentLocalDecision(customerId, isModellingPep, currentValue);
            if (includeNewCurrentData)
                return Json2(new { NewCurrentData = this.Service.KycManagement.FetchLocalDecisionCurrentData(customerId) });
            else
                return Json2(new { });
        }

        [HttpPost]
        [Route("Api/KycManagement/FetchLocalDecisionHistoryData")]
        public ActionResult FetchLocalDecisionHistoryData(int customerId, bool isModellingPep)
        {

            return Json2(this.Service.KycManagement.FetchLocalDecisionHistoryData(customerId, isModellingPep, GetUserDisplayNameByUserId));
        }

        [HttpPost]
        [Route("Api/KycManagement/AddCustomerQuestionsSet")]
        public ActionResult AddCustomerQuestionsSet(CustomerQuestionsSet customerQuestionsSet, string sourceType, string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceType) || string.IsNullOrWhiteSpace(sourceId))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing sourceType or sourceId");

            return Json2(new
            {
                key = this.Service.KycManagement.AddCustomerQuestionsSet(customerQuestionsSet, sourceType, sourceId)
            });
        }

        [HttpPost]
        [Route("Api/KycManagement/CopyCustomerQuestionsSetIfNotExists")]
        public ActionResult CopyCustomerQuestionsSetIfNotExists(List<int> customerIds, string fromSourceType, string fromSourceId, string toSourceType, string toSourceId, DateTime? ignoreOlderThanDate)
        {
            if (string.IsNullOrWhiteSpace(fromSourceType) || string.IsNullOrWhiteSpace(fromSourceId) || string.IsNullOrWhiteSpace(toSourceType) ||
                string.IsNullOrWhiteSpace(toSourceId) || customerIds == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "sourceType, sourceId or customerIds");

            var wasCopiedByCustomerId = Service.KycAnswersUpdate.CopyCustomerQuestionsSetIfNotExists(customerIds.ToHashSetShared(), fromSourceType, fromSourceId, toSourceType, toSourceId, ignoreOlderThanDate);

            return Json2(new
            {
                wasCopiedByCustomerId
            });
        }

        [HttpPost]
        [Route("Api/KycManagement/AddCustomerQuestionsSetBatch")]
        public ActionResult AddCustomerQuestionsSetBatch(List<CustomerQuestionsSet> customerQuestionsSets, string sourceType, string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceType) || string.IsNullOrWhiteSpace(sourceId))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing sourceType or sourceId");

            foreach (var q in customerQuestionsSets)
            {
                if (string.IsNullOrWhiteSpace(q.Source))
                {
                    q.Source = $"{sourceType} {sourceId}";
                }
            }

            var keys = new List<string>();
            foreach (var customerQuestionsSet in customerQuestionsSets)
                keys.Add(Service.KycManagement.AddCustomerQuestionsSet(customerQuestionsSet, sourceType, sourceId));

            return Json2(new { keys });
        }

        [HttpPost]
        [Route("Api/KycManagement/FetchLatestCustomerQuestionsSet")]
        public ActionResult FetchLatestCustomerQuestionsSet(int customerId)
        {
            return Json2(this.Service.KycManagement.FetchLatestCustomerQuestionsSet(customerId));
        }

        [HttpPost]
        [Route("Api/KycManagement/FetchLatestTrapetsQueryResult")]
        public ActionResult FetchLatestTrapetsQueryResult(int customerId)
        {
            return Json2(this.Service.KycScreeningManagement.FetchLatestQueryResult(customerId));
        }

        [HttpPost]
        [Route("Api/KycManagement/FetchTrapetsQueryHistorySummary")]
        public ActionResult FetchTrapetsQueryHistorySummary(int customerId)
        {
            return Json2(this.Service.KycScreeningManagement.FetchQueryHistorySummary(customerId));
        }

        [HttpPost]
        [Route("Api/KycManagement/FetchPropertiesWithGroupedEditHistory")]
        public ActionResult FetchPropertiesWithGroupedEditHistory(int customerId, List<string> propertyNames)
        {
            return Json2(this.Service.KycManagement.FetchPropertiesWithGroupedEditHistory(customerId, propertyNames, GetUserDisplayNameByUserId));
        }
    }
}