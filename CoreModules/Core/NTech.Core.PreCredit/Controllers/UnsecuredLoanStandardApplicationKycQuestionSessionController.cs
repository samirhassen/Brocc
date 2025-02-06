using Microsoft.AspNetCore.Mvc;
using NTech.Core.PreCredit.Shared.Services;
using NTech.Core.PreCredit.Shared.Services.UlStandard;
using System.ComponentModel.DataAnnotations;

namespace NTech.Core.PreCredit.Apis
{
    [ApiController]
    public class UnsecuredLoanStandardApplicationKycQuestionSessionController : Controller
    {
        private readonly UnsecuredLoanStandardApplicationKycQuestionSessionService service;
        private readonly KycQuestionsSessionCompletionCallbackService completionService;

        public UnsecuredLoanStandardApplicationKycQuestionSessionController(UnsecuredLoanStandardApplicationKycQuestionSessionService sessionService, 
            KycQuestionsSessionCompletionCallbackService completionService)
        {
            this.service = sessionService;
            this.completionService = completionService;
        }

        [HttpPost]
        [Route("Api/PreCredit/UnsecuredLoanStandard/Create-Application-KycQuestionSession")]
        public UlStandardKycSessionResponse CreateSession(UlStandardKycSessionRequest request)
        {
            var session = service.CreateSession(request?.ApplicationNr, request?.CustomerId);
            return new UlStandardKycSessionResponse
            {
                SessionId = session.SessionId
            };
        }

        [HttpPost]
        [Route("Api/PreCredit/UnsecuredLoanStandard/OnKycQuestionSessionCompleted")]
        public ActionResult CompleteKycSession(CompleteKycSessionRequest request)
        {
            completionService.HandleKycQuestionSessionCompleted(request.SessionId);
            return Ok();
        }
    }

    public class UlStandardKycSessionRequest
    {
        [Required]
        public int? CustomerId { get; set; }

        [Required]
        public string ApplicationNr { get; set; }
    }

    public class UlStandardKycSessionResponse
    {
        public string SessionId { get; set; }
    }

    public class CompleteKycSessionRequest
    {
        [Required]
        public string SessionId { get; set; }
    }
}
