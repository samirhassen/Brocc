using nCredit.Code;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using nCredit.Excel;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCredit.WebserviceMethods.Reports
{
    public class CompanyLoanCustomLedgerReportMethod : FileStreamWebserviceMethod<CompanyLoanCustomLedgerReportMethod.Request>
    {
        public override string Path => "Reports/GetCompanyLoanCustomLedger";

        public override bool IsEnabled => IsReportEnabled;

        public static bool IsReportEnabled => NEnv.IsCompanyLoansEnabled
            && NEnv.ClientCfg.Country.BaseCountry == "SE"
            && NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.customCompanyLoanLedgerReport.v1");

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var toDate = request.MonthEndDate.Value;

            using (var context = new CreditContext())
            {
                var openNotifications = CurrentNotificationStateServiceLegacy.GetCurrentOpenNotificationsStateQuery(context, toDate);

                var credits = CurrentCreditState
                    .GetCreditsQueryable(context, toDate)
                    .OrderBy(x => x.CreationDate)
                    .Where(x => x.Status == CreditStatus.Normal.ToString())
                    .Select(x => new
                    {
                        Credit = x,
                        NrOfDaysOverdue = openNotifications.Where(y => y.CreditNr == x.CreditNr).OrderBy(y => y.DueDate).Select(y => (int?)y.NrOfDaysOverdue).FirstOrDefault()
                    })
                    .ToList();

                var sheets = new List<DocumentClientExcelRequest.Sheet>();

                sheets.Add(new DocumentClientExcelRequest.Sheet
                {
                    AutoSizeColumns = true,
                    Title = $"CompanyLoan Custom Ledger ({toDate.ToString("yyyy-MM")})"
                });

                var excelRequest = new DocumentClientExcelRequest
                {
                    Sheets = sheets.ToArray()
                };

                var s = excelRequest.Sheets[0];
                var notificationSettings = NEnv.NotificationProcessSettings.GetByCreditType(CreditType.CompanyLoan);

                var clientConfig = NEnv.ClientCfg;
                var systemName = clientConfig.RequiredSetting("ntech.customCompanyLoanLedgerReport.systemName");
                var productType = clientConfig.RequiredSetting("ntech.customCompanyLoanLedgerReport.productType");
                var overdueDaysLimit = int.Parse(clientConfig.RequiredSetting("ntech.customCompanyLoanLedgerReport.overdueDaysLimit"));

                var getDividerOverride = CreditDomainModel.GetInterestDividerOverrideByCode(NEnv.ClientInterestModel);
                Func<CurrentCreditState, AmortizationPlan.Item> computeNextMonth = credit =>
                {
                    var amortizationModelCode = AmortizationModelCode.MonthlyAnnuity.ToString();
                    var amortizationModel = CreditDomainModel.CreateAmortizationModel(amortizationModelCode, () => credit.AnnuityAmount.Value, () => { throw new NotImplementedException(); }, null, null);
                    if (FixedDueDayAmortizationPlanCalculator.TrySimulateFutureMonths(
                        credit.CurrentNotNotifiedCapitalBalance ?? 0m,
                        toDate.AddMonths(1),
                        credit.NextInterestFromDate == null ? credit.CreationDate : credit.GetParsedNextInterestFromDate().Value,
                        (credit.MarginInterestRate ?? 0m) + (credit.ReferenceInterestRate ?? 0m),
                        amortizationModel,
                        credit.CurrentNotificationFeeAmount ?? 0m,
                        new List<DateTime>(),
                        notificationSettings,
                        null, out var futureMonths, out var failedMessage, getDividerOverride, onlyComputeFirstNMonths: 1))
                    {
                        if (futureMonths.Count > 0)
                            return futureMonths[0];
                    }
                    return null;
                };

                var nextAmortizationItemByCreditNr = credits
                    .Select(x => new
                    {
                        NextMonth = computeNextMonth(x.Credit),
                        CreditNr = x.Credit.CreditNr
                    })
                    .Where(x => x.NextMonth != null)
                    .ToDictionary(x => x.CreditNr, x => x.NextMonth);

                var customerData = Shared.Credits.GetCustomerData(credits.Select(x => x.Credit.CustomerId).ToHashSet(), "companyName", "orgnr");



                s.SetColumnsAndData(credits,
                    Concat(
                        Concat(
                            credits.Col(x => x.Credit.CreditNr, ExcelType.Text, "Unique ID"),
                            credits.Col(x => customerData.Opt(x.Credit.CustomerId)?.Opt("orgnr"), ExcelType.Text, "Unique ID Group of Connected Clients"),
                            credits.Col(x => productType, ExcelType.Text, "Product Type"),
                            credits.Col(x => "SEK", ExcelType.Text, "Original Currency"),
                            credits.Col(x => x.Credit.CreationDate, ExcelType.Date, "Start date"),
                            credits.Col(x => x.Credit.GetCurrentRemainingPayments(toDate, notificationSettings).LastPaymentDate, ExcelType.Date, "Maturity Date"),
                            credits.Col(x => 30, ExcelType.Number, "Interest rate payment frequency", nrOfDecimals: 0),
                            credits.Col(x => nextAmortizationItemByCreditNr.Opt(x.Credit.CreditNr)?.FutureItemDueDate, ExcelType.Date, "Upcoming interest rate payment DATE"),
                            credits.Col(x => nextAmortizationItemByCreditNr.Opt(x.Credit.CreditNr)?.InterestTransaction, ExcelType.Number, "Upcoming interest rate payment AMOUNT"),
                            credits.Col(x => 30, ExcelType.Number, "Amortization payment frequency", nrOfDecimals: 0),
                            credits.Col(x => "Annuity", ExcelType.Text, "Amortization model"),
                            credits.Col(x => nextAmortizationItemByCreditNr.Opt(x.Credit.CreditNr)?.FutureItemDueDate, ExcelType.Date, "Upcoming payment amortization DATE"),
                            credits.Col(x => nextAmortizationItemByCreditNr.Opt(x.Credit.CreditNr)?.CapitalTransaction, ExcelType.Number, "Upcoming payment amortization AMOUNT"),
                            credits.Col(x => nextAmortizationItemByCreditNr.Opt(x.Credit.CreditNr)?.NotificationFeeTransaction, ExcelType.Number, "Upcoming fees (30d)"),
                            credits.Col(x => x.Credit.MarginInterestRate.HasValue && x.Credit.ReferenceInterestRate.HasValue ? new decimal?((x.Credit.MarginInterestRate.Value + x.Credit.ReferenceInterestRate.Value) / 100m) : null, ExcelType.Percent, "Annual interest rate"),
                            credits.Col(x => x.Credit.InitalCapitalDebt, ExcelType.Number, "Nominal amount Original Exposure", nrOfDecimals: 2),
                            credits.Col(x => x.Credit.CapitalDebt, ExcelType.Number, "Original Exposure"), //NOTE: The spec says current debt even though the name is original exposure
                            credits.Col(x => x.Credit.CapitalDebt < overdueDaysLimit ? new int?() : x.NrOfDaysOverdue, ExcelType.Number, "Past due", nrOfDecimals: 0),
                            credits.Col(_ => "", ExcelType.Text, "Counterparty type"),
                            credits.Col(x => customerData.Opt(x.Credit.CustomerId)?.Opt("companyName"), ExcelType.Text, "Counterparty name"),
                            credits.Col(x => x.Credit.CompanyLoanSniKodSe, ExcelType.Text, "SNI/NACE Counterparty"),
                            credits.Col(x => customerData.Opt(x.Credit.CustomerId)?.Opt("orgnr"), ExcelType.Text, "Org. Number"),
                            credits.Col(x => "Sweden", ExcelType.Text, "Country of residence for the counterparty")
                        ),
                        EmptyTextCols(credits,
                            "Provision", "Provision previous quarter", "Deposit", "Mortgage lien value", "Mortgage lien priority",
                            "Market value underlying object", "Security type", "Interest rate reset period", "IFRS", "IFRS last period",
                            "Forborne Exposure", "h", "Exposure in reporting currency", "Provision in reporting currency" //NOTE: The single h is from the spec
                        ),
                        Concat(
                            credits.Col(x => systemName, ExcelType.Text, "System")
                        ),
                        EmptyTextCols(credits, "Typ", "Bel-Dep"),
                        Concat(
                            credits.Col(x => x.Credit.ApplicationProbabilityOfDefault, ExcelType.Number, "PD", nrOfDecimals: 0),
                            credits.Col(x => x.Credit.ApplicationLossGivenDefault, ExcelType.Number, "LGD", nrOfDecimals: 0)
                        ),
                        EmptyTextCols(credits, "GALEN FX", "GALEN FX" //NOTE: Yes the column occurs twice
                        )
                    ));

                var client = requestContext.Service().DocumentClientHttpContext;
                var result = client.CreateXlsx(excelRequest);

                return ExcelFile(result, downloadFileName: $"CompanyLoanCustomLedger-{toDate.ToString("yyyy-MM")}.xlsx");

            }
        }

        private T[] Concat<T>(params T[] items) => items;
        private T[] Concat<T>(params T[][] items) => items.SelectMany(x => x).ToArray();
        private static Tuple<DocumentClientExcelRequest.Column, Func<T, object>, Func<T, DocumentClientExcelRequest.StyleData>>[] EmptyTextCols<T>(
            IList<T> source, params string[] columnNames) => columnNames.Select(x => source.Col(_ => "", ExcelType.Text, x)).ToArray();

        public class Request
        {
            [Required]
            public DateTime? MonthEndDate { get; set; }
        }
    }

}
