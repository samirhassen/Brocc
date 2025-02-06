using nCredit.Code;
using nCredit.Excel;
using NTech.Core.Credit.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;

namespace nCredit.WebserviceMethods.Reports
{
    public class MortgageLoanQuarterlyBKIReportMethod : ReportWebserviceMethod<MortgageLoanQuarterlyBKIReportMethod.Request>
    {
        public override string ReportName => "MortgageLoanQuarterlyBKI";
        public override bool IsEnabled => NEnv.IsMortgageLoansEnabled && NEnv.IsMortgageLoanBKIClient;

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, x =>
            {
                x.Require(y => y.QuarterEndDate);
            });

            var q = Quarter.ContainingDate(request.QuarterEndDate.Value);

            var service = new MortgageLoanBkiF820ReportService(CoreClock.SharedInstance, requestContext.Service().ContextFactory);
            var reportData = service.CreateReportData(q);

            var sheets = new List<DocumentClientExcelRequest.Sheet>(3);

            var sumSheet = new DocumentClientExcelRequest.Sheet
            {
                AutoSizeColumns = true,
                Title = $"F820 Kvartalsrapport BKI"
            };
            var sumsAndCounts = new Tuple<string, decimal>[]
            {
                Tuple.Create("Current total capital debt", reportData.CapitalBalance),
                Tuple.Create("Paid out amount this quarter", reportData.PaidOutAmountInQuarter),
                Tuple.Create("Nr of active credits", (decimal)reportData.NrOfCredits),
                Tuple.Create("Nr of new credits this quarter", (decimal)reportData.NrOfNewCreditsInQuarter),
                Tuple.Create("Nr of customers ", (decimal)reportData.NrOfCustomers),
                Tuple.Create("Interest revenue this quarter", reportData.InterestRevenue),
                Tuple.Create("Fee revenue this quarter", reportData.FeeRevenue),
                Tuple.Create("Nr of impared credits", (decimal)reportData.NrOfImpairedCredits),
                Tuple.Create("Nr of new impared credits this quarter", (decimal)reportData.NrOfNewImpairedCreditsThisQuarter),
            };
            sumSheet.SetColumnsAndData(sumsAndCounts,
                sumsAndCounts.Col(x => x.Item1, ExcelType.Text, null),
                sumsAndCounts.Col(x => x.Item2, ExcelType.Number, null, includeSum: false));
            sheets.Add(sumSheet);

            var dataSheet = new DocumentClientExcelRequest.Sheet
            {
                AutoSizeColumns = true,
                Title = $"Data ({q.Name})"
            };
            dataSheet.SetColumnsAndData(reportData.Loans,
                reportData.Loans.Col(x => x.CreditNr, ExcelType.Text, "Credit nr"),
                reportData.Loans.Col(x => x.StartDate, ExcelType.Date, "Start date"),
                reportData.Loans.Col(x => x.NrOfCustomers, ExcelType.Number, "Nr of customers", includeSum: true, nrOfDecimals: 0, sumNrOfDecimals: 0),
                reportData.Loans.Col(x => x.CapitalBalance, ExcelType.Number, "Capital balance", includeSum: true),
                reportData.Loans.Col(x => x.InterestRevenue, ExcelType.Number, "Interest revenue", includeSum: true),
                reportData.Loans.Col(x => x.FeeRevenue, ExcelType.Number, "Fee revenue", includeSum: true),
                reportData.Loans.Col(x => x.FinalStatus, ExcelType.Text, "Final Status"),
                reportData.Loans.Col(x => x.NrOfOverdueDays, ExcelType.Number, "Overdue days", nrOfDecimals: 0),
                reportData.Loans.Col(x => x.InitialPaidOutAmount, ExcelType.Number, "Paid out amount", includeSum: true),
                reportData.Loans.Col(x => x.IsImpaired ? 1 : 0, ExcelType.Number, "Impaired", nrOfDecimals: 0, includeSum: true),
                reportData.Loans.Col(x => x.WasImpairedDuringLastQuarter ? 1 : 0, ExcelType.Number, "Impaired last quarter", nrOfDecimals: 0, includeSum: true),
                reportData.Loans.Col(x => x.IsNewInQuarter ? 1 : 0, ExcelType.Number, "New", nrOfDecimals: 0, includeSum: true));
            sheets.Add(dataSheet);

            var activeCustomersSheet = new DocumentClientExcelRequest.Sheet
            {
                AutoSizeColumns = true,
                Title = $"Active customers"
            };
            activeCustomersSheet.SetColumnsAndData(reportData.ActiveCreditCustomers,
                reportData.ActiveCreditCustomers.Col(x => x.CreditNr, ExcelType.Text, "Credit nr"),
                reportData.ActiveCreditCustomers.Col(x => x.CustomerId.ToString(), ExcelType.Text, "Customer id"));
            sheets.Add(activeCustomersSheet);

            var excelRequest = new DocumentClientExcelRequest
            {
                Sheets = sheets.ToArray()
            };

            var client = requestContext.Service().DocumentClientHttpContext;
            var result = client.CreateXlsx(excelRequest);

            return ExcelFile(result, downloadFileName: $"QuarterlyBKI-{q.Name}.xlsx");
        }

        public class Request
        {
            public DateTime? QuarterEndDate { get; set; }
        }
    }
}