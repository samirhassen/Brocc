using Microsoft.AspNetCore.Mvc;
using nCredit.Excel;
using NTech.Core.Credit.Shared.Repository;
using NTech.Core.Module.Shared.Clients;

namespace NTech.Core.Credit.Controllers
{
    [ApiController]    
    public class CustomerCreditHistoryController : Controller
    {
        private readonly CustomerCreditHistoryCoreRepository repository;
        private readonly IDocumentClient documentClient;

        public CustomerCreditHistoryController(CustomerCreditHistoryCoreRepository repository, IDocumentClient documentClient)
        {
            this.repository = repository;
            this.documentClient = documentClient;
        }

        [HttpGet]
        [Route("Api/Credit/CustomerCreditHistory/SingleCredit-Excel-Preview")]
        public async Task<FileStreamResult> SingleCreditExcelPreview([FromQuery] string creditNr)
        {
            var credits = repository.GetCustomerCreditHistory(null, new List<string> { creditNr });
            var excelRequest = DocumentClientExcelRequest.CreateSimpleRequest(credits, $"History {creditNr}");
            var report = await documentClient.CreateXlsxAsync(excelRequest);
            return File(report, DocumentClientExcelRequest.XlsxContentType);
        }
    }
}
