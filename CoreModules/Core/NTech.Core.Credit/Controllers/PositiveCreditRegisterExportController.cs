using Microsoft.AspNetCore.Mvc;
using NTech.Core.Credit.Shared.Services.PositiveCreditRegister;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure.Email;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NTech.Core.Credit.Controllers
{
    [ApiController]
    [NTechRequireFeatures(RequireFeaturesAll = new[] { "ntech.feature.ullegacy", "ntech.feature.positivecreditregister" }, RequireClientCountryAny = new[] { "FI" })]
    public class PositiveCreditRegisterExportController : Controller
    {
        private readonly PositiveCreditRegisterExportService service;
        private readonly INTechEmailServiceFactory emailServiceFactory;
        private readonly PcrLoggingService pcrLoggingService;

        public PositiveCreditRegisterExportController(PositiveCreditRegisterExportService service, INTechEmailServiceFactory emailServiceFactory, PcrLoggingService pcrLoggingService)
        {
            this.service = service;
            this.emailServiceFactory = emailServiceFactory;
            this.pcrLoggingService = pcrLoggingService;
        }

        [HttpPost]
        [Route("Api/Credit/PositiveCreditRegisterExport")]
        public PositiveCreditRegisterExportResponse PositiveCreditRegisterExport()
        {
            var (SuccessCount, Warnings) = service.ExportAllBatches();

            return new PositiveCreditRegisterExportResponse
            {
                SuccessCount = SuccessCount,
                Warnings = Warnings,
                Errors = new List<string>()
            };
        }

        [HttpPost]
        [Route("Api/Credit/PositiveCreditRegisterExport/CheckBatchStatus")]
        public PositiveCreditRegisterExportResponse PositiveCreditRegisterExportCheckBatchStatus()
        {
            var (SuccessCount, Warnings) = service.GetAllBatchStatusReportingFailedBatches(emailServiceFactory);

            return new PositiveCreditRegisterExportResponse
            {
                SuccessCount = SuccessCount,
                Warnings = Warnings,
                Errors = new List<string>()
            };
        }


        [HttpPost]
        [Route("Api/Credit/PositiveCreditRegisterExport/GetLoan")]
        public FetchGetLoanResponse FetchGetLoan(FetchGetLoanRequest request)
        {
            var rawResponse = service.FetchRawGetLoanResponse(request.CreditNr);

            return new FetchGetLoanResponse
            {
                IsSuccess = true,
                RawResponse = rawResponse
            };
        }

        [HttpPost]
        [Route("Api/Credit/PositiveCreditRegisterExport/ManualExport")]
        public ManualPositiveCreditRegisterExportResponse ManualPositiveCreditRegisterExport(ManualPositiveCreditRegisterExportRequest request)
        {
            if (request.IsFirstTimeExport)
            {
                var (SuccessCount, RawResponse) = service.ExportFirstTime(request.FromDate, request.ToDate);

                return new ManualPositiveCreditRegisterExportResponse
                {
                    SuccessCount = SuccessCount,
                    RawResponse = RawResponse
                };
            }

            else
            {
                var (SuccessCount, Warnings) = service.ExportAllBatches(request.FromDate, request.ToDate);

                return new ManualPositiveCreditRegisterExportResponse
                {
                    SuccessCount = SuccessCount,
                    RawResponse = null
                };
            }
        }

        [HttpPost]
        [Route("Api/Credit/PositiveCreditRegisterExport/GetLogs")]
        public GetLogsResponse GetLogs(GetLogsRequest request)
        {
            var systemLogs = service.GetSystemLogs(request.NumberOfLogs);
            return new GetLogsResponse
            {
                StatusLogs = systemLogs.StatusLogs, 
                ExportLogs = systemLogs.ExportLogs
            };
        }

        [HttpPost]
        [Route("Api/Credit/PositiveCreditRegisterExport/GetBatchLogs")]
        public GetPcrBatchLogsResponse GetBatchLogs(GetPcrBatchLogsRequest request) => pcrLoggingService.GetBatchLogs(request);

        [HttpPost]
        [Route("Api/Credit/PositiveCreditRegisterExport/BatchLogFileContent")]
        public FileContentResult GetBatchLogContent(PcrBatchLogContentRequest request)
        {
            var logfileContent = pcrLoggingService.GetLogfileContent(request.LogCorrelationId, request.Filename);
            return new FileContentResult(Encoding.UTF8.GetBytes(logfileContent ?? "No such logfile exists"), "text/plain");
        }
    }

    public class PcrBatchLogContentRequest
    {
        [Required]
        public string LogCorrelationId { get; set; }
        [Required]
        public string Filename { get; set; }
    }

    public class PositiveCreditRegisterExportResponse : ScheduledJobResult
    {
        public int SuccessCount { get; set; }
    }

    public class FetchGetLoanRequest
    {
        [Required]
        public string CreditNr { get; set; }
    }

    public class FetchGetLoanResponse
    {
        public bool IsSuccess { get; set; }
        public string RawResponse { get; set; }
    }

    public class ManualPositiveCreditRegisterExportRequest
    {
        [Required]
        public DateTime FromDate { get; set; }
        [Required]
        public DateTime ToDate { get; set; }
        public bool IsFirstTimeExport { get; set; }
    }

    public class ManualPositiveCreditRegisterExportResponse
    {
        public int SuccessCount { get; set; }
        public string RawResponse { get; set; }
    }

    public class GetLogsRequest
    {
        [Required]
        public int NumberOfLogs { get; set; }
    }

    public class GetLogsResponse
    {
        public List<string> StatusLogs { get; set; }
        public List<string> ExportLogs { get; set; }
    }
}

