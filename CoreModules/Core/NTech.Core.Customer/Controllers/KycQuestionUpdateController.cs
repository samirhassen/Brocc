using Microsoft.AspNetCore.Mvc;
using NTech.Core.Customer.Shared.Services;
using NTech.Core.Module.Shared.Clients;
using System.ComponentModel.DataAnnotations;

namespace NTech.Core.Customer.Controllers
{
    [ApiController]
    [NTechRequireFeatures(RequireFeaturesAll = new[] { "feature.customerpages.kyc" })]
    public class KycQuestionUpdateController : Controller
    {
        private readonly KycAnswersUpdateService answersUpdateService;

        public KycQuestionUpdateController(KycAnswersUpdateService answersUpdateService)
        {
            this.answersUpdateService = answersUpdateService;
        }

        [HttpPost]
        [Route("Api/Customer/KycQuestionUpdate/GetCustomerStatus")]
        public KycQuestionsPeriodicUpdateService.CustomerPagesCustomerStatus GetCustomerPagesStatusForCustomer(KycQuestionCustomerStatusRequest request) =>
            answersUpdateService.GetCustomerPagesStatusForCustomer(request.CustomerId.Value);

        [HttpPost]
        [Route("Api/Customer/KycQuestionUpdate/UpdateAnswers")]
        public KycQuestionsPeriodicUpdateService.CustomerPagesCustomerStatus GetCustomerPagesStatusForCustomer(KycQuestionUpdateAnswersRequest request)
        {
            answersUpdateService.AddCustomerQuestionsSetFromCustomerPages(request.CustomerId.Value, request.RelationType,
                request.RelationId, request.Answers);

            return answersUpdateService.GetCustomerPagesStatusForCustomer(request.CustomerId.Value);
        }
    }

    public class KycQuestionCustomerStatusRequest
    {
        [Required]
        public int? CustomerId { get; set; }
    }

    public class KycQuestionUpdateAnswersRequest
    {
        [Required]
        public int? CustomerId { get; set; }

        [Required]
        public string RelationType { get; set; }

        [Required]
        public string RelationId { get; set; }

        [Required]
        public List<CustomerQuestionsSetItem> Answers { get; set; }
    }
}
