using nCredit.Code;
using nCredit.DomainModel;
using nCredit.Excel;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace nCredit.WebserviceMethods.Reports
{
    public class BookKeepingLedgerReportMethod : FileStreamWebserviceMethod<BookKeepingLedgerReportMethod.Request>
    {
        public override string Path => "Reports/GetBookkeepingLoanLedger";

        public override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled && !NEnv.IsStandardUnsecuredLoansEnabled;

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, x =>
            {
                x.Require(r => r.Date);
            });

            var nSettings = NEnv.NotificationProcessSettings.GetByCreditType(CreditType.UnsecuredLoan);

            var c = new DataWarehouseClient();
            var p = new ExpandoObject();
            p.SetValues(d => d["forDate"] = request.Date.Value);

            var supportItems = c.FetchReportData<DwPartialDataModel>("BookKeepingLedgeReportPartialData1", p)?.ToDictionary(x => x.CreditNr);

            using (var context = new CreditContext())
            {
                var d = request.Date.Value;
                var creditsBasis = context
                    .CreditHeaders.Where(x => x.CreatedByEvent.TransactionDate <= d);

                if (request.CreditNr != null)
                {
                    creditsBasis = creditsBasis.Where(x => x.CreditNr == request.CreditNr);
                }
                /*
                 The idea behind these two (BookKeepingCapitalDebtExceptNewLoans, BookKeepingOutgoingPaymentFileAmount):
                 - The economy guys want to pretend that the capital debt appears when the money leaves the bank rather than when the loan is created.
                 - We attempt to handle this by using the outgoing payment transactions to, in a sense, simluate moving the payment forward in time to the outgoing payment transaction
                 */
                var newLoanAmountEventTypes = new List<string> { BusinessEventType.NewCredit.ToString(), BusinessEventType.NewAdditionalLoan.ToString() };
                var credits = creditsBasis
                    .Select(x => new
                    {
                        x.CreditNr,
                        x.CreatedByBusinessEventId,
                        MarginInterestRate = x
                            .DatedCreditValues
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditValueCode.MarginInterestRate.ToString())
                            .OrderByDescending(y => y.TransactionDate)
                            .ThenByDescending(y => y.Timestamp)
                            .Select(y => (decimal?)y.Value)
                            .FirstOrDefault(),
                        AnnuityAmount = x
                            .DatedCreditValues
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditValueCode.AnnuityAmount.ToString())
                            .OrderByDescending(y => y.TransactionDate)
                            .ThenByDescending(y => y.Timestamp)
                            .Select(y => (decimal?)y.Value)
                            .FirstOrDefault(),
                        ReferenceInterestRate = x
                            .DatedCreditValues
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditValueCode.ReferenceInterestRate.ToString())
                            .OrderByDescending(y => y.TransactionDate)
                            .ThenByDescending(y => y.Timestamp)
                            .Select(y => (decimal?)y.Value)
                            .FirstOrDefault(),
                        Status = x
                            .DatedCreditStrings
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditStringCode.CreditStatus.ToString())
                            .OrderByDescending(y => y.TransactionDate)
                            .ThenByDescending(y => y.Timestamp)
                            .Select(y => y.Value)
                            .FirstOrDefault(),
                        TransactionCapitalDebt = x.Transactions.Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString() && y.TransactionDate <= d).Sum(y => (decimal?)y.Amount) ?? 0m,
                        TransactionNotNotifiedCapitalDebt = x.Transactions.Where(y => y.AccountCode == TransactionAccountType.NotNotifiedCapital.ToString() && y.TransactionDate <= d).Sum(y => (decimal?)y.Amount) ?? 0m,
                        BookKeepingCapitalDebtExceptNewLoans = (x
                            .Transactions
                            .Where(y => !newLoanAmountEventTypes.Contains(y.BusinessEvent.EventType) && y.AccountCode == TransactionAccountType.CapitalDebt.ToString() && y.BookKeepingDate <= d)
                            .Sum(y => (decimal?)y.Amount) ?? 0m),
                        BookKeepingOutgoingPaymentFileAmount = -(x
                            .Transactions
                            .Where(y => y.BusinessEvent.EventType == BusinessEventType.NewOutgoingPaymentFile.ToString() && y.AccountCode == TransactionAccountType.ShouldBePaidToCustomer.ToString() && y.BookKeepingDate <= d)
                            .Sum(y => (decimal?)y.Amount) ?? 0m),
                        InitialPaymentTransactionDate = x
                            .Transactions
                            .Where(y => y.AccountCode == TransactionAccountType.ShouldBePaidToCustomer.ToString() && y.Amount < 0 && y.BusinessEvent.EventType == BusinessEventType.NewOutgoingPaymentFile.ToString())
                            .OrderBy(y => y.Id)
                            .Select(y => (DateTime?)y.TransactionDate)
                            .FirstOrDefault(),
                    })
                    .OrderBy(x => x.CreatedByBusinessEventId)
                    .Select(x => new
                    {
                        x.CreditNr,
                        x.MarginInterestRate,
                        x.ReferenceInterestRate,
                        x.Status,
                        x.TransactionCapitalDebt,
                        x.TransactionNotNotifiedCapitalDebt,
                        x.AnnuityAmount,
                        BookKeepingCapitalDebt = x.BookKeepingCapitalDebtExceptNewLoans + x.BookKeepingOutgoingPaymentFileAmount,
                        x.InitialPaymentTransactionDate
                    })
                    .ToList()
                    .Select(x =>
                    {
                        var m = supportItems?.Opt(x.CreditNr);
                        int? remaningRuntimeInMonths = null;
                        int rm; string _;

                        //TODO: Use credittype of each credit here to get separate nSettings
                        if (x.TransactionCapitalDebt > 0m && x.AnnuityAmount.HasValue && Controllers.ApiCreditDetailsController.TryGetNrOfRemainingPayments(
                            request.Date.Value,
                            nSettings,
                            NTech.Banking.LoanModel.CreditAmortizationModel.CreateAnnuity(x.AnnuityAmount.Value, null),
                            x.TransactionNotNotifiedCapitalDebt, x.MarginInterestRate.GetValueOrDefault() + x.ReferenceInterestRate.GetValueOrDefault(), 0m, null,
                            out rm, out _))
                        {
                            remaningRuntimeInMonths = rm;
                        }

                        return new
                        {
                            x.CreditNr,
                            x.MarginInterestRate,
                            x.ReferenceInterestRate,
                            x.Status,
                            BookKeepingCapitalDebt = x.BookKeepingCapitalDebt,
                            TransactionCapitalDebt = x.TransactionCapitalDebt,
                            InitialPaymentFileDate = x.InitialPaymentTransactionDate,
                            InitialPaymentFileAmount = m?.InitialNewCreditCapitalAmount,
                            NrOfDaysOverdue = m?.NrOfDaysOverdue,
                            RemaningRuntimeInMonths = remaningRuntimeInMonths
                        };
                    })
                    .ToList();

                Dictionary<string, LoanPerformanceReportMethod.LoanPerformanceCredit> loanPerformanceRowByCreditNr = null;
                if (!request.ExcludeLoanPerformanceData.GetValueOrDefault())
                {
                    var resolver = requestContext.Service();
                    if (!LoanPerformanceReportMethod.TryGetLoanPerformanceRows(new LoanPerformanceReportMethod.Request
                    {
                        Date = request.Date,
                        CreditNr = request.CreditNr,
                        IncludeActualOverdue = true,
                        IncludeNotNotifiedInterestDetails = false
                    }, resolver.ContextFactory, resolver.CalendarDateService, out var loanPerformanceData, out var failedMessage))
                        return Error(failedMessage);

                    loanPerformanceRowByCreditNr = loanPerformanceData.Credits.ToDictionary(x => x.CreditNr);
                }

                var sheets = new List<DocumentClientExcelRequest.Sheet>();

                sheets.Add(new DocumentClientExcelRequest.Sheet
                {
                    AutoSizeColumns = true,
                    Title = $"Loan ledger ({request.Date.Value.ToString("yyyy-MM-dd")})"
                });

                var excelRequest = new DocumentClientExcelRequest
                {
                    Sheets = sheets.ToArray()
                };

                var currency = NEnv.ClientCfg.Country.BaseCurrency;

                Func<int?, DateTime?> simluateEndDate = remaningRuntimeInMonths =>
                {
                    if (!remaningRuntimeInMonths.HasValue)
                        return null;

                    return new DateTime(d.Year, d.Month, nSettings.NotificationDueDay).AddMonths(remaningRuntimeInMonths.Value);
                };

                var s = excelRequest.Sheets[0];

                var cols = DocumentClientExcelRequest.CreateDynamicColumnList(credits);

                cols.Add(credits.Col(x => x.CreditNr, ExcelType.Text, "Credit nr"));
                cols.Add(credits.Col(x => x.InitialPaymentFileDate, ExcelType.Date, "Initial payment date"));
                cols.Add(credits.Col(x => x.InitialPaymentFileAmount, ExcelType.Number, "Initial payment amount"));
                cols.Add(credits.Col(x => x.MarginInterestRate.HasValue && x.ReferenceInterestRate.HasValue ? new decimal?((x.MarginInterestRate.Value + x.ReferenceInterestRate.Value) / 100m) : null, ExcelType.Number, "Interest rate", nrOfDecimals: 4));
                cols.Add(credits.Col(x => x.BookKeepingCapitalDebt, ExcelType.Number, "Capital balance"));

                if (request.IncludeTransactionDateBalance.GetValueOrDefault())
                    cols.Add(credits.Col(x => x.TransactionCapitalDebt, ExcelType.Number, "TR Capital balance"));

                cols.Add(credits.Col(x => currency, ExcelType.Text, "Currency"));
                cols.Add(credits.Col(x => x.Status, ExcelType.Text, "Status"));
                cols.Add(credits.Col(x => x.NrOfDaysOverdue, ExcelType.Number, "Actual days past due date", nrOfDecimals: 0));
                cols.Add(credits.Col(x => simluateEndDate(x.RemaningRuntimeInMonths), ExcelType.Date, "Expected end date"));
                if (!request.ExcludeLoanPerformanceData.GetValueOrDefault())
                {
                    cols.Add(credits.Col(x => loanPerformanceRowByCreditNr.Opt(x.CreditNr)?.ReservationNrOfDaysOverdue, ExcelType.Number, "Days past due date", nrOfDecimals: 0));
                    cols.Add(credits.Col(x => loanPerformanceRowByCreditNr.Opt(x.CreditNr)?.ReservationOverDueCount ?? 0, ExcelType.Number, "Overdue count", nrOfDecimals: 0));
                    cols.Add(credits.Col(x => loanPerformanceRowByCreditNr.Opt(x.CreditNr)?.NrOfOverdueCount, ExcelType.Number, "Actual overdue count", nrOfDecimals: 0));
                    cols.Add(credits.Col(x => loanPerformanceRowByCreditNr.Opt(x.CreditNr)?.TotalAdditionalLoanPaymentAmount, ExcelType.Number, "Add. loan payment amount"));
                    cols.Add(credits.Col(x => loanPerformanceRowByCreditNr.Opt(x.CreditNr)?.NotifiedInterestDebt ?? 0m, ExcelType.Number, "Notified interest balance", includeSum: true));
                    cols.Add(credits.Col(x => loanPerformanceRowByCreditNr.Opt(x.CreditNr)?.Status == CreditStatus.Normal.ToString() ? new DateTime?() : loanPerformanceRowByCreditNr.Opt(x.CreditNr)?.StatusDate, ExcelType.Date, "LP - Closed date"));
                    cols.Add(credits.Col(x => ProviderDisplayNames.GetProviderDisplayName(loanPerformanceRowByCreditNr.Opt(x.CreditNr)?.ProviderName), ExcelType.Text, "Provider"));
                }

                s.SetColumnsAndData(credits, cols.ToArray());

                var client = requestContext.Service().DocumentClientHttpContext;
                var result = client.CreateXlsx(excelRequest);

                return this.ExcelFile(result, $"BookkeepingLoanLedger-{request.Date.Value.ToString("yyyy-MM-dd")}.xlsx");
            }
        }

        public class Request
        {
            public DateTime? Date { get; set; }
            public string CreditNr { get; set; }
            public bool? IncludeTransactionDateBalance { get; set; }
            public bool? ExcludeLoanPerformanceData { get; set; }
        }

        private class DwPartialDataModel
        {
            public string CreditNr { get; set; }
            public DateTime? NewCreditTransactionDate { get; set; }
            public decimal InitialNewCreditCapitalAmount { get; set; }
            public int? NrOfDaysOverdue { get; set; }
        }
    }
}