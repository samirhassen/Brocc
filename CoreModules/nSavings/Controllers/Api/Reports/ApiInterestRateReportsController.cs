using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using nSavings.Code;
using nSavings.DbModel;
using nSavings.DbModel.BusinessEvents;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;
using NTech.Services.Infrastructure;
using Serilog;

namespace nSavings.Controllers.Api.Reports
{
    [NTechApi]
    public class ApiInterestRateReportsController : NController
    {
        [HttpGet, Route("Api/Reports/GetCurrentInterestRates")]
        public ActionResult GetCurrentInterestRates()
        {
            try
            {
                var dc = new DocumentClient();
                using (var context = new SavingsContext())
                {
                    var today = Clock.Today;

                    var activeRates = ChangeInterestRateBusinessEventManager
                        .GetActiveInterestRates(context)
                        .Where(x => x.ValidFromDate <= today)
                        .OrderByDescending(x => x.ValidFromDate)
                        .ThenByDescending(x => x.Id)
                        .Select(x => new
                        {
                            x.AccountTypeCode,
                            x.InterestRatePercent,
                            x.ValidFromDate,
                            x.TransactionDate,
                            x.AppliesToAccountsSinceBusinessEventId
                        })
                        .ToList();

                    var sheets = new List<DocumentClientExcelRequest.Sheet>
                    {
                        new DocumentClientExcelRequest.Sheet
                        {
                            AutoSizeColumns = true,
                            Title = $"Interest history ({today:yyyy-MM-dd})"
                        }
                    };

                    var request = new DocumentClientExcelRequest
                    {
                        Sheets = sheets.ToArray()
                    };

                    var s = request.Sheets[0];
                    s.SetColumnsAndData(activeRates,
                        activeRates.Col(x => x.AccountTypeCode, ExcelType.Text, "Account Type"),
                        activeRates.Col(x => x.TransactionDate, ExcelType.Date, "Transaction Date"),
                        activeRates.Col(x => x.ValidFromDate, ExcelType.Date, "Valid From Date"),
                        activeRates.Col(x => x.InterestRatePercent / 100m, ExcelType.Percent, "Interest Rate"),
                        activeRates.Col(x => x.AppliesToAccountsSinceBusinessEventId.HasValue ? "Yes" : "No",
                            ExcelType.Text, "New accounts only"));

                    var report = dc.CreateXlsx(request);

                    return new FileStreamResult(report, XlsxContentType)
                    {
                        FileDownloadName = $"currentInterestRates-{today:yyyy-MM-dd}.xlsx"
                    };
                }
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Failed to create currentInterestRates report");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Route("Api/Reports/GetInterestRatesPerAccount")]
        [HttpGet()]
        public ActionResult GetInterestRatesPerAccount(DateTime date, bool? includeClosedAccounts)
        {
            try
            {
                var dc = new DocumentClient();
                using (var context = new SavingsContext())
                {
                    var rates = ChangeInterestRateBusinessEventManager.GetPerAccountActiveInterestRates(context);

                    var accountsBase = context
                        .SavingsAccountHeaders
                        .Where(x => x.CreatedByEvent.TransactionDate <= date)
                        .Select(x => new
                        {
                            x.SavingsAccountNr,
                            AccountCreatedDate = x.CreatedByEvent.TransactionDate,
                            CapitalTransactions = x.Transactions
                                .Where(y => y.AccountCode == LedgerAccountTypeCode.Capital.ToString())
                                .Select(y => new
                                {
                                    y.Amount
                                }),
                            x.CreatedByBusinessEventId,
                            x.AccountTypeCode,
                            CurrentStatusItem = x
                                .DatedStrings
                                .Where(y => y.Name == DatedSavingsAccountStringCode.SavingsAccountStatus.ToString() &&
                                            y.TransactionDate <= date)
                                .OrderByDescending(y => y.BusinessEventId)
                                .FirstOrDefault()
                        })
                        .Select(x => new
                        {
                            x.SavingsAccountNr,
                            x.AccountCreatedDate,
                            x.CreatedByBusinessEventId,
                            x.AccountTypeCode,
                            x.CapitalTransactions,
                            ClosedDate = x.CurrentStatusItem.Value == SavingsAccountStatusCode.Closed.ToString()
                                ? (DateTime?)x.CurrentStatusItem.TransactionDate
                                : null
                        });

                    if (!includeClosedAccounts.GetValueOrDefault())
                    {
                        accountsBase = accountsBase.Where(x => !x.ClosedDate.HasValue);
                    }

                    var accounts = accountsBase
                        .Select(x => new
                        {
                            x.SavingsAccountNr,
                            x.AccountCreatedDate,
                            x.CreatedByBusinessEventId,
                            x.AccountTypeCode,
                            x.ClosedDate,
                            CurrentRate = rates
                                .Where(y => y.SavingsAccountNr == x.SavingsAccountNr && y.ValidFromDate <= date &&
                                            (!x.ClosedDate.HasValue || y.ValidFromDate <= x.ClosedDate.Value))
                                .OrderByDescending(y => y.BusinessEventId)
                                .Select(y => new
                                {
                                    y.InterestRatePercent,
                                    y.TransactionDate,
                                    y.ValidFromDate
                                })
                                .FirstOrDefault(),
                            Balance = x.CapitalTransactions.Sum(y => (decimal?)y.Amount) ?? 0m,
                        })
                        .ToList();

                    var summaryModel = accounts
                        .GroupBy(x => new { x.AccountTypeCode, x.CurrentRate?.InterestRatePercent })
                        .Select(x => new
                        {
                            x.Key.AccountTypeCode,
                            x.Key.InterestRatePercent,
                            Count = x.Count(),
                            Balance = x.Sum(y => (decimal?)y.Balance) ?? 0m,
                            ExampleSavingsAccountNr = x.First().SavingsAccountNr
                        })
                        .ToList();

                    var sheets = new List<DocumentClientExcelRequest.Sheet>
                    {
                        new DocumentClientExcelRequest.Sheet
                        {
                            AutoSizeColumns = true,
                            Title = $"Summary ({date:yyyy-MM-dd})"
                        }
                    };
                    sheets[0].SetColumnsAndData(summaryModel,
                        summaryModel.Col(x => x.AccountTypeCode, ExcelType.Text, "Account Type"),
                        summaryModel.Col(x => x.InterestRatePercent / 100m, ExcelType.Percent, "Interest Rate"),
                        summaryModel.Col(x => x.Count, ExcelType.Number, "Count", nrOfDecimals: 0, includeSum: true),
                        summaryModel.Col(x => x.ExampleSavingsAccountNr, ExcelType.Text, "Example savings account nr"),
                        summaryModel.Col(x => x.Balance, ExcelType.Number, "Balance", includeSum: true));

                    sheets.Add(new DocumentClientExcelRequest.Sheet
                    {
                        AutoSizeColumns = true,
                        Title = "Details"
                    });
                    var sheet1Cols = DocumentClientExcelRequest.CreateDynamicColumnList(accounts);

                    sheet1Cols.Add(accounts.Col(x => x.AccountTypeCode, ExcelType.Text, "Account Type"));
                    sheet1Cols.Add(accounts.Col(x => x.SavingsAccountNr, ExcelType.Text, "Savings account nr"));
                    sheet1Cols.Add(accounts.Col(x => x.AccountCreatedDate, ExcelType.Date, "Account created date"));
                    sheet1Cols.Add(accounts.Col(x => x.CurrentRate?.TransactionDate, ExcelType.Date,
                        "Rate created date"));
                    sheet1Cols.Add(accounts.Col(x => x.CurrentRate?.ValidFromDate, ExcelType.Date,
                        "Rate valid from date"));
                    sheet1Cols.Add(accounts.Col(x => x.CurrentRate?.InterestRatePercent / 100m, ExcelType.Percent,
                        "Interest Rate"));
                    if (includeClosedAccounts.GetValueOrDefault())
                    {
                        sheet1Cols.Add(accounts.Col(x => x.ClosedDate, ExcelType.Date, "Closed date"));
                    }

                    sheet1Cols.Add(accounts.Col(x => x.Balance, ExcelType.Number, "Balance"));

                    sheets[1].SetColumnsAndData(accounts, sheet1Cols.ToArray());

                    var request = new DocumentClientExcelRequest
                    {
                        Sheets = sheets.ToArray()
                    };
                    var report = dc.CreateXlsx(request);

                    return new FileStreamResult(report, XlsxContentType)
                    {
                        FileDownloadName = $"interestRatesPerAccount-{date:yyyy-MM-dd}.xlsx"
                    };
                }
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Failed to create interestRatesPerAccount report");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }
    }
}