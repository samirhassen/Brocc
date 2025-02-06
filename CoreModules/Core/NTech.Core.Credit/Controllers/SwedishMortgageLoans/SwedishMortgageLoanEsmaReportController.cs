using Microsoft.AspNetCore.Mvc;
using nCredit.Excel;
using NTech.Core.Credit.Shared.Models;
using NTech.Core.Credit.Shared.Services.SwedishMortgageLoans;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Services;

namespace NTech.Core.Credit.Controllers.SwedishMortgageLoans
{
    [ApiController]
    [NTechRequireFeatures(RequireClientCountryAny = new[] { "SE" }, RequireFeaturesAll = new[] { "ntech.feature.mortgageloans.standard" })]
    public class SwedishMortgageLoanEsmaReportController: Controller    
    {
        private readonly AnnexTwoEsmaReportService annexTwoService;
        private readonly IDocumentClient documentClient;
        private readonly AnnexTwelveEsmaReportService annexTwelveService;
        private readonly FundOwnerReportService fundOwnerReportService;

        public SwedishMortgageLoanEsmaReportController(AnnexTwoEsmaReportService annexTwoService, IDocumentClient documentClient, AnnexTwelveEsmaReportService annexTwelveService,
            FundOwnerReportService fundOwnerReportService)
        {
            this.annexTwoService = annexTwoService;
            this.documentClient = documentClient;
            this.annexTwelveService = annexTwelveService;
            this.fundOwnerReportService = fundOwnerReportService;
        }

        /// <summary>
        /// Report data for esma annex 2 reporting
        /// </summary>
        [HttpPost]
        [Route("Api/Credit/Report/MortageLoan/AnnexTwo-Data")]
        public EsmaAnnexTwoReportResponse AnnexTwoData(FromDateToDateReportRequest request)
        {
            return annexTwoService.GetAnnexTwoReportData(request);
        }

        /// <summary>
        /// Report data for esma annex 2 reporting in excel
        /// </summary>
        [HttpGet]
        [Route("Api/Credit/Report/MortageLoan/AnnexTwo-Excel")]
        public async Task<FileStreamResult> AnnexTwoExcel([FromQuery]FromDateToDateReportRequest request)
        {
            var response = annexTwoService.GetAnnexTwoReportData(request);
            
            var excelRequest = DocumentClientExcelRequest.CreateSimpleRequest(response.Loans, "Annex2");
            var report = await documentClient.CreateXlsxAsync(excelRequest);
            return File(report, DocumentClientExcelRequest.XlsxContentType);
        }

        /// <summary>
        /// Report data for esma annex 12 reporting
        /// </summary>
        [HttpPost]
        [Route("Api/Credit/Report/MortageLoan/AnnexTwelve-Data")]
        public EsmaAnnexTwelveReportResponse AnnexTwelveData(FromDateToDateReportRequest request)
        {
            return annexTwelveService.GetAnnexTwelveReportData(request);
        }

        /// <summary>
        /// Report data for esma annex 12 reporting in excel
        /// </summary>
        [HttpGet]
        [Route("Api/Credit/Report/MortageLoan/AnnexTwelve-Excel")]
        public async Task<FileStreamResult> AnnexTwelveExcel([FromQuery] FromDateToDateReportRequest request)
        {
            var response = annexTwelveService.GetAnnexTwelveReportData(request);

            var excelRequest = DocumentClientExcelRequest.CreateSimpleRequest(response.Loans, "Annex12");
            var report = await documentClient.CreateXlsxAsync(excelRequest);
            return File(report, DocumentClientExcelRequest.XlsxContentType);
        }

        /// <summary>
        /// Report data for the fund owner report
        /// </summary>
        [HttpPost]
        [Route("Api/Credit/Report/MortageLoan/FundOwner-Data")]
        public FundOwnerReportResponse FundOwnerData(FromDateToDateReportRequest request)
        {
            return fundOwnerReportService.GetFundOwnerReportData(request);
        }

        /// <summary>
        /// Report data for the fund owner report as excel
        /// </summary>
        [HttpGet]
        [Route("Api/Credit/Report/MortageLoan/FundOwner-Excel")]
        public async Task<FileStreamResult> FundOwnerExcel([FromQuery] FromDateToDateReportRequest request)
        {
            var response = fundOwnerReportService.GetFundOwnerReportData(request);

            var excelRequest = DocumentClientExcelRequest.CreateSimpleRequest(response.Loans, "FundOwner");
            var report = await documentClient.CreateXlsxAsync(excelRequest);
            return File(report, DocumentClientExcelRequest.XlsxContentType);
        }
    }
}
