using Microsoft.AspNetCore.Mvc;
using NTech.Core.Customer.Shared.Services;
using NTech.Core.Module.Shared.Clients;
using System.ComponentModel.DataAnnotations;

namespace NTech.Core.Customer.Controllers
{
    [ApiController]
    [NTechRequireFeatures(RequireFeaturesAll = new[] { "feature.customerpages.kyc" })]
    public class KycQuestionSessionController : Controller
    {
        private readonly KycQuestionsSessionService kycQuestionsSessionService;
        private readonly KycQuestionsTemplateService templateService;

        public KycQuestionSessionController(KycQuestionsSessionService kycQuestionsSessionService,
            KycQuestionsTemplateService templateService)
        {
            this.kycQuestionsSessionService = kycQuestionsSessionService;
            this.templateService = templateService;
        }

        [HttpPost]
        [Route("Api/Customer/KycQuestionSession/LoadCustomerPagesSession")]
        public CustomerPagesKycQuestionSessionResponse LoadCustomerPagesSession(KycQuestionSessionRequest request) =>
            kycQuestionsSessionService.LoadCustomerPagesSession(request?.SessionId, templateService);

        [HttpPost]
        [Route("Api/Customer/KycQuestionSession/Fetch")]
        public KycQuestionsSession Load(KycQuestionSessionRequest request) =>
            kycQuestionsSessionService.GetSession(request?.SessionId);

        [HttpPost]
        [Route("Api/Customer/KycQuestionSession/CreateSession")]
        public KycQuestionsSession CreateSession(CreateKycQuestionSessionRequest request) =>
            kycQuestionsSessionService.CreateSession(request);

        [HttpPost]
        [Route("Api/Customer/KycQuestionSession/AddAlternateKey")]
        public ActionResult AddAlternateKey(CustomerPagesKycQuestionSessionAlternateKeyRequest request)
        {
            kycQuestionsSessionService.AddAlternateKey(request.SessionId, request.AlternateKey);
            return Ok();
        }

        [HttpPost]
        [Route("Api/Customer/KycQuestionSession/HandleAnswers")]
        public CustomerPagesHandleKycQuestionAnswersResponse HandleAnswers(CustomerPagesHandleKycQuestionAnswersRequest request) =>
            kycQuestionsSessionService.HandleSessionAnswers(request);
    }

    public class CustomerPagesKycQuestionSessionAlternateKeyRequest
    {
        [Required]
        public string SessionId { get; set; }
        [Required]
        public string AlternateKey { get; set; }
    }

    public class KycQuestionSessionRequest
    {
        [Required]
        public string SessionId { get; set; }
    }
}
