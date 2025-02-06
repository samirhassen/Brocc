using Microsoft.AspNetCore.Mvc;
using NTech.Core.Module.Shared.Infrastructure;

namespace NTech.Core.PreCredit.Controllers
{
    [ApiController]
    public class LoanObjectivesController: Controller
    {
        private readonly IClientConfigurationCore clientConfiguration;

        public LoanObjectivesController(IClientConfigurationCore clientConfiguration)
        {
            this.clientConfiguration = clientConfiguration;
        }
        
        [HttpPost]
        [Route("Api/PreCredit/LoanObjectives/All")]
        public List<string> LoanObjectives() => 
            clientConfiguration.GetRepeatedCustomValue("LoanObjectives", "LoanObjective");
    }
}
