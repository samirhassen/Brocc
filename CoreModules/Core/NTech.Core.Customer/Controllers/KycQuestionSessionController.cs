using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using NTech.Core.Customer.Shared.Services;
using NTech.Core.Module.Shared.Clients;

namespace NTech.Core.Customer.Controllers;

[ApiController]
[NTechRequireFeatures(RequireFeaturesAll = new[] { "feature.customerpages.kyc" })]
[Route("Api/Customer/KycQuestionSession")]
public class KycQuestionSessionController : Controller
{
    private readonly KycQuestionsSessionService _kycQuestionsSessionService;
    private readonly KycQuestionsTemplateService _templateService;

    public KycQuestionSessionController(KycQuestionsSessionService kycQuestionsSessionService,
        KycQuestionsTemplateService templateService)
    {
        _kycQuestionsSessionService = kycQuestionsSessionService;
        _templateService = templateService;
    }

    [HttpPost]
    [Route("LoadCustomerPagesSession")]
    public CustomerPagesKycQuestionSessionResponse LoadCustomerPagesSession(KycQuestionSessionRequest request) =>
        _kycQuestionsSessionService.LoadCustomerPagesSession(request?.SessionId, _templateService);

    [HttpPost]
    [Route("Fetch")]
    public KycQuestionsSession Load(KycQuestionSessionRequest request) =>
        _kycQuestionsSessionService.GetSession(request?.SessionId);

    [HttpPost]
    [Route("CreateSession")]
    public KycQuestionsSession CreateSession(CreateKycQuestionSessionRequest request) =>
        _kycQuestionsSessionService.CreateSession(request);

    [HttpPost]
    [Route("AddAlternateKey")]
    public ActionResult AddAlternateKey(CustomerPagesKycQuestionSessionAlternateKeyRequest request)
    {
        _kycQuestionsSessionService.AddAlternateKey(request.SessionId, request.AlternateKey);
        return Ok();
    }

    [HttpPost]
    [Route("HandleAnswers")]
    public CustomerPagesHandleKycQuestionAnswersResponse HandleAnswers(
        CustomerPagesHandleKycQuestionAnswersRequest request) =>
        _kycQuestionsSessionService.HandleSessionAnswers(request);
}

public class CustomerPagesKycQuestionSessionAlternateKeyRequest
{
    [Required] public string SessionId { get; set; }
    [Required] public string AlternateKey { get; set; }
}

public class KycQuestionSessionRequest
{
    [Required] public string SessionId { get; set; }
}