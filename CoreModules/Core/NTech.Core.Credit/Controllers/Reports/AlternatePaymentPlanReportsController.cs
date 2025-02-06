using Microsoft.AspNetCore.Mvc;
using nCredit.Excel;
using NTech.Core.Credit.Shared.Services;

namespace NTech.Core.Credit.Controllers.Reports
{
    [ApiController]
    [NTechRequireFeatures(RequireFeaturesAll = new[] { "ntech.feature.paymentplan" })]
    public class AlternatePaymentPlanReportsController : Controller
    {
        private readonly AlternatePaymentPlanReportsService alternatePaymentPlanReportsService;

        public AlternatePaymentPlanReportsController(AlternatePaymentPlanReportsService alternatePaymentPlanReportsService)
        {
            this.alternatePaymentPlanReportsService = alternatePaymentPlanReportsService;
        }

        [HttpGet]
        [Route("Api/Credit/Reports/AlternatePaymentPlans")]
        public async Task<FileResult> AlternatePaymentPlansReport([FromQuery] AlternatePaymentPlansReportRequest request)
        {
            var reportResult = await alternatePaymentPlanReportsService.CreateAlternatePaymentPlansExcelReportAsync(request);
            return File(reportResult.ReportData, DocumentClientExcelRequest.XlsxContentType, reportResult.ReportFileName);
        }
    }
}
