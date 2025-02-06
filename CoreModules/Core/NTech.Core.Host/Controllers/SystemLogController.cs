using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTech.Core.Host.Infrastructure;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace NTech.Core.Host.Controllers
{
    [ApiController]
    public class SystemLogController : Controller
    {
        private readonly SystemLogService systemLogService;
        private readonly NEnv env;

        public SystemLogController(SystemLogService systemLogService, NEnv env)
        {
            this.systemLogService = systemLogService;
            this.env = env;
        }

        /// <summary>
        /// Add items to the system log (error log among other things)
        /// </summary>
        [HttpPost]
        [Route("Api/SystemLog/Create-Batch")]
        public async Task<OkResult> CreateBatch(SystemLogCreateBatchRequest request)
        {
            await systemLogService.LogBatchAsync(request.Items);

            return Ok();
        }

        [HttpPost]
        [Route("Api/SystemLog/Create-Batch-Legacy")]
        [AllowAnonymous]
        public async Task<ActionResult> CreateBatchLegacy(SystemLogCreateBatchRequest request)
        {
            var isEnabled = (env.OptionalSetting("ntech.systemlog.legacyendpoint.enabled") ?? "false").ToLowerInvariant() == "true";
            if (!isEnabled)
                return NotFound();
            //TODO: Change NTechSerilogSink in the NTech.Services.Infrastructure to have an access token and then drop this endpoint
            //      Note that the nAudit endpoint we are moving from is also AllowAnonymous
            return await CreateBatch(request);
        }

        [HttpPost]
        [Route("Api/SystemLog/Fetch-Latest-Errors")]
        public async Task<List<SystemLogItem>> FetchLatestErrors(FetchLatestErrorsRequest request) =>
            await systemLogService.FetchLatestErrorsAsync(page: request?.Page ?? 0);
    }

    public class SystemLogCreateBatchRequest
    {
        [Required]
        public List<AuditClientSystemLogItem> Items { get; set; }
    }

    public class FetchLatestErrorsRequest
    {
        public int Page { get; set; }
    }
}
