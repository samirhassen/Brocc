using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using nSavings.Code;
using nSavings.DbModel;
using nSavings.DbModel.BusinessEvents;
using NTech.Services.Infrastructure;

namespace nSavings.Controllers.Api
{
    [NTechApi]
    [RoutePrefix("Api/SavingsAccountFutureInterestReport")]
    public class ApiSavingsAccountFutureInterestReportController : NController
    {
        [HttpGet, Route("CreateReport")]
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

            using (var context = new SavingsContext())
            {
                if (!YearlyInterestCapitalizationBusinessEventManager.TryComputeAccumulatedInterestUntilDate(
                        context,
                        new List<string> { savingsAccountNr },
                        toDate.Value,
                        true,
                        out var result,
                        out var failedMessage))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
                }

                var r = result.Single().Value;

                switch (format)
                {
                    case "json":
                        return Json2(r);
                    case "excel":
                    {
                        var er = r.ToDocumentClientExcelRequest();

                        var dc = new DocumentClient();
                        var excelStream = dc.CreateXlsx(er);
                        var f = new FileStreamResult(excelStream, XlsxContentType)
                        {
                            FileDownloadName = $"FutureInterestReport_{savingsAccountNr}_{toDate.Value:yyyyMMdd}.xlsx"
                        };
                        return f;
                    }
                    default:
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "format must be excel or json");
                }
            }
        }
    }
}