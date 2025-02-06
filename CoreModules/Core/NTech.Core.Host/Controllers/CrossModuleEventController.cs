using Microsoft.AspNetCore.Mvc;
using NTech.Core.Host.Infrastructure;

namespace NTech.Core.Host.Controllers
{
    [ApiController]
    public class CrossModuleEventController : Controller
    {
        private readonly ICrossModuleEventQueue eventQueue;

        public CrossModuleEventController(ICrossModuleEventQueue eventQueue)
        {
            this.eventQueue = eventQueue;
        }

        [HttpPost]
        [Route("Api/Common/ReceiveEvent")]
        public async Task<IActionResult> ReceiveEvent(CrossModuleEvent request)
        {
            if (request != null)
            {
                await eventQueue.QueueEventAsync(request);
            }
            return Ok();
        }
    }
}
