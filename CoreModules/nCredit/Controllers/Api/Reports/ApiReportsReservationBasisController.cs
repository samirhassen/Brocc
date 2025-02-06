using nCredit.Code;
using nCredit.Excel;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechAuthorizeCreditHigh]
    [NTechApi]
    public class ApiReportsReservationBasisController : NController
    {
        private static IEnumerable<IEnumerable<T>> SplitIntoGroupsOfN<T>(T[] array, int n)
        {
            for (var i = 0; i < (float)array.Length / n; i++)
            {
                yield return array.Skip(i * n).Take(n);
            }
        }

        [Route("Api/Reports/ReservationBasis")]
        [HttpGet]
        public ActionResult Get(DateTime date)
        {
            if (!NEnv.IsUnsecuredLoansEnabled)
                return HttpNotFound();

            try
            {
                using (var context = new CreditContext())
                {
                    var credits = context
                        .CreditHeaders
                        .Where(x => x.CreatedByEvent.TransactionDate <= date)
                        .Select(x => new
                        {
                            x.CreditNr,
                            CreditStatusItem = x
                                .DatedCreditStrings
                                .Where(y => y.Name == DatedCreditStringCode.CreditStatus.ToString() && y.TransactionDate <= date)
                                .OrderByDescending(y => y.TransactionDate)
                                .ThenByDescending(y => y.Timestamp)
                                .FirstOrDefault(),
                            CaptialDebt = x
                                .Transactions
                                .Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString() && y.TransactionDate <= date)
                                .Sum(y => (decimal?)y.Amount) ?? 0m,
                            DebtCollectionDebt = -x
                                .Transactions
                                .Where(y => y.WriteoffId.HasValue && y.BusinessEvent.EventType == BusinessEventType.CreditDebtCollectionExport.ToString() && y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                                .Sum(y => (decimal?)y.Amount) ?? 0m,
                            OldestUnpaidDueDate = x
                                .Notifications
                                .Where(y => y.TransactionDate <= date && (!y.ClosedTransactionDate.HasValue || y.ClosedTransactionDate.Value > date))
                                .Select(y => (DateTime?)y.DueDate)
                                .Min()
                        })
                        .Select(x => new
                        {
                            x.CreditNr,
                            CaptialDebt = x.CreditStatusItem.Value == CreditStatus.SentToDebtCollection.ToString()
                                            ? x.DebtCollectionDebt
                                            : x.CaptialDebt,
                            OverdueDays = !x.OldestUnpaidDueDate.HasValue ? 0 : (date < x.OldestUnpaidDueDate.Value ? 0 : DbFunctions.DiffDays(x.OldestUnpaidDueDate.Value, date) ?? 0),
                            DebtCollectionDate = x.CreditStatusItem.Value == CreditStatus.SentToDebtCollection.ToString()
                                            ? (DateTime?)x.CreditStatusItem.TransactionDate
                                            : null

                        })
                        .Where(x => x.OverdueDays >= 90 || x.DebtCollectionDate.HasValue)
                        .OrderBy(x => x.CreditNr)
                        .ToList();

                    var request = new DocumentClientExcelRequest
                    {
                        Sheets = new DocumentClientExcelRequest.Sheet[]
                        {
                            new DocumentClientExcelRequest.Sheet
                            {
                                AutoSizeColumns = true,
                                Title = $"Reservation basis ({date.ToString("yyyy-MM-dd")})"
                            }
                        }
                    };

                    var s = request.Sheets[0];
                    s.SetColumnsAndData(credits,
                        credits.Col(x => x.CreditNr, ExcelType.Text, "Loan number"),
                        credits.Col(x => x.CaptialDebt, ExcelType.Number, "Capital debt", nrOfDecimals: 2, includeSum: true),
                        credits.Col(x => x.OverdueDays, ExcelType.Number, "Overdue days", nrOfDecimals: 0),
                        credits.Col(x => x.DebtCollectionDate, ExcelType.Date, "Sent to debt collection"));

                    var client = Service.DocumentClientHttpContext;
                    var report = client.CreateXlsx(request);

                    return new FileStreamResult(report, XlsxContentType) { FileDownloadName = $"CreditReservationBasis-{date.ToString("yyyy-MM-dd")}.xlsx" };
                }
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Failed to create credit reservation basis report");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }
    }
}