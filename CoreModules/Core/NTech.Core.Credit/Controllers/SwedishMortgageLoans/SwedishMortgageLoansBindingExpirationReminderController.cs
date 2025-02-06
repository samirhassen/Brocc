using Microsoft.AspNetCore.Mvc;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Infrastructure;

namespace NTech.Core.Credit.Controllers.SwedishMortgageLoans
{
    [ApiController]
    [NTechRequireFeatures(RequireClientCountryAny = new[] { "SE" }, RequireFeaturesAll = new[] { "ntech.feature.mortgageloans.standard" })]
    public class SwedishMortgageLoansBindingExpirationReminderController : Controller
    {
        private readonly BoundInterestExpirationReminderService service;

        public SwedishMortgageLoansBindingExpirationReminderController(BoundInterestExpirationReminderService service)
        {
            this.service = service;
        }

        [HttpPost]
        [Route("Api/Credit/SeMortgageLoans/Send-BoundInterest-Expiration-Reminders")]
        public SwedishMortgageLoansBindingExpirationReminderResponse SendReminders()
        {
            var reminderCount = service.SendReminderMessages();
            return new SwedishMortgageLoansBindingExpirationReminderResponse
            {
                SuccessCount = reminderCount
            };
        }
    }

    public class SwedishMortgageLoansBindingExpirationReminderResponse : ScheduledJobResult
    {
        public int SuccessCount { get; set; }
    }
}
