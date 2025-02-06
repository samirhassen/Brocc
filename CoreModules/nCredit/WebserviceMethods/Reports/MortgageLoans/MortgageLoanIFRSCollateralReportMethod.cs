using nCredit.Code;
using nCredit.Code.Services;
using nCredit.DbModel.Repository;
using nCredit.DomainModel;
using nCredit.Excel;
using Newtonsoft.Json;
using NTech;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;

namespace nCredit.WebserviceMethods.Reports
{
    public class MortgageLoanIFRSCollateralReportMethod : FileStreamWebserviceMethod<MortgageLoanIFRSCollateralReportMethod.Request>
    {
        public override string Path => "Reports/MortgageLoanIfrsCollateral";

        public override bool IsEnabled => NEnv.IsMortgageLoansEnabled && NEnv.ClientCfg.Country.BaseCountry == "FI";

        private class ExtraCreditData
        {
            public decimal? CurrentCapitalAmount { get; set; }
            public decimal? OriginalCapitalAmount { get; set; }
            public string CollateralModel { get; set; }
        }

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            var forDate = (request.Date ?? requestContext.Clock().Today).Date;

            var p = new ExpandoObject();
            p.SetValues(d => d["forDate"] = forDate);

            using (var context = new CreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                var credits = context.CreditHeaders.Where(x => x.CreatedByEvent.TransactionDate >= forDate);
                if (!string.IsNullOrWhiteSpace(request.CreditNr))
                {
                    credits = credits.Where(x => x.CreditNr == request.CreditNr);
                }

                var rows = new PartialCreditModelRepository()
                    .NewQuery(forDate)
                    .WithStrings(DatedCreditStringCode.IsForNonPropertyUse, DatedCreditStringCode.MainCreditCreditNr)
                    .ExecuteExtended(context, x =>
                    {
                        if (!string.IsNullOrWhiteSpace(request.CreditNr))
                        {
                            x = x.Where(y => y.Credit.CreditNr == request.CreditNr);
                        }

                        return x.Select(y => new PartialCreditModelRepository.CreditFinalDataWrapper<ExtraCreditData>
                        {
                            BasicCreditData = y.BasicCreditData,
                            ExtraCreditData = new ExtraCreditData
                            {
                                OriginalCapitalAmount = y.Credit.CreatedByEvent.Transactions.Where(z => z.CreditNr == y.Credit.CreditNr && z.AccountCode == TransactionAccountType.CapitalDebt.ToString()).Sum(z => (decimal?)z.Amount),
                                CurrentCapitalAmount = y.Credit.Transactions.Where(z => z.TransactionDate <= forDate && z.AccountCode == TransactionAccountType.CapitalDebt.ToString()).Sum(z => (decimal?)z.Amount),
                                CollateralModel = context
                                    .KeyValueItems
                                    .Where(z => z.KeySpace == KeyValueStoreKeySpaceCode.MortgageLoanCollateralsV1.ToString() && z.Key == y.Credit.CreditNr)
                                    .Select(z => z.Value)
                                    .FirstOrDefault()
                            }
                        });
                    });

                var collaterals = rows
                    .Where(x => x.ExtraData.CollateralModel != null)
                    .SelectMany(x =>
                    {
                        var c = JsonConvert.DeserializeObject<MortgageLoanCollateralsModel>(x.ExtraData.CollateralModel);
                        return c.Collaterals.Select(y => new
                        {
                            x.CreditNr,
                            y.IsMain,
                            OriginalExternalLoanAmountExceptHousingCompanyLoans = Numbers.ParseDecimalOrNull(y.Properties.Where(z => z.CodeName == "securityElsewhereAmount").Select(z => z.CodeValue).FirstOrDefault()),
                            OriginalExternalLoanAmountHousingCompanyLoans = Numbers.ParseDecimalOrNull(y.Properties.Where(z => z.CodeName == "housingCompanyLoans").Select(z => z.CodeValue).FirstOrDefault()),
                            OriginalExternalValuation = y
                                .Valuations
                                .Where(z => z.ValuationDate.HasValue && z.ValuationDate.Value <= forDate && z.TypeCode == "External")
                                .OrderBy(z => z.ValuationDate.Value)
                                .FirstOrDefault(),
                            CurrentExternalValuation = y
                                .Valuations
                                .Where(z => z.ValuationDate.HasValue && z.ValuationDate.Value <= forDate && z.TypeCode == "External")
                                .OrderByDescending(z => z.ValuationDate.Value)
                                .FirstOrDefault()
                        });
                    })
                    .GroupBy(x => x.CreditNr)
                    .ToDictionary(x => x.Key, x => x.ToList());

                var rowsByCreditNr = rows.ToDictionary(x => x.CreditNr, x => x);

                var rowsPre = rows.Select(x => new
                {
                    Row = x,
                    MainCreditCreditNr = x.GetString(DatedCreditStringCode.MainCreditCreditNr),
                    IsForNonPropertyUse = x.GetString(DatedCreditStringCode.IsForNonPropertyUse) == "true"
                });

                var childRowsByMainCreditNr = rowsPre
                    .Where(x => x.IsForNonPropertyUse && x.MainCreditCreditNr != null)
                    .ToDictionary(x => x.MainCreditCreditNr, x => x.Row);

                var ltv = new FinnishMortgageLoanExternalLtvCalculator();
                var loans = rowsPre.Where(x => !x.IsForNonPropertyUse).Select(x => x.Row).Select(x =>
                {
                    FinnishMortgageLoanExternalLtvCalculator.LtvDataModel ltvCurrentData = null;
                    FinnishMortgageLoanExternalLtvCalculator.LtvDataModel ltvOriginalData = null;
                    decimal? ltvOriginal = null;
                    decimal? ltvCurrent = null;
                    string ltvOriginalFormula = null;
                    string ltvCurrentFormula = null;

                    var childLoan = childRowsByMainCreditNr.Opt(x.CreditNr);
                    var objectCollaterals = CreateIfNull(collaterals.Opt(x.CreditNr)).Where(y => y.IsMain).ToList();
                    var nonObjectCollaterals = CreateIfNull(collaterals.Opt(x.CreditNr)).Where(y => !y.IsMain).ToList();
                    var c = JsonConvert.DeserializeObject<MortgageLoanCollateralsModel>(x.ExtraData.CollateralModel);

                    ltvOriginalData = new FinnishMortgageLoanExternalLtvCalculator.LtvDataModel
                    {
                        ObjectInternalMortgageLoanBalance = x.ExtraData.OriginalCapitalAmount ?? 0m,
                        ObjectHousingCompanyLoanBalance = objectCollaterals.Sum(y => y.OriginalExternalLoanAmountHousingCompanyLoans ?? 0m),
                        ObjectExternalValue = objectCollaterals.Sum(y => y.OriginalExternalValuation?.Amount ?? 0m),
                        OtherExternalValue = nonObjectCollaterals.Sum(y => y.OriginalExternalValuation?.Amount ?? 0m),
                        OtherHousingCompanyLoansBalance = nonObjectCollaterals.Sum(y => y.OriginalExternalLoanAmountHousingCompanyLoans ?? 0m),
                        OtherSecurityElsewhereAmount = nonObjectCollaterals.Sum(y => y.OriginalExternalLoanAmountExceptHousingCompanyLoans ?? 0m),
                        ObjectOtherLoans = childLoan?.ExtraData?.OriginalCapitalAmount ?? 0m
                    };

                    ltvCurrentData = new FinnishMortgageLoanExternalLtvCalculator.LtvDataModel
                    {
                        ObjectInternalMortgageLoanBalance = x.ExtraData.CurrentCapitalAmount ?? 0m,
                        ObjectHousingCompanyLoanBalance = objectCollaterals.Sum(y => y.OriginalExternalLoanAmountHousingCompanyLoans ?? 0m),
                        ObjectExternalValue = objectCollaterals.Sum(y => y.CurrentExternalValuation?.Amount ?? 0m),
                        OtherExternalValue = nonObjectCollaterals.Sum(y => y.CurrentExternalValuation?.Amount ?? 0m),
                        OtherHousingCompanyLoansBalance = nonObjectCollaterals.Sum(y => y.OriginalExternalLoanAmountHousingCompanyLoans ?? 0m),
                        OtherSecurityElsewhereAmount = nonObjectCollaterals.Sum(y => y.OriginalExternalLoanAmountExceptHousingCompanyLoans ?? 0m),
                        ObjectOtherLoans = childLoan?.ExtraData?.CurrentCapitalAmount ?? 0m
                    };

                    ltvOriginal = ltv.CalculateLtv(ltvOriginalData, y => ltvOriginalFormula = y);
                    ltvCurrent = ltv.CalculateLtv(ltvCurrentData, y => ltvCurrentFormula = y);

                    Func<decimal?, string> f = s => s?.ToString("F2", CultureInfo.InvariantCulture);

                    return new
                    {
                        CreditNr = x.CreditNr,
                        CapitalBalance = new
                        {
                            Current = x.ExtraData.CurrentCapitalAmount,
                            Original = x.ExtraData.OriginalCapitalAmount
                        },
                        ChildCapitalBalance = childLoan == null ? null : new
                        {
                            Current = childLoan.ExtraData.CurrentCapitalAmount,
                            Original = childLoan.ExtraData.OriginalCapitalAmount
                        },
                        MortgageLtvOriginal = new
                        {
                            Data = ltvOriginalData,
                            Ltv = ltvOriginal
                        },
                        MortgageLtvCurrent = new
                        {
                            Data = ltvCurrentData,
                            Ltv = ltvCurrent
                        }
                    };
                })
                .ToList();

                var sheets = new List<DocumentClientExcelRequest.Sheet>();

                sheets.Add(new DocumentClientExcelRequest.Sheet
                {
                    AutoSizeColumns = true,
                    Title = $"IFRS Collateral ({forDate.ToString("yyyy-MM-dd")})"
                });

                sheets[0].SetColumnsAndData(loans,
                    loans.Col(x => x.CreditNr, ExcelType.Text, "LoanId"),
                    loans.Col(x => x.CapitalBalance?.Original, ExcelType.Number, "Mortgage loan balance - Original"),
                    loans.Col(x => x.CapitalBalance?.Current, ExcelType.Number, "Mortgage loan balance - Current"),
                    loans.Col(x => x.ChildCapitalBalance?.Original, ExcelType.Number, "Other loan balance - Original"),
                    loans.Col(x => x.ChildCapitalBalance?.Current, ExcelType.Number, "Other loan balance - Current"),
                    loans.Col(x => x.MortgageLtvOriginal?.Ltv, ExcelType.Number, "External LTV - Original"),
                    loans.Col(x => x.MortgageLtvCurrent?.Ltv, ExcelType.Number, "External LTV - Current"),
                    loans.Col(x => x.MortgageLtvOriginal?.Data?.ObjectHousingCompanyLoanBalance, ExcelType.Number, "Object housing company loans - Original"),
                    loans.Col(x => x.MortgageLtvCurrent?.Data?.ObjectHousingCompanyLoanBalance, ExcelType.Number, "Object housing company loans - Current"),
                    loans.Col(x => x.MortgageLtvOriginal?.Data?.ObjectExternalValue, ExcelType.Number, "Object external value - Original"),
                    loans.Col(x => x.MortgageLtvCurrent?.Data?.ObjectExternalValue, ExcelType.Number, "Object external value - Current"),
                    loans.Col(x => x.MortgageLtvOriginal?.Data?.OtherHousingCompanyLoansBalance, ExcelType.Number, "Other housing company loans - Original"),
                    loans.Col(x => x.MortgageLtvCurrent?.Data?.OtherHousingCompanyLoansBalance, ExcelType.Number, "Other housing company loans - Current"),
                    loans.Col(x => x.MortgageLtvOriginal?.Data?.OtherExternalValue, ExcelType.Number, "Other external value - Original"),
                    loans.Col(x => x.MortgageLtvCurrent?.Data?.OtherExternalValue, ExcelType.Number, "Other external value - Current"),
                    loans.Col(x => x.MortgageLtvOriginal?.Data?.OtherSecurityElsewhereAmount, ExcelType.Number, "Other security elsewhere amount - Original"),
                    loans.Col(x => x.MortgageLtvCurrent?.Data?.OtherSecurityElsewhereAmount, ExcelType.Number, "Other security elsewhere amount - Current"));

                var client = requestContext.Service().DocumentClientHttpContext;

                var excelRequest = new DocumentClientExcelRequest
                {
                    Sheets = sheets.ToArray(),
                };
                var result = client.CreateXlsx(excelRequest);

                return ExcelFile(result, downloadFileName: $"IFRS-Collateral-{forDate.ToString("yyyy-MM-dd")}.xlsx");
            }
        }

        private List<T> CreateIfNull<T>(List<T> items)
        {
            //Because of the c# shit typesystem where anonymous types cannot be expressed in generic calls
            return items == null ? new List<T>() : items;
        }

        public class Request
        {
            public DateTime? Date { get; set; }
            public string CreditNr { get; set; }
        }
    }
}