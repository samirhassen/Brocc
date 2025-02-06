using Microsoft.AspNetCore.Mvc;
using NTech.Core.Host.Infrastructure;
using NTech.Core.Module;
using System.ComponentModel.DataAnnotations;

namespace NTech.Core.Host.Controllers
{
    [ApiController]
    public class TimeMachineController : Controller
    {
        private readonly NEnv env;
        private readonly CoreHostClock clock;

        public TimeMachineController(NEnv env, CoreHostClock clock)
        {
            this.env = env;
            this.clock = clock;
        }

        /// <summary>
        /// Receive time change from the test module. Does nothing in production
        /// </summary>
        [HttpPost]
        [Route("Api/Set-TimeMachine-Time")]
        public IActionResult SetTimeMachineTime(TimeMachineSetTimeRequest request)
        {
            if (env.IsProduction)
            {
                return NotFound();
            }

            if (request?.Now != null)
            {
                clock.OnTimeMachineUpdate(request.Now.Value);
            }

            return Ok();
        }
    }

    public class TimeMachineSetTimeRequest
    {
        [Required]
        public DateTimeOffset? Now { get; set; }
    }
}
