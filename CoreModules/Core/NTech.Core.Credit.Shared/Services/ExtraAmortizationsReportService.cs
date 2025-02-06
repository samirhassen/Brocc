using nCredit;
using nCredit.Excel;
using NTech.Core.Credit.Shared.Database;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services
{
    public class ExtraAmortizationsReportService
    {
        private readonly CreditContextFactory creditContextFactory;

        public ExtraAmortizationsReportService(CreditContextFactory creditContextFactory)
        {
            this.creditContextFactory = creditContextFactory;
        }

        public DocumentClientExcelRequest CreateReportExcelRequest(ExtraAmortizationsReportRequest request)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var d1 = request.Date1.Value.Date;
                var d2 = request.Date2.Value.Date;

                var extraAmortizations = context
                    .TransactionsQueryable
                    .Where(x =>
                        x.AccountCode == TransactionAccountType.CapitalDebt.ToString()
                        && x.IncomingPaymentId != null
                        && x.CreditNr != null
                        && x.CreditNotificationId == null
                        && x.TransactionDate >= d1
                        && x.TransactionDate <= d2)
                    .GroupBy(x => new { x.CreditNr, x.TransactionDate })
                    .Select(x => new
                    {
                        CreditNr = x.Key.CreditNr,
                        TransactionDate = x.Key.TransactionDate,
                        ExtraAmortizationAmount = -x.Sum(y => y.Amount)
                    })
                    .ToList()
                    .OrderBy(x => x.TransactionDate)
                    .ThenBy(x => x.CreditNr)
                    .ToList();

                var excelRequest = new DocumentClientExcelRequest
                {
                    Sheets = new DocumentClientExcelRequest.Sheet[]
                    {
                        new DocumentClientExcelRequest.Sheet
                        {
                            AutoSizeColumns = true,
                            Title = "Extra amortizations"
                        }
                    }
                };

                excelRequest.Sheets[0].SetColumnsAndData(extraAmortizations,
                    extraAmortizations.Col(x => x.TransactionDate, ExcelType.Date, "Date"),
                    extraAmortizations.Col(x => x.CreditNr, ExcelType.Text, "Credit nr"),
                    extraAmortizations.Col(x => x.ExtraAmortizationAmount, ExcelType.Number, "Extra amortization amount"));

                return excelRequest;
            }
        }
    }

    public class ExtraAmortizationsReportRequest
    {
        [Required]
        public DateTime? Date1 { get; set; }

        [Required]
        public DateTime? Date2 { get; set; }
    }
}
