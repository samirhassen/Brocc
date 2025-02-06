using Microsoft.AspNetCore.Mvc;
using NTech.Core.Module.Infrastrucutre;

namespace NTech.Core.Host.Controllers
{
    [ApiController]
    public class TelemetryController : Controller
    {
        private readonly TelemetryService telemetryService;

        public TelemetryController(TelemetryService telemetryService)
        {
            this.telemetryService = telemetryService;
        }

        /// <summary>
        /// Telemetry log (invoice metrics and such)
        /// </summary>
        [HttpPost]
        [Route("Api/Telemetry/LogData")]
        public async Task<OkResult> CreateBatch(LogTelemetryDataRequest request)
        {
            await telemetryService.LogTelemetryDataAsync(request);
            return Ok();
        }
    }
}
