using nSavings.Code;
using nSavings.DbModel.BusinessEvents;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nSavings.Controllers
{
    [NTechApi]
    [RoutePrefix("Api/SavingsAccountFutureInterestReport")]
    public class ApiSavingsAccountFutureInterestReportController : NController
    {
        [Route("CreateReport")]
        [HttpGet()]
        public ActionResult CreateReport(string savingsAccountNr, string format, DateTime? toDate)
        {
            if (string.IsNullOrWhiteSpace(savingsAccountNr))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "missing savingsAccountNr");
            }
            if (!toDate.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "missing toDate");
            }
            IDictionary<string, YearlyInterestCapitalizationBusinessEventManager.ResultModel> result;
            string failedMessage;
            using (var context = new SavingsContext())
            {
                if (!YearlyInterestCapitalizationBusinessEventManager.TryComputeAccumulatedInterestUntilDate(
                    context,
                    new List<string> { savingsAccountNr },
                    toDate.Value,
                    true,
                    out result,
                    out failedMessage))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
                }
                var r = result.Single().Value;

                if (format == "json")
                {
                    return Json2(r);
                }
                else if (format == "excel")
                {
                    var er = r.ToDocumentClientExcelRequest();

                    var dc = new DocumentClient();
                    var excelStream = dc.CreateXlsx(er);
                    var f = new FileStreamResult(excelStream, XlsxContentType);
                    f.FileDownloadName = $"FutureInterestReport_{savingsAccountNr}_{toDate.Value.ToString("yyyyMMdd")}.xlsx";
                    return f;
                }
                else
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "format must be excel or json");
                }
            }

        }
    }
}