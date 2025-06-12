using System;
using System.Collections.Generic;
using System.Linq;
using nSavings.Code;
using nSavings.DbModel;
using nSavings.DbModel.BusinessEvents;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;
using NTech.Services.Infrastructure.NTechWs;

namespace nSavings.WebserviceMethods.Reports
{
    public class SavingsLedgerReportMethod : FileStreamWebserviceMethod<SavingsLedgerReportMethod.Request>
    {
        public override string Path => "Reports/GetSavingsLedger";

        protected override ActionResult.FileStream DoExecuteFileStream(
            NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, r => { r.Require(x => x.Date); });

            var dc = new DocumentClient();
            using (var context = new SavingsContext())
            {
                var date = request.Date.Value;
                var includeCustomerDetails = request.IncludeCustomerDetails.GetValueOrDefault();

                var sheets = new List<DocumentClientExcelRequest.Sheet>
                {
                    new DocumentClientExcelRequest.Sheet
                    {
                        AutoSizeColumns = true,
                        Title = $"Savings ledger ({date:yyyy-MM-dd})"
                    }
                };

                if (request.IncludeInterestAmountParts.GetValueOrDefault())
                {
                    sheets.Add(new DocumentClientExcelRequest.Sheet
                    {
                        AutoSizeColumns = false,
                        Title = "Interest details"
                    });
                }

                var excelRequest = new DocumentClientExcelRequest
                {
                    Sheets = sheets.ToArray()
                };

                IDictionary<string, IList<YearlyInterestCapitalizationBusinessEventManager.ResultModel.DayResultModel>>
                    interestAmountParts = null;
                Action<IDictionary<string,
                        IList<YearlyInterestCapitalizationBusinessEventManager.ResultModel.DayResultModel>>>
                    observeInterestAmountParts = null;
                if (request.IncludeInterestAmountParts.GetValueOrDefault())
                {
                    observeInterestAmountParts = d => interestAmountParts = d;
                }

                var historicalAccumulatedRates =
                    YearlyInterestCapitalizationBusinessEventManager
                        .ComputeAccumulatedInterestForAllAccountsOnHistoricalDate(context, date,
                            observeInterestAmountParts: observeInterestAmountParts);
                var lastYearYear = date.AddYears(-1).Year;
                var lastMonthDate = date.AddMonths(-1);
                var lastMonthMonth = lastMonthDate.Month;
                var lastMonthYear = lastMonthDate.Year;

                var activeRates = ChangeInterestRateBusinessEventManager.GetPerAccountActiveInterestRates(context);

                IQueryable<TmpData> accountsBase;
                if (request.UseBookKeepingDate.GetValueOrDefault())
                    accountsBase = context.SavingsAccountHeaders.Select(x => new TmpData
                    {
                        H = x,
                        CapitalBalance = x
                            .Transactions
                            .Where(y => y.BookKeepingDate <= date &&
                                        y.AccountCode == LedgerAccountTypeCode.Capital.ToString())
                            .Sum(y => (decimal?)y.Amount) ?? 0m
                    });
                else
                    accountsBase = context.SavingsAccountHeaders.Select(x => new TmpData
                    {
                        H = x,
                        CapitalBalance = x
                            .Transactions
                            .Where(y => y.TransactionDate <= date &&
                                        y.AccountCode == LedgerAccountTypeCode.Capital.ToString())
                            .Sum(y => (decimal?)y.Amount) ?? 0m
                    });

                var result = accountsBase
                    .Where(x => x.H.CreatedByEvent.TransactionDate <= date)
                    .Select(x => new
                    {
                        x.H.SavingsAccountNr,
                        x.H.MainCustomerId,
                        x.H.AccountTypeCode,
                        CreatedDate = x.H.CreatedByEvent.TransactionDate,
                        ClosedDate = x.H
                            .DatedStrings
                            .Where(y => y.Name == DatedSavingsAccountStringCode.SavingsAccountStatus.ToString() &&
                                        y.Value == SavingsAccountStatusCode.Closed.ToString() &&
                                        y.BusinessEvent.TransactionDate <= date)
                            .OrderByDescending(y => y.BusinessEventId)
                            .Select(y => (DateTime?)y.BusinessEvent.TransactionDate)
                            .FirstOrDefault(),
                        CapitalBalance = x.CapitalBalance,
                        SavingsAccountStatus = x.H
                            .DatedStrings
                            .Where(y => y.TransactionDate <= date &&
                                        y.Name == DatedSavingsAccountStringCode.SavingsAccountStatus.ToString())
                            .OrderByDescending(y => y.BusinessEventId)
                            .Select(y => y.Value)
                            .FirstOrDefault(),
                        InterestRatePercent = activeRates
                            .Where(y => y.SavingsAccountNr == x.H.SavingsAccountNr && y.TransactionDate <= date &&
                                        y.ValidFromDate <= date)
                            .OrderByDescending(y => y.ValidFromDate)
                            .ThenByDescending(y => y.BusinessEventId)
                            .Select(y => (decimal?)y.InterestRatePercent)
                            .FirstOrDefault(),
                        LatestKycAnswerDate = context
                            .SavingsAccountKycQuestions
                            .Where(y => y.SavingsAccount.MainCustomerId == x.H.MainCustomerId &&
                                        y.BusinessEvent.TransactionDate <= date)
                            .OrderByDescending(y => y.BusinessEvent.TransactionDate)
                            .Select(y => (DateTime?)y.BusinessEvent.TransactionDate)
                            .FirstOrDefault(),
                        CapitalizationsLastYear = x.H
                            .SavingsAccountInterestCapitalizations
                            .Where(y => y.ToDate.Year == lastYearYear)
                            .SelectMany(y => y.CreatedByEvent.CreatedLedgerTransactions),
                        CapitalizationsLastMonth = x.H
                            .SavingsAccountInterestCapitalizations
                            .Where(y => y.ToDate.Year == lastMonthYear && y.ToDate.Month == lastMonthMonth)
                            .SelectMany(y => y.CreatedByEvent.CreatedLedgerTransactions)
                    })
                    .Select(x => new
                    {
                        x.SavingsAccountNr,
                        x.AccountTypeCode,
                        x.MainCustomerId,
                        x.SavingsAccountStatus,
                        x.CreatedDate,
                        x.ClosedDate,
                        x.CapitalBalance,
                        WithheldTaxForLastYearAmount = x.CapitalizationsLastYear
                            .Where(y => y.SavingsAccountNr == x.SavingsAccountNr && y.AccountCode ==
                                LedgerAccountTypeCode.WithheldCapitalizedInterestTax.ToString())
                            .Sum(y => (decimal?)y.Amount) ?? 0m,
                        WithheldTaxForLastMonthAmount = x.CapitalizationsLastMonth
                            .Where(y => y.SavingsAccountNr == x.SavingsAccountNr && y.AccountCode ==
                                LedgerAccountTypeCode.WithheldCapitalizedInterestTax.ToString())
                            .Sum(y => (decimal?)y.Amount) ?? 0m,
                        CapitalizedInterestForLastYearAmount = x.CapitalizationsLastYear
                            .Where(y => y.SavingsAccountNr == x.SavingsAccountNr &&
                                        y.AccountCode == LedgerAccountTypeCode.CapitalizedInterest.ToString())
                            .Sum(y => (decimal?)y.Amount) ?? 0m,
                        CapitalizedInterestForLastMonthAmount = x.CapitalizationsLastMonth
                            .Where(y => y.SavingsAccountNr == x.SavingsAccountNr &&
                                        y.AccountCode == LedgerAccountTypeCode.CapitalizedInterest.ToString())
                            .Sum(y => (decimal?)y.Amount) ?? 0m,
                        x.InterestRatePercent,
                        x.LatestKycAnswerDate
                    })
                    .OrderByDescending(x => x.CreatedDate)
                    .ToList();

                IDictionary<int, SavingsCustomerData> customerInfoByCustomerId = null;
                if (includeCustomerDetails)
                {
                    customerInfoByCustomerId =
                        GetSavingsCustomerDataByCustomerId(new HashSet<int>(result.Select(x => x.MainCustomerId)));
                }

                var understandsSv = NEnv.ClientCfg.Country.BaseCountry.IsOneOf("SE", "FI");

                var excelTemplate = NEnv.GetOptionalExcelTemplateFilePath(
                    request.IncludeCustomerDetails.GetValueOrDefault()
                        ? "SavingsLedgerReportWithCustomerDetails.xlsx"
                        : "SavingsLedgerReportWithoutCustomerDetails.xlsx");
                if (excelTemplate.Exists && !request.IncludeInterestAmountParts.GetValueOrDefault())
                {
                    excelRequest.TemplateXlsxDocumentBytesAsBase64 =
                        Convert.ToBase64String(System.IO.File.ReadAllBytes(excelTemplate.FullName));
                }

                var useBkDate = request.UseBookKeepingDate.GetValueOrDefault();

                var s = excelRequest.Sheets[0];
                s.SetColumnsAndData(result, SkipNulls(
                    !includeCustomerDetails
                        ? null
                        : result.Col(x => customerInfoByCustomerId.Opt(x.MainCustomerId)?.CivicRegNr, ExcelType.Text,
                            HeaderText("Civic regnr")),
                    !includeCustomerDetails
                        ? null
                        : result.Col(x => customerInfoByCustomerId.Opt(x.MainCustomerId)?.FullName, ExcelType.Text,
                            HeaderText("Name")),
                    !includeCustomerDetails
                        ? null
                        : result.Col(x => customerInfoByCustomerId.Opt(x.MainCustomerId)?.Street, ExcelType.Text,
                            HeaderText("Street")),
                    !includeCustomerDetails
                        ? null
                        : result.Col(x => customerInfoByCustomerId.Opt(x.MainCustomerId)?.Zipcode, ExcelType.Text,
                            HeaderText("Zipcode")),
                    !includeCustomerDetails
                        ? null
                        : result.Col(x => customerInfoByCustomerId.Opt(x.MainCustomerId)?.City, ExcelType.Text,
                            HeaderText("City")),
                    !includeCustomerDetails
                        ? null
                        : result.Col(x => customerInfoByCustomerId.Opt(x.MainCustomerId)?.Country, ExcelType.Text,
                            HeaderText("Country")),
                    !includeCustomerDetails
                        ? null
                        : result.Col(x => customerInfoByCustomerId.Opt(x.MainCustomerId)?.Email, ExcelType.Text,
                            HeaderText("Email")),
                    !includeCustomerDetails
                        ? null
                        : result.Col(x => customerInfoByCustomerId.Opt(x.MainCustomerId)?.PhoneNr, ExcelType.Text,
                            HeaderText("Phone")),
                    result.Col(x => x.SavingsAccountNr, ExcelType.Text, HeaderText("Savings account nr")),
                    result.Col(x => x.CapitalBalance, ExcelType.Number, HeaderText("Balance"), nrOfDecimals: 2,
                        includeSum: true),
                    result.Col(x => x.CreatedDate, ExcelType.Date, HeaderText("Opened date")),
                    result.Col(x => x.ClosedDate, ExcelType.Date, HeaderText("Closed date")),
                    result.Col(x => x.InterestRatePercent / 100m, ExcelType.Percent, HeaderText("Interest rate"),
                        nrOfDecimals: 2),
                    result.Col(x => GetStatusDisplayText(x.SavingsAccountStatus), ExcelType.Text, HeaderText("Status")),
                    result.Col(x => NEnv.ClientCfg.Country.BaseCurrency, ExcelType.Text, HeaderText("Currency")),
                    result.Col(x => GetAccumulatedInterestByAccountNr(x.SavingsAccountNr) ?? 0m, ExcelType.Number,
                        HeaderText("Accumulated interest"), nrOfDecimals: 2, includeSum: true),
                    result.Col(
                        x => Math.Max(0m,
                            x.CapitalBalance + (GetAccumulatedInterestByAccountNr(x.SavingsAccountNr) ?? 0m) -
                            NEnv.MaxAllowedSavingsCustomerBalance), ExcelType.Number, HeaderText("Over max amount"),
                        nrOfDecimals: 2, includeSum: true),
                    result.Col(x => x.WithheldTaxForLastMonthAmount, ExcelType.Number,
                        HeaderText("Withheld tax from last month"), nrOfDecimals: 2, includeSum: true),
                    result.Col(x => x.WithheldTaxForLastYearAmount, ExcelType.Number,
                        HeaderText("Withheld tax from last year"), nrOfDecimals: 2, includeSum: true),
                    result.Col(x => x.CapitalizedInterestForLastMonthAmount, ExcelType.Number,
                        HeaderText("Capitalized interest from last month"), nrOfDecimals: 2, includeSum: true),
                    result.Col(x => x.CapitalizedInterestForLastYearAmount, ExcelType.Number,
                        HeaderText("Capitalized interest from last year"), nrOfDecimals: 2, includeSum: true),
                    result.Col(
                        x => x.AccountTypeCode == SavingsAccountTypeCode.StandardAccount.ToString()
                            ? NEnv.LedgerReportStandardAccountProductName ?? x.AccountTypeCode.ToString()
                            : x.AccountTypeCode.ToString(), ExcelType.Text, HeaderText("Account type")),
                    result.Col(x => null, ExcelType.Date, HeaderText("Autoclose date")),
                    result.Col(x => null, ExcelType.Text, HeaderText("Days until autoclosed")),
                    !includeCustomerDetails
                        ? null
                        : result.Col(x => x.LatestKycAnswerDate, ExcelType.Date, HeaderText("Latest KYC answer date"))
                ));

                if (request.IncludeInterestAmountParts.GetValueOrDefault())
                {
                    var i = excelRequest.Sheets[1];
                    var interestItems = result
                        .Where(x => interestAmountParts.ContainsKey(x.SavingsAccountNr))
                        .SelectMany(x => interestAmountParts[x.SavingsAccountNr].Select(y => new
                        {
                            x.SavingsAccountNr,
                            y.Date,
                            y.AccountBalance,
                            y.AccountInterestRatePercent,
                            y.Amount,
                        }))
                        .OrderBy(x => x.SavingsAccountNr)
                        .ThenBy(x => x.Date)
                        .ToList();

                    var blocks = new List<InterestBlock>();
                    InterestBlock b = null;
                    foreach (var item in interestItems)
                    {
                        if (b == null || b.SavingsAccountNr != item.SavingsAccountNr ||
                            b.AccountInterestRatePercent != item.AccountInterestRatePercent ||
                            b.AccountBalance != item.AccountBalance || b.DayInterestAmount != item.Amount)
                        {
                            b = new InterestBlock
                            {
                                SavingsAccountNr = item.SavingsAccountNr,
                                FromDate = item.Date,
                                AccountBalance = item.AccountBalance,
                                AccountInterestRatePercent = item.AccountInterestRatePercent,
                                DayInterestAmount = item.Amount,
                                NrOfDays = 0
                            };
                            blocks.Add(b);
                        }

                        b.NrOfDays += 1;
                        b.ToDate = item.Date;
                        b.TotalInterestAmount = (decimal)b.NrOfDays * b.DayInterestAmount;
                    }

                    i.SetColumnsAndData(blocks,
                        blocks.Col(x => x.SavingsAccountNr, ExcelType.Text, "SavingsAccountNr"),
                        blocks.Col(x => x.FromDate, ExcelType.Date, "FromDate"),
                        blocks.Col(x => x.ToDate, ExcelType.Date, "ToDate"),
                        blocks.Col(x => x.AccountBalance, ExcelType.Number, "AccountBalance", nrOfDecimals: 2),
                        blocks.Col(x => x.AccountInterestRatePercent, ExcelType.Number, "AccountInterestRatePercent",
                            nrOfDecimals: 2),
                        blocks.Col(x => x.DayInterestAmount, ExcelType.Number, "DayInterestAmount", nrOfDecimals: 2),
                        blocks.Col(x => x.NrOfDays, ExcelType.Number, "NrOfDays", nrOfDecimals: 0),
                        blocks.Col(x => x.TotalInterestAmount, ExcelType.Number, "TotalInterestAmount",
                            nrOfDecimals: 2));
                }

                var report = dc.CreateXlsx(excelRequest);

                return ExcelFile(report, downloadFileName: $"SavingsLedger-{date:yyyy-MM-dd}.xlsx");

                string HeaderText(string x) => excelRequest.TemplateXlsxDocumentBytesAsBase64 == null ? x : null;

                string GetStatusDisplayText(string st)
                {
                    if (!understandsSv) return st;
                    if (st == SavingsAccountStatusCode.Active.ToString()) return "Aktivt";
                    if (st == SavingsAccountStatusCode.Closed.ToString()) return "Avslutat";
                    return st == SavingsAccountStatusCode.FrozenBeforeActive.ToString() ? "Förkontroll" : st;
                }

                decimal? GetAccumulatedInterestByAccountNr(string nr) =>
                    historicalAccumulatedRates.TryGetValue(nr, out var rate)
                        ? rate
                        : null;
            }
        }

