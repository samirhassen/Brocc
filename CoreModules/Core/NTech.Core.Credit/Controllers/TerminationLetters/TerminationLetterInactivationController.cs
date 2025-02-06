using Microsoft.AspNetCore.Mvc;
using nCredit.DbModel.BusinessEvents;
using NTech.Core.Credit.Shared.Services;

namespace NTech.Core.Credit.Controllers.TerminationLetters
{
    [ApiController]
    public class TerminationLetterInactivationController : Controller
    {
        private readonly TerminationLetterInactivationService service;

        public TerminationLetterInactivationController(TerminationLetterInactivationService service)
        {
            this.service = service;
        }

        [HttpPost]
        [Route("Api/Credit/TerminationLetters/Inactivate-On-Credits")]
        public InactivateTerminationLettersResult InactivateTerminationLetters(InactivateTerminationLettersRequest request) =>
            service.InactivateTerminationLetters(request);

        [HttpPost]
        [Route("Api/Credit/TerminationLetters/Postpone")]
        public PostponeTerminationLettersResponse PostponeTerminationLetters(PostponeTerminationLettersRequest request) =>
            service.PostponeTerminationLetters(request);

        [HttpPost]
        [Route("Api/Credit/TerminationLetters/Resume")]
        public ResumeTerminationLettersResponse ResumeTerminationLetters(ResumeTerminationLettersRequest request) =>
            service.ResumeTerminationLetters(request);
    }
}
