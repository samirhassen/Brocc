using nPreCredit.Code;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    [AllowAnonymous]
    public class CreditManagementMonitorController : NController
    {
        private CreditManagementMonitorRepository CreateRepository()
        {
            return new CreditManagementMonitorRepository(this.Clock);
        }

        [Route("CreditManagementMonitor")]
        public ActionResult Index()
        {
            if (NEnv.IsMortgageLoansEnabled)
                return HttpNotFound();

            if (NEnv.IsCreditManagementMonitorDisabled)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Disabled");

            var repo = CreateRepository();

            SetInitialData(new
            {
                refreshUrl = Url.Action("Refresh", "CreditManagementMonitor"),
                providers = repo.GetProviders(),
                rejectionReasonToDisplayNameMapping = NEnv.ScoringSetup.GetRejectionReasonToDisplayNameMapping()
            });
            return View();
        }

        [Route("Api/CreditManagementMonitor/Refresh")]
        [HttpPost()]
        public ActionResult Refresh(string providerName, string timeSpan, bool? includeDetails, int? nrOfAutoRejectionReasonsToShow, int? nrOfManualRejectionReasonsToShow, int? nrOfProviderItemsToShow)
        {
            if (NEnv.IsMortgageLoansEnabled)
                return HttpNotFound();

            if (NEnv.IsCreditManagementMonitorDisabled)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Disabled");

            var repo = CreateRepository();

            CreditManagementMonitorRepository.MonitorDataSet result;
            string errorMessage;

            if (!repo.TryGetMonitorData(providerName, timeSpan, includeDetails, nrOfAutoRejectionReasonsToShow, nrOfManualRejectionReasonsToShow, nrOfProviderItemsToShow, out result, out errorMessage))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, errorMessage);
            }

            return Json2(new { result = result });
        }

        [Route("Api/CreditManagementMonitor/DetailsReport")]
        [HttpGet()]
        public ActionResult DetailsReport(string providerName, string timeSpan)
        {
            if (NEnv.IsMortgageLoansEnabled)
                return HttpNotFound();

            if (NEnv.IsCreditManagementMonitorDisabled)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Disabled");

            if (!(timeSpan ?? "").IsOneOf("today", "yesterday"))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid or missing timeSpan. Must be one of today|yesterday");

            var repo = CreateRepository();

            List<CreditManagementMonitorRepository.MonitorApplicationModel> result;
            string errorMessage;

            if (!repo.TryGetMonitorDataDetails(providerName, timeSpan, out errorMessage, out result))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, errorMessage);

            var providerNameClearText = providerName?.Replace("*", "all");
            var request = new DocumentClientExcelRequest
            {
                Sheets = new DocumentClientExcelRequest.Sheet[]
                {
                    new DocumentClientExcelRequest.Sheet
                    {
                        AutoSizeColumns = true,
                        Title = $"Details {providerNameClearText} {timeSpan}".Trim()
                    }
                }
            };

            result = result.OrderByDescending(x => x.ApplicationDate).ToList();

            var allRejectionReasons = new HashSet<string>();
            foreach (var a in result)
                if (a?.RejectionReasons != null)
                    foreach (var r in (a.RejectionReasons))
                        allRejectionReasons.Add(r);

            var cols = DocumentClientExcelRequest.CreateDynamicColumnList(result);
            cols.Add(result.Col(x => x.ApplicationDate.DateTime, ExcelType.Date, "Date", includeTime: true));
            cols.Add(result.Col(x => x.ApplicationNr, ExcelType.Text, "ApplicationNr"));
            cols.Add(result.Col(x => x.CategoryCode, ExcelType.Text, "Category"));
            cols.Add(result.Col(x => x.ProviderName, ExcelType.Text, "Provider"));
            foreach (var r in allRejectionReasons)
                cols.Add(result.Col(x => (x.RejectionReasons?.Contains(r) ?? false) ? 1 : 0, ExcelType.Number, r, nrOfDecimals: 0, includeSum: true));

            var s = request.Sheets[0];
            s.SetColumnsAndData(result, cols.ToArray());

            var client = new nDocumentClient();
            var report = client.CreateXlsx(request);

            return new FileStreamResult(report, XlsxContentType) { FileDownloadName = $"ApplicationDetails{providerNameClearText}{timeSpan}".Trim() + ".xlsx" };
        }
    }
}