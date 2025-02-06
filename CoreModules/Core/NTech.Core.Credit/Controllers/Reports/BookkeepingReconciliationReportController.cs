using Microsoft.AspNetCore.Mvc;
using nCredit;
using nCredit.Excel;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Infrastructure;

namespace NTech.Core.Credit.Controllers.Reports
{
    [ApiController]
    public class BookkeepingReconciliationReportController : Controller
    {
        private readonly BookkeepingReconciliationReportService reconciliationReportService;
        private readonly ICreditEnvSettings envSettings;

        public BookkeepingReconciliationReportController(BookkeepingReconciliationReportService reconciliationReportService, ICreditEnvSettings envSettings)
        {
            this.reconciliationReportService = reconciliationReportService;
            this.envSettings = envSettings;
        }

        [HttpGet]
        [Route("Api/Credit/Reports/BookkeepingReconciliation")]
        public async Task<FileResult> BookKeepingReconciliationReport([FromQuery]BookKeepingReconciliationReportRequest request)
        {
            if (!BookkeepingReconciliationReportService.IsReportEnabled(envSettings))
                throw new NTechCoreWebserviceException("Report disabled. Setup a custom format file to enable it") { ErrorCode = "reportDisabled", ErrorHttpStatusCode = 400, IsUserFacing = true };

            var reportResult = await reconciliationReportService.CreateExcelReportAsync(request);
            return File(reportResult.ReportData, DocumentClientExcelRequest.XlsxContentType, reportResult.ReportFileName);
        }
    }
}
