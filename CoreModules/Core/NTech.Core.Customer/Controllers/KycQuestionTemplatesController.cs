using Microsoft.AspNetCore.Mvc;
using NTech.Core.Customer.Shared.Services;

namespace NTech.Core.Customer.Controllers
{
    [ApiController]
    [NTechRequireFeatures(RequireFeaturesAll = new[] { "feature.customerpages.kyc" })]
    public class KycQuestionTemplatesController : Controller
    {
        private readonly KycQuestionsTemplateService templateService;

        public KycQuestionTemplatesController(KycQuestionsTemplateService templateService)
        {
            this.templateService = templateService;
        }

        [HttpPost]
        [Route("Api/Customer/Kyc/QuestionTemplates/Get-All")]
        public KycQuestionsTemplateInitialDataResponse GetInitialData() => templateService.GetInitialData();

        [HttpPost]
        [Route("Api/Customer/Kyc/QuestionTemplates/Set")]
        public SaveQuestionsResponse SaveQuestions(SaveQuestionsRequest request) => templateService.SaveQuestions(request);

        [HttpPost]
        [Route("Api/Customer/Kyc/QuestionTemplates/Get-ModelData")]
        public GetModelDataResponse GetModelData(GetModelDataRequest request) => templateService.GetModelData(request);

        [HttpPost]
        [Route("Api/Customer/Kyc/QuestionTemplates/Validate-Template")]
        public ValidateTemplateResponse Validate(ValidateTemplateRequest request) => templateService.Validate(request);
    }
}