        private class TmpData
        {
            public SavingsAccountHeader H { get; set; }
            public decimal CapitalBalance { get; set; }
        }

        private static IDictionary<int, SavingsCustomerData> GetSavingsCustomerDataByCustomerId(ISet<int> customerIds)
        {
            var result = new Dictionary<int, SavingsCustomerData>(customerIds.Count);
            var c = new CustomerClient();
            foreach (var customerIdGroup in customerIds.ToArray().SplitIntoGroupsOfN(300))
            {
                foreach (var r in c.BulkFetchPropertiesByCustomerIds(new HashSet<int>(customerIdGroup), "civicRegNr",
                             "firstName", "lastName", "email", "phone", "addressStreet", "addressZipcode",
                             "addressCity", "addressCountry"))
                {
                    var firstName = r.Value.Properties.SingleOrDefault(y => y.Name == "firstName")?.Value;
                    var lastName = r.Value.Properties.SingleOrDefault(y => y.Name == "lastName")?.Value;
                    result[r.Key] = new SavingsCustomerData
                    {
                        CivicRegNr = r.Value.Properties.SingleOrDefault(y => y.Name == "civicRegNr")?.Value,
                        Email = r.Value.Properties.SingleOrDefault(y => y.Name == "email")?.Value,
                        PhoneNr = r.Value.Properties.SingleOrDefault(y => y.Name == "phone")?.Value,
                        FullName = string.Join(" ", firstName, lastName).Trim(),
                        Street = r.Value.Properties.SingleOrDefault(y => y.Name == "addressStreet")?.Value,
                        Zipcode = r.Value.Properties.SingleOrDefault(y => y.Name == "addressZipcode")?.Value,
                        City = r.Value.Properties.SingleOrDefault(y => y.Name == "addressCity")?.Value,
                        Country = r.Value.Properties.SingleOrDefault(y => y.Name == "addressCountry")?.Value
                    };
                }
            }

            return result;
        }

        private class SavingsCustomerData
        {
            public string CivicRegNr { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string PhoneNr { get; set; } //Normalize for calling?
            public string Street { get; set; }
            public string Zipcode { get; set; }
            public string City { get; set; }
            public string Country { get; set; }
        }

        private class InterestBlock
        {
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public string SavingsAccountNr { get; set; }
            public decimal AccountBalance { get; set; }
            public decimal? AccountInterestRatePercent { get; set; }
            public decimal DayInterestAmount { get; set; }
            public int NrOfDays { get; set; }
            public decimal TotalInterestAmount { get; set; }
        }

        public class Request
        {
            public DateTime? Date { get; set; }
            public bool? IncludeCustomerDetails { get; set; }
            public bool? IncludeInterestAmountParts { get; set; }
            public bool? UseBookKeepingDate { get; set; }
        }
    }
}