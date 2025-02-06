using nCredit.Code;
using nCredit.DbModel;
using nCredit.Excel;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace nCredit.WebserviceMethods.Reports
{
    public class LoanPerformanceReportMethod : FileStreamWebserviceMethod<LoanPerformanceReportMethod.Request>
    {
        public override string Path => "Reports/GetLoanPerformance";

        public override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled && !NEnv.IsStandardUnsecuredLoansEnabled;

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, x =>
            {
                x.Require(y => y.Date);
            });

            var resolver = requestContext.Service();
            if (!TryGetLoanPerformanceRows(request, resolver.ContextFactory, resolver.CalendarDateService, out var performanceData, out var failedMessage))
                return Error(failedMessage);

            var credits = performanceData.Credits;
            var interestDetails = performanceData.InterestDetails;
            var notNotifiedInterestByCreditNr = performanceData.NotNotifiedInterestByCreditNr;

            var sheets = new List<DocumentClientExcelRequest.Sheet>();

            sheets.Add(new DocumentClientExcelRequest.Sheet
            {
                AutoSizeColumns = true,
                Title = $"Loan performance ({request.Date.Value.ToString("yyyy-MM-dd")})"
            });

            if (interestDetails != null)
            {
                sheets.Add(new DocumentClientExcelRequest.Sheet
                {
                    AutoSizeColumns = true,
                    Title = $"Interest details"
                });
            }

            var excelRequest = new DocumentClientExcelRequest
            {
                Sheets = sheets.ToArray()
            };

            Func<string, string, decimal> getNotNotifiedInterestAmount = (cnr, status) =>
            {
                if (!notNotifiedInterestByCreditNr.ContainsKey(cnr))
                    return 0m;

                if (status != "Normal")
                    return 0m;

                return notNotifiedInterestByCreditNr[cnr];
            };

            Func<decimal?, string, string, decimal> getTotalInterestBalance = (notifiedInterestDebt, cnr, creditStatus) =>
                (notifiedInterestDebt ?? 0m) + getNotNotifiedInterestAmount(cnr, creditStatus);

            var s = excelRequest.Sheets[0];
            s.SetColumnsAndData(credits,
                credits.Col(x => x.CreditNr, ExcelType.Text, "Credit nr"),
                credits.Col(x => x.CreationDate, ExcelType.Date, "Creation date"),
                credits.Col(x => x.InitialPaymentFileDate, ExcelType.Date, "Initial payment date"),
                credits.Col(x => x.InitialPaymentFileAmount, ExcelType.Number, "Initial payment amount"),
                credits.Col(x => x.TotalAdditionalLoanPaymentAmount, ExcelType.Number, "Add. loan payment amount"),
                credits.Col(x => x.MarginInterestRate.HasValue && x.ReferenceInterestRate.HasValue ? new decimal?((x.MarginInterestRate.Value + x.ReferenceInterestRate.Value) / 100m) : null, ExcelType.Percent, "Interest rate"),
                credits.Col(x => x.InitialEffectiveInterestRatePercent / 100m, ExcelType.Percent, "Eff. Interest rate"),
                credits.Col(x => x.CapitalDebt, ExcelType.Number, "Capital balance"),
                credits.Col(x => x.NotifiedInterestDebt ?? 0m, ExcelType.Number, "Notified interest balance", includeSum: true),
                NEnv.IsUnsecuredLoansEnabled ? credits.Col(x => (x.NrOfDaysOverdue < 90 ? 1m : 0.5m) * getTotalInterestBalance(x.NotifiedInterestDebt, x.CreditNr, x.Status), ExcelType.Number, "Interest revenue", includeSum: true) : null,
                credits.Col(x => ProviderDisplayNames.GetProviderDisplayName(x.ProviderName), ExcelType.Text, "Provider"),
                credits.Col(x => x.Status, ExcelType.Text, "Status"),
                credits.Col(x => x.Status == CreditStatus.Normal.ToString() ? new DateTime?() : x.StatusDate, ExcelType.Date, "Closed date"),
                credits.Col(x => x.ReservationOverDueCount ?? 0, ExcelType.Number, "Overdue count", nrOfDecimals: 0),
                credits.Col(x => x.ReservationNrOfDaysOverdue, ExcelType.Number, "Days past due date", nrOfDecimals: 0),
                request.IncludeActualOverdue.GetValueOrDefault() ? credits.Col(x => x.NrOfOverdueCount, ExcelType.Number, "Actual overdue count", nrOfDecimals: 0) : null,
                request.IncludeActualOverdue.GetValueOrDefault() ? credits.Col(x => x.NrOfDaysOverdue, ExcelType.Number, "Actual days past due date", nrOfDecimals: 0) : null,
                credits.Col(x => getNotNotifiedInterestAmount(x.CreditNr, x.Status), ExcelType.Number, "Not notified interest balance", includeSum: true),
                credits.Col(x => getTotalInterestBalance(x.NotifiedInterestDebt, x.CreditNr, x.Status), ExcelType.Number, "Notified and not notified interest balance", includeSum: true),
                credits.Col(x => x.TotalNotifiedUnpaidBalance ?? 0m, ExcelType.Number, "Total unpaid notified", includeSum: true));

            if (interestDetails != null)
            {
                var s2 = excelRequest.Sheets[1];
                s2.SetColumnsAndData(interestDetails,
                    interestDetails.Col(x => x.CreditNr, ExcelType.Text, "CreditNr"),
                    interestDetails.Col(x => x.CurrentCreditNextInterestFromDate, ExcelType.Date, "CurrentCreditNextInterestFromDate"),
                    interestDetails.Col(x => x.BlockFromDate, ExcelType.Date, "BlockFromDate"),
                    interestDetails.Col(x => x.BlockToDate, ExcelType.Date, "BlockToDate"),
                    interestDetails.Col(x => x.BlockInterestAmount, ExcelType.Number, "BlockInterestAmount", includeSum: true),
                    interestDetails.Col(x => x.CapitalDebtAmount, ExcelType.Number, "CapitalDebtAmount"),
                    interestDetails.Col(x => x.InterestRate, ExcelType.Number, "InterestRate"),
                    interestDetails.Col(x => x.DayInterestAmount, ExcelType.Number, "DayInterestAmount"),
                    interestDetails.Col(x => x.NrOfDaysInBlock, ExcelType.Number, "NrOfDaysInBlock", nrOfDecimals: 0, includeSum: true));
            }

            var client = requestContext.Service().DocumentClientHttpContext;
            var result = client.CreateXlsx(excelRequest);

            return ExcelFile(result, downloadFileName: $"LoanPerformance-{request.Date.Value.ToString("yyyy-MM-dd")}.xlsx");
        }

        private class LoanPerformanceReportPartialDataModel
        {
            public string CreditNr { get; set; }
            public decimal CurrentCapitalDebt { get; set; }
            public DateTime? NewCreditTransactionDate { get; set; }
            public decimal InitialNewCreditCapitalAmount { get; set; }
            public decimal TotalNewAdditionalLoanCapitalAmount { get; set; }
            public int OverDueCount { get; set; }
            public int? NrOfDaysOverdue { get; set; }
            public decimal? InitialEffectiveInterestRatePercent { get; set; }
            public decimal? TotalNotifiedUnpaidBalance { get; set; }
            public int? ReservationOverDueCount { get; set; }
            public int? ReservationNrOfDaysOverdue { get; set; }
        }

        public class Request
        {
            public DateTime? Date { get; set; }
            public bool? IncludeNotNotifiedInterestDetails { get; set; }
            public string CreditNr { get; set; }
            public bool? IncludeActualOverdue { get; set; }
        }

        public static bool TryGetLoanPerformanceRows(Request request, CreditContextFactory creditContextFactory, CalendarDateService calendarDateService, out LoanPerformanceData data, out string failedMessage)
        {
            var c = new DataWarehouseClient();
            var p = new ExpandoObject();
            p.SetValues(d => d["forDate"] = request.Date.Value);

            var supportItems = c.FetchReportData<LoanPerformanceReportPartialDataModel>("LoanPerformanceReportPartialData1", p)?.ToDictionary(x => x.CreditNr);

            using (var context = new CreditContext())
            {
                var d = request.Date.Value.Date;
                var creditsBasis = context
                    .CreditHeaders.Where(x => x.CreatedByEvent.TransactionDate <= d);

                if (!string.IsNullOrWhiteSpace(request.CreditNr))
                {
                    creditsBasis = creditsBasis.Where(x => x.CreditNr == request.CreditNr);
                }
                data = new LoanPerformanceData();

                data.Credits = creditsBasis
                    .Select(x => new
                    {
                        x.CreditNr,
                        Ts = x.CreatedByEvent.Timestamp,
                        CreationDate = x.CreatedByEvent.TransactionDate,
                        OutgoingPayments = (context.OutgoingPaymentHeaders.Where(y => y.Transactions.Any(z => z.CreditNr == x.CreditNr)))
                            .Select(y => new
                            {
                                Event = y.CreatedByEvent,
                                PaymentFile = y.OutgoingPaymentFile,
                                Amount = y.Transactions.Where(z => z.AccountCode == TransactionAccountType.ShouldBePaidToCustomer.ToString() && z.BusinessEventId == y.CreatedByBusinessEventId).Sum(z => (decimal?)z.Amount) ?? 0m
                            })
                            .Where(y => y.Event.TransactionDate <= d)
                            .Select(y => new
                            {
                                y.Event.EventType,
                                PaymentDate = y.PaymentFile == null ? null : (DateTime?)y.PaymentFile.TransactionDate,
                                y.Amount
                            }),
                        MarginInterestRate = x
                            .DatedCreditValues
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditValueCode.MarginInterestRate.ToString())
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
                        CapitalDebt = x
                            .Transactions
                            .Where(y => y.TransactionDate <= d && y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                            .Sum(y => (decimal?)y.Amount),
                        NotifiedInterestDebt = x
                            .Transactions
                            .Where(y => y.CreditNotificationId.HasValue && y.TransactionDate <= d && y.AccountCode == TransactionAccountType.InterestDebt.ToString())
                            .Sum(y => (decimal?)y.Amount),
                        x.ProviderName,
                        StatusItem = x
                            .DatedCreditStrings
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditStringCode.CreditStatus.ToString())
                            .OrderByDescending(y => y.TransactionDate)
                            .ThenByDescending(y => y.Timestamp)
                            .FirstOrDefault()
                    })
                    .OrderBy(x => x.CreationDate)
                    .ThenBy(x => x.Ts)
                    .Select(x => new
                    {
                        x.CreditNr,
                        x.CreationDate,
                        x.MarginInterestRate,
                        x.ReferenceInterestRate,
                        x.ProviderName,
                        Status = x.StatusItem.Value,
                        StatusDate = (DateTime?)x.StatusItem.TransactionDate,
                        x.NotifiedInterestDebt
                    })
                    .ToList()
                    .Select(x =>
                    {
                        var m = supportItems?.Opt(x.CreditNr);
                        return new LoanPerformanceCredit
                        {
                            CreditNr = x.CreditNr,
                            CreationDate = x.CreationDate,
                            MarginInterestRate = x.MarginInterestRate,
                            ReferenceInterestRate = x.ReferenceInterestRate,
                            ProviderName = x.ProviderName,
                            Status = x.Status,
                            StatusDate = x.StatusDate,
                            CapitalDebt = m?.CurrentCapitalDebt,
                            InitialPaymentFileDate = m?.NewCreditTransactionDate,
                            TotalAdditionalLoanPaymentAmount = m?.TotalNewAdditionalLoanCapitalAmount,
                            InitialPaymentFileAmount = m?.InitialNewCreditCapitalAmount,
                            InitialEffectiveInterestRatePercent = m?.InitialEffectiveInterestRatePercent,
                            NrOfOverdueCount = m?.OverDueCount,
                            NrOfDaysOverdue = m?.NrOfDaysOverdue,
                            NotifiedInterestDebt = x.NotifiedInterestDebt,
                            TotalNotifiedUnpaidBalance = m?.TotalNotifiedUnpaidBalance,
                            ReservationOverDueCount = m?.ReservationOverDueCount,
                            ReservationNrOfDaysOverdue = m?.ReservationNrOfDaysOverdue
                        };
                    })
                    .ToList();

                var notNotifiedInterestRepo = new CreditNotNotifiedInterestRepository(NEnv.EnvSettings, creditContextFactory, calendarDateService);
                List<CreditNotNotifiedInterestRepository.CreditNotNotifiedInterestDetailItem> interestDetails = null;
                Action<List<CreditNotNotifiedInterestRepository.CreditNotNotifiedInterestDetailItem>> setInterestDetails = null;

                if (request.IncludeNotNotifiedInterestDetails.GetValueOrDefault())
                {
                    setInterestDetails = x => interestDetails = x;
                }

                data.NotNotifiedInterestByCreditNr = notNotifiedInterestRepo.GetNotNotifiedInterestAmount(request.Date.Value, creditNr: request.CreditNr, includeDetails: setInterestDetails);
                data.InterestDetails = interestDetails;

                failedMessage = null;
                return true;
            }
        }

        public class LoanPerformanceData
        {
            public List<LoanPerformanceCredit> Credits { get; set; }
            public List<CreditNotNotifiedInterestRepository.CreditNotNotifiedInterestDetailItem> InterestDetails { get; set; }
            public Dictionary<string, decimal> NotNotifiedInterestByCreditNr { get; set; }
        }

        public class LoanPerformanceCredit
        {
            public string CreditNr { get; set; }
            public DateTime CreationDate { get; set; }
            public decimal? MarginInterestRate { get; set; }
            public decimal? ReferenceInterestRate { get; set; }
            public string ProviderName { get; set; }
            public string Status { get; set; }
            public DateTime? StatusDate { get; set; }
            public decimal? CapitalDebt { get; set; }
            public DateTime? InitialPaymentFileDate { get; set; }
            public decimal? TotalAdditionalLoanPaymentAmount { get; set; }
            public decimal? InitialPaymentFileAmount { get; set; }
            public decimal? InitialEffectiveInterestRatePercent { get; set; }
            public int? NrOfOverdueCount { get; set; }
            public int? NrOfDaysOverdue { get; set; }
            public decimal? NotifiedInterestDebt { get; set; }
            public decimal? TotalNotifiedUnpaidBalance { get; set; }
            public int? ReservationOverDueCount { get; set; }
            public int? ReservationNrOfDaysOverdue { get; set; }
            public decimal? NotNotifiedInterestBalance { get; set; }
        }
    }
}