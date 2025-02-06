using nCredit.Code;
using nCredit.DbModel.Repository;
using nCredit.Excel;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechAuthorizeCreditHigh]
    [NTechApi]
    public class ApiReportsQuarterlyRATIController : NController
    {
        protected override bool IsEnabled => !NEnv.IsStandardUnsecuredLoansEnabled;

        private List<RATIReportDataWarehouseModel.RatiBasisData> GetRatiBasisDataForQuarter(RATIReportDataWarehouseModel.RATIQuarter q, DataWarehouseClient client, bool bypassDw)
        {
            if (bypassDw)
            {
                using (var context = new CreditContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    context.Database.CommandTimeout = 60 * 20; //20 minutes

                    var r = new RATIReportDataWarehouseModel();
                    var allCreditNrs = context.CreditHeaders.Select(x => x.CreditNr).ToArray();
                    var missingPaymentPlanCreditNrs = new HashSet<string>();

                    var fromDate = q.FromDate;
                    var toDate = q.ToDate;

                    var result = SplitIntoGroupsOfN(allCreditNrs, 100).SelectMany(g => r
                           .GetRatiModel(context, fromDate, toDate, onlyTheseCreditNrs: g.ToList(), obeserveCreditNrMissingRuntime: s => missingPaymentPlanCreditNrs.Add(s))
                           .Select(x => new RATIReportDataWarehouseModel.RatiBasisData
                           {
                               CreditNr = x.CreditNr,
                               StartDate = x.StartDate,
                               InitialRuntimeInMonths = x.InitialRuntimeInMonths,
                               CurrentRuntimeInMonths = x.CurrentRuntimeInMonths,
                               InitialCapitalDebt = x.InitialCapitalDebt,
                               CurrentCapitalDebt = x.CurrentCapitalDebt,
                               InitialInterestRate = x.InitialInterestRate,
                               CurrentInterestRate = x.CurrentInterestRate,
                               InitialEffectiveInterest = x.InitialEffectiveInterest,
                               DebtCollectionDate = x.DebtCollectionDate,
                               DebtCollectionInterestDebt = x.DebtCollectionInterestDebt,
                               DebtCollectionCapitalDebt = x.DebtCollectionCapitalDebt,
                               CurrentInterestDebt = x.CurrentInterestDebt,
                               OverdueDays = x.OverdueDays
                           })
                           .ToList())
                            .ToList();

                    if (missingPaymentPlanCreditNrs.Any())
                    {
                        throw new Exception($"MergeQuarterlyRatiBasis: {missingPaymentPlanCreditNrs.Count} credits had terms that meant they would never be paid and so have no runtime. Examples: {string.Join(", ", missingPaymentPlanCreditNrs.Take(5))}");
                    }

                    return result;
                }
            }
            else
            {
                var p = new ExpandoObject();
                (p as IDictionary<string, object>)["fromDate"] = q.FromDate;
                (p as IDictionary<string, object>)["toDate"] = q.ToDate;

                return client.FetchReportData<RATIReportDataWarehouseModel.RatiBasisData>("quarterlyRATIBasis", p);
            }
        }

        private List<RATIReportDataWarehouseModel.BusinessEventRatiData> GetBusinessEventRatiDataForQuarter(RATIReportDataWarehouseModel.RATIQuarter q, DataWarehouseClient client, bool bypassDw)
        {
            if (bypassDw)
            {
                using (var context = new CreditContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    context.Database.CommandTimeout = 60 * 20; //20 minutes

                    var r = new RATIReportDataWarehouseModel();
                    var allCreditNrs = context.CreditHeaders.Select(x => x.CreditNr).ToArray();

                    var missingPaymentPlanCreditNrs = new HashSet<string>();

                    var fromDate = q.FromDate;
                    var toDate = q.ToDate;

                    var newAdditionalLoans = SplitIntoGroupsOfN(allCreditNrs, 100)
                            .SelectMany(g => r
                            .GetRatiBusinessEventRatiDataDataForPeriod(context, fromDate, toDate, BusinessEventType.NewAdditionalLoan, s => missingPaymentPlanCreditNrs.Add(s), onlyTheseCreditNrs: g.ToList()))
                            .ToList();

                    var newTermChanges = SplitIntoGroupsOfN(allCreditNrs, 100)
                            .SelectMany(g => r
                            .GetRatiBusinessEventRatiDataDataForPeriod(context, fromDate, toDate, BusinessEventType.AcceptedCreditTermsChange, s => missingPaymentPlanCreditNrs.Add(s), onlyTheseCreditNrs: g.ToList()))
                            .ToList();

                    if (missingPaymentPlanCreditNrs.Any())
                    {
                        Log.Warning($"MergeQuarterlyRatiBusinessEvents: {missingPaymentPlanCreditNrs.Count} credits have terms that mean they will never be paid. Examples: {string.Join(", ", missingPaymentPlanCreditNrs.Take(5))}");
                    }

                    return newAdditionalLoans
                        .Union(newTermChanges)
                        .OrderBy(x => x.TransactionDate)
                        .ThenBy(x => x.EventId)
                        .ToList();
                }
            }
            else
            {
                var p = new ExpandoObject();
                (p as IDictionary<string, object>)["fromDate"] = q.FromDate;
                (p as IDictionary<string, object>)["toDate"] = q.ToDate;

                var result = client.FetchReportData<RATIReportDataWarehouseModel.BusinessEventRatiData>("quarterlyRATIBusinessEvents", p)
                    .OrderBy(x => x.TransactionDate)
                    .ThenBy(x => x.EventId)
                    .ToList();

                return result;

            }
        }

        private List<RATIReportDataWarehouseModel.CreditRatiData> GetNewCredits(
            IEnumerable<RATIReportDataWarehouseModel.RatiBasisData> newCreditsInitialBasis,
            List<RATIReportDataWarehouseModel.BusinessEventRatiData> businessEvents,
            Dictionary<string, RATIReportDataWarehouseModel.RatiBasisData> creditsByCreditNr,
            out HashSet<string> newCreditNrsMissingCreditData
            )
        {
            var newCreditsBasis = newCreditsInitialBasis
                .Select(x => new
                {
                    x.CreditNr,
                    InitialRuntimeRoundedToYears = InitialRuntimeRoundedToYears(x.InitialRuntimeInMonths),
                    x.InitialCapitalDebt,
                    x.InitialInterestRate,
                    x.InitialEffectiveInterest,
                    CurrentInterestDebt = (decimal?)x.CurrentInterestDebt,
                    DebtCollectionInterestDebt = (decimal?)x.DebtCollectionInterestDebt,
                    InterestDebt = (decimal?)(x.DebtCollectionDate.HasValue ? x.DebtCollectionInterestDebt : x.CurrentInterestDebt),
                    IsMissingCreditData = false
                })
                .Union(businessEvents
                .Where(x => x.EventType == BusinessEventType.NewAdditionalLoan.ToString())
                .Select(x =>
                {
                    var c = creditsByCreditNr.Opt(x.CreditNr);
                    decimal? interestDebt = null;
                    if (c != null)
                        interestDebt = c.DebtCollectionDate.HasValue
                        ? creditsByCreditNr[x.CreditNr].CurrentInterestDebt * (x.CurrentInterstDebtFraction ?? 0m)
                        : creditsByCreditNr[x.CreditNr].DebtCollectionInterestDebt * (x.CurrentInterstDebtFraction ?? 0m);

                    return new
                    {
                        x.CreditNr,
                        InitialRuntimeRoundedToYears = InitialRuntimeRoundedToYears(x.AfterEventRuntimeInMonths ?? 0),
                        InitialCapitalDebt = x.ByEventAddedCapitalDebt ?? 0m,
                        InitialInterestRate = x.AfterEventInterestRate,
                        InitialEffectiveInterest = x.AfterEventEffectiveInterest,
                        CurrentInterestDebt = c?.CurrentInterestDebt * (x.CurrentInterstDebtFraction ?? 0m),
                        DebtCollectionInterestDebt = c?.DebtCollectionInterestDebt * (x.CurrentInterstDebtFraction ?? 0m),
                        InterestDebt = interestDebt,
                        IsMissingCreditData = c == null
                    };
                }));

            newCreditNrsMissingCreditData = newCreditsBasis.Where(x => x.IsMissingCreditData).Select(x => x.CreditNr).ToHashSet();

            var newCredits = newCreditsBasis
                .GroupBy(x => x.InitialRuntimeRoundedToYears)
                .OrderBy(x => x.Key)
                .Select(x => new RATIReportDataWarehouseModel.CreditRatiData
                {
                    InitialMaturity = x.Key,
                    Count = x.Count(),
                    ExampleCreditNr = x.Select(y => y.CreditNr).FirstOrDefault(),
                    InitialCapitalDebt = x.Sum(y => y.InitialCapitalDebt),
                    WeightedInitialInterestRate = x.Sum(y => y.InitialCapitalDebt) == 0 ? 0m : (x.Sum(y => y.InitialCapitalDebt * (y.InitialInterestRate ?? 0m)) / x.Sum(y => y.InitialCapitalDebt)),
                    WeightedInitialEffectiveInterestRate = x.Sum(y => y.InitialCapitalDebt) == 0 ? 0m : (x.Sum(y => y.InitialCapitalDebt * (y.InitialEffectiveInterest ?? 0m)) / x.Sum(y => y.InitialCapitalDebt)),
                    InterestDebt = x.Sum(y => y.InterestDebt)
                })
                .ToList();

            return newCredits;
        }

        public List<RATIReportDataWarehouseModel.RenegotiatedCreditsRatiData> GetRenegotiatedCredits
            (List<RATIReportDataWarehouseModel.BusinessEventRatiData> businessEvents,
            Dictionary<string, RATIReportDataWarehouseModel.RatiBasisData> creditsByCreditNr
            )
        {
            var renegotiatedCredits = businessEvents
                 .Where(x => x.EventType == BusinessEventType.AcceptedCreditTermsChange.ToString() || x.EventType == BusinessEventType.NewAdditionalLoan.ToString())
                 .Select(x => new
                 {
                     Basis = x,
                     RuntimeRoundedToYears = InitialRuntimeRoundedToYears(x.AfterEventRuntimeInMonths ?? 0),
                     CapitalAmount = x.EventType == BusinessEventType.NewAdditionalLoan.ToString()
                         ? (x.AfterEventCapitalDebt - x.ByEventAddedCapitalDebt)
                         : x.AfterEventCapitalDebt,
                     CurrentInterestDebt = creditsByCreditNr.ContainsKey(x.CreditNr)
                         ? creditsByCreditNr[x.CreditNr].CurrentInterestDebt * (x.CurrentInterstDebtFraction ?? 0m)
                         : 0m
                 })
                 .GroupBy(x => x.RuntimeRoundedToYears)
                 .OrderBy(x => x.Key)
                 .Select(x => new RATIReportDataWarehouseModel.RenegotiatedCreditsRatiData
                 {
                     Maturity = x.Key,
                     Count = x.Count(),
                     ExampleCreditNr = x.Select(y => y.Basis.CreditNr).FirstOrDefault(),
                     CapitalDebt = x.Sum(y => y.CapitalAmount),
                     WeightedInterestRate = x.Sum(y => y.CapitalAmount) == 0
                         ? 0m
                         : (x.Sum(y => y.CapitalAmount * (y.Basis.AfterEventInterestRate ?? 0m)) / x.Sum(y => y.CapitalAmount)),
                     WeightedEffectiveInterestRate = x.Sum(y => y.CapitalAmount) == 0
                         ? 0m
                         : (x.Sum(y => y.CapitalAmount * (y.Basis.AfterEventEffectiveInterest ?? 0m)) / x.Sum(y => y.CapitalAmount)),
                     InterestDebt = x.Sum(y => (decimal?)y.CurrentInterestDebt) ?? 0m
                 })
                 .ToList();

            return renegotiatedCredits;
        }

        private static IEnumerable<IEnumerable<T>> SplitIntoGroupsOfN<T>(T[] array, int n)
        {
            for (var i = 0; i < (float)array.Length / n; i++)
            {
                yield return array.Skip(i * n).Take(n);
            }
        }

        int InitialRuntimeRoundedToYears(int runtimeInMonths) => (int)Math.Ceiling((decimal)runtimeInMonths / 12m);

        [Route("Api/Reports/QuarterlyRATI")]
        [HttpGet]
        public ActionResult Get(DateTime quarterEndDate, bool? bypassDw)
        {
            if (!NEnv.IsUnsecuredLoansEnabled)
                return HttpNotFound();

            try
            {
                var q = RATIReportDataWarehouseModel.RATIQuarter.ContainingRatiDate(quarterEndDate);

                DateTime fromDate = q.FromDate;
                DateTime toDate = q.ToDate;

                DateTime lastMonthFromDate = q.LastMonthFromDate;
                DateTime lastMonthToDate = q.LastMonthToDate;

                var dc = new DataWarehouseClient();


                Func<RATIReportDataWarehouseModel.RatiBasisData, int> currentRuntimeCategory = r => r.CurrentRuntimeInMonths < 12 ? 0 : (r.CurrentRuntimeInMonths <= 24 ? 1 : 2);

                //quarter new credits 
                var businessEvents = GetBusinessEventRatiDataForQuarter(q, dc, bypassDw ?? false);
                var credits = GetRatiBasisDataForQuarter(q, dc, bypassDw ?? false);

                var allCreditNrs = credits.Select(x => x.CreditNr).ToHashSet();
                businessEvents = businessEvents.Where(x => allCreditNrs.Contains(x.CreditNr)).ToList(); //Remove events on credits that are settled                                
                var businessEventsLastMonth = businessEvents.Where(x => x.TransactionDate >= q.LastMonthFromDate && x.TransactionDate <= q.LastMonthToDate).ToList();

                var creditsByCreditNr = credits.ToDictionary(x => x.CreditNr);

                var newCreditsInitialBasis = credits.Where(x => x.StartDate >= fromDate);
                var newCreditsInitialBasisLastMonth = newCreditsInitialBasis.Where(x => x.StartDate >= q.LastMonthFromDate && x.StartDate <= q.LastMonthToDate);

                var newCreditNrsMissingCreditData = new HashSet<string>();

                var newCredits = GetNewCredits(newCreditsInitialBasis, businessEvents, creditsByCreditNr, out newCreditNrsMissingCreditData);
                var newCreditsLastMonth = GetNewCredits(newCreditsInitialBasisLastMonth, businessEventsLastMonth, creditsByCreditNr, out var _);

                var renegotiatedCredits = GetRenegotiatedCredits(businessEvents, creditsByCreditNr);
                var renegotiatedCreditsLastMonth = GetRenegotiatedCredits(businessEventsLastMonth, creditsByCreditNr);

                var portfolioCredits = credits
                    .Select(x => new
                    {
                        x.CreditNr,
                        InitialRuntimeRoundedToYears = InitialRuntimeRoundedToYears(x.InitialRuntimeInMonths),
                        CurrentRuntimeCategory = currentRuntimeCategory(x),
                        x.CurrentCapitalDebt,
                        x.CurrentInterestDebt,
                        x.CurrentInterestRate,
                        x.DebtCollectionCapitalDebt,
                        x.DebtCollectionInterestDebt,
                        IsOnDebtCollection = x.DebtCollectionDate.HasValue,
                        IsOverdue90Plus = !x.DebtCollectionDate.HasValue && x.OverdueDays >= 90
                    })
                    .GroupBy(x => new
                    {
                        x.InitialRuntimeRoundedToYears,
                        x.CurrentRuntimeCategory
                    })
                    .OrderBy(x => x.Key.InitialRuntimeRoundedToYears)
                    .ThenBy(x => x.Key.CurrentRuntimeCategory)
                    .Select(x => new
                    {
                        InitialRuntimeRoundedToYears = x.Key.InitialRuntimeRoundedToYears,
                        CurrentRuntimeCategory = x.Key.CurrentRuntimeCategory,
                        CurrentNormalCapitalDebt = x.Where(y => !y.IsOnDebtCollection && !y.IsOverdue90Plus).Sum(y => (decimal?)y.CurrentCapitalDebt) ?? 0m,
                        WeightedNormalInterestSum = x.Where(y => !y.IsOnDebtCollection && !y.IsOverdue90Plus).Sum(y => (decimal?)(y.CurrentCapitalDebt * y.CurrentInterestRate)) ?? 0m,
                        CurrentCapital90PlusDebt = x.Where(y => y.IsOverdue90Plus).Sum(y => (decimal?)y.CurrentCapitalDebt) ?? 0m,
                        CurrentCapitalDebtCollectionDebt = x.Where(y => y.IsOnDebtCollection).Sum(y => (decimal?)y.DebtCollectionCapitalDebt) ?? 0m,
                        CurrentNonDebtCollectionInterestDebt = (x.Where(y => !y.IsOnDebtCollection).Sum(y => (decimal?)y.CurrentInterestDebt) ?? 0m),
                        CurrentDebtCollectionInterestDebt = (x.Where(y => y.IsOnDebtCollection).Sum(y => (decimal?)y.DebtCollectionInterestDebt) ?? 0m),
                        Count = x.Count(),
                        ExampleCreditNr = x.Select(y => y.CreditNr).FirstOrDefault(),
                        Current = x
                    })
                    .Select(x => new
                    {
                        x.InitialRuntimeRoundedToYears,
                        x.CurrentRuntimeCategory,
                        x.CurrentNormalCapitalDebt,
                        WeightedCurrentInterestRate = x.CurrentNormalCapitalDebt == 0m ? 0m : Math.Round(x.WeightedNormalInterestSum / x.CurrentNormalCapitalDebt, 2),
                        x.CurrentCapital90PlusDebt,
                        x.CurrentNonDebtCollectionInterestDebt,
                        x.CurrentDebtCollectionInterestDebt,
                        x.CurrentCapitalDebtCollectionDebt,
                        x.Count,
                        x.ExampleCreditNr
                    })
                    .ToList();

                var request = GetDocumentRequest(q);

                //New Credits
                SetNewCreditsQuarterColumns(request, newCredits);
                SetNewCreditsLastMonthColumns(request, newCreditsLastMonth);

                //Renegotiated Credits
                SetRenegotiatedCreditsQuarterColumns(request, renegotiatedCredits);
                SetRenegotiatedCreditsLastMonthColumns(request, renegotiatedCreditsLastMonth);


                Func<int, string> translateRemainingMaturity = i =>
                    {
                        switch (i)
                        {
                            case 0: return "<1";
                            case 1: return "1-2";
                            default: return ">2";
                        }
                    };

                request.Sheets[4].SetColumnsAndData(portfolioCredits,
                    portfolioCredits.Col(x => x.InitialRuntimeRoundedToYears, ExcelType.Number, "Initial maturity", nrOfDecimals: 0),
                    portfolioCredits.Col(x => translateRemainingMaturity(x.CurrentRuntimeCategory), ExcelType.Text, "Rem. maturity"),
                    portfolioCredits.Col(x => x.CurrentNormalCapitalDebt, ExcelType.Number, "Current healthy capital", nrOfDecimals: 2, includeSum: true),
                    portfolioCredits.Col(x => x.WeightedCurrentInterestRate / 100m, ExcelType.Percent, "Current nominal interest"),
                    portfolioCredits.Col(x => x.CurrentCapital90PlusDebt, ExcelType.Number, "Current 90+ capital", nrOfDecimals: 2, includeSum: true),
                    portfolioCredits.Col(x => x.CurrentNonDebtCollectionInterestDebt, ExcelType.Number, "Current (exc debt col.) interest debt", nrOfDecimals: 2, includeSum: true),
                    portfolioCredits.Col(x => x.CurrentDebtCollectionInterestDebt, ExcelType.Number, "Debt collection interest debt", nrOfDecimals: 2, includeSum: true),

                    //<--These are for testing so maybe only keep in test or have setting to turn on/off
                    portfolioCredits.Col(x => x.Count, ExcelType.Number, "Count", nrOfDecimals: 0, includeSum: true),
                    portfolioCredits.Col(x => x.ExampleCreditNr, ExcelType.Text, "Example"),
                    portfolioCredits.Col(x => x.CurrentCapitalDebtCollectionDebt, ExcelType.Number, "Debt col capital", nrOfDecimals: 2, includeSum: true));

                var rawCredits =
                    credits
                        .OrderBy(x => x.CreditNr)
                        .ToList();


                request.Sheets[5].SetColumnsAndData(rawCredits,
                    rawCredits.Col(x => x.CreditNr, ExcelType.Text, "CreditNr"),
                    rawCredits.Col(x => x.StartDate, ExcelType.Date, "StartDate"),
                    rawCredits.Col(x => InitialRuntimeRoundedToYears(x.InitialRuntimeInMonths), ExcelType.Number, "InitialRuntimeRoundedToYears", nrOfDecimals: 0),
                    rawCredits.Col(x => translateRemainingMaturity(currentRuntimeCategory(x)), ExcelType.Text, "CurrentRuntimeCategory"),
                    rawCredits.Col(x => x.InitialRuntimeInMonths, ExcelType.Number, "InitialRuntimeInMonths", nrOfDecimals: 0),
                    rawCredits.Col(x => x.CurrentRuntimeInMonths, ExcelType.Number, "CurrentRuntimeInMonths", nrOfDecimals: 0),
                    rawCredits.Col(x => x.InitialCapitalDebt, ExcelType.Number, "InitialCapitalDebt", nrOfDecimals: 2, includeSum: true),
                    rawCredits.Col(x => x.CurrentCapitalDebt, ExcelType.Number, "CurrentCapitalDebt", nrOfDecimals: 2, includeSum: true),
                    rawCredits.Col(x => x.InitialInterestRate / 100m, ExcelType.Percent, "InitialInterestRate", nrOfDecimals: 2),
                    rawCredits.Col(x => x.CurrentInterestRate / 100m, ExcelType.Percent, "CurrentInterestRate", nrOfDecimals: 2),
                    rawCredits.Col(x => x.InitialEffectiveInterest / 100m, ExcelType.Percent, "InitialEffectiveInterest", nrOfDecimals: 2),
                    rawCredits.Col(x => x.CurrentInterestDebt, ExcelType.Number, "CurrentInterestDebt", nrOfDecimals: 2, includeSum: true),
                    rawCredits.Col(x => x.OverdueDays, ExcelType.Number, "OverdueDays", nrOfDecimals: 0),
                    rawCredits.Col(x => x.DebtCollectionDate, ExcelType.Date, "DebtCollectionDate"),
                    rawCredits.Col(x => x.DebtCollectionInterestDebt, ExcelType.Number, "DebtCollectionInterestDebt", nrOfDecimals: 2, includeSum: true),
                    rawCredits.Col(x => x.DebtCollectionCapitalDebt, ExcelType.Number, "DebtCollectionCapitalDebt", nrOfDecimals: 2, includeSum: true),
                    rawCredits.Col(x => newCreditNrsMissingCreditData.Contains(x.CreditNr) ? "Missing creditdata" : null, ExcelType.Text, "Issues"));

                request.Sheets[6].SetColumnsAndData(businessEvents,
                    businessEvents.Col(x => x.CreditNr, ExcelType.Text, "CreditNr"),
                    businessEvents.Col(x => x.EventType, ExcelType.Text, "EventType"),
                    businessEvents.Col(x => x.TransactionDate, ExcelType.Date, "TransactionDate"),
                    businessEvents.Col(x => x.AfterEventRuntimeInMonths, ExcelType.Number, "AfterEventRuntimeInMonths", nrOfDecimals: 0),
                    businessEvents.Col(x => x.AfterEventCapitalDebt, ExcelType.Number, "AfterEventCapitalDebt", nrOfDecimals: 2),
                    businessEvents.Col(x => x.AfterEventInterestRate / 100m, ExcelType.Percent, "AfterEventInterestRate", nrOfDecimals: 2),
                    businessEvents.Col(x => x.AfterEventEffectiveInterest / 100m, ExcelType.Percent, "AfterEventEffectiveInterest", nrOfDecimals: 2),
                    businessEvents.Col(x => x.ByEventAddedCapitalDebt, ExcelType.Number, "ByEventAddedCapitalDebt", nrOfDecimals: 2, includeSum: true),
                    businessEvents.Col(x => x.CurrentInterstDebtFraction, ExcelType.Percent, "CurrentInterstDebtFraction", nrOfDecimals: 2),
                    businessEvents.Col(x => x.EventId, ExcelType.Number, "EventId", nrOfDecimals: 2),
                    businessEvents.Col(x => newCreditNrsMissingCreditData.Contains(x.CreditNr) ? "Missing creditdata" : null, ExcelType.Text, "Issues"));

                var client = Service.DocumentClientHttpContext;
                var report = client.CreateXlsx(request);

                return new FileStreamResult(report, XlsxContentType) { FileDownloadName = $"QuarterlyRATIReport-{fromDate.ToString("yyyy-MM-dd")}-{toDate.ToString("yyyy-MM-dd")}.xlsx" };
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Failed to create quarterly RATI report");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        private static void SetNewCreditsQuarterColumns(DocumentClientExcelRequest request, List<RATIReportDataWarehouseModel.CreditRatiData> newCredits)
        {
            request.Sheets[0].SetColumnsAndData(newCredits,
                newCredits.Col(x => x.InitialMaturity, ExcelType.Number, "Initial maturity", nrOfDecimals: 0),
                newCredits.Col(x => x.InitialCapitalDebt, ExcelType.Number, "Initial capital", nrOfDecimals: 2, includeSum: true),
                newCredits.Col(x => x.WeightedInitialInterestRate / 100m, ExcelType.Percent, "Initial nominal interest"),
                newCredits.Col(x => x.WeightedInitialEffectiveInterestRate / 100m, ExcelType.Percent, "Initial effective interest"),
                newCredits.Col(x => x.InterestDebt, ExcelType.Number, "Current interest debt", nrOfDecimals: 2, includeSum: true),

                //<--These are for testing so maybe only keep in test or have setting to turn on/off
                newCredits.Col(x => x.Count, ExcelType.Number, "Count", nrOfDecimals: 0, includeSum: true),
                newCredits.Col(x => x.ExampleCreditNr, ExcelType.Text, "Example"));
        }

        private static void SetNewCreditsLastMonthColumns(DocumentClientExcelRequest request, List<RATIReportDataWarehouseModel.CreditRatiData> lastMonthNewCredits)
        {
            request.Sheets[1].SetColumnsAndData(lastMonthNewCredits,
                lastMonthNewCredits.Col(x => x.InitialMaturity, ExcelType.Number, "Initial maturity", nrOfDecimals: 0),
                lastMonthNewCredits.Col(x => x.InitialCapitalDebt, ExcelType.Number, "Initial capital", nrOfDecimals: 2, includeSum: true),
                lastMonthNewCredits.Col(x => x.WeightedInitialInterestRate / 100m, ExcelType.Percent, "Initial nominal interest"),
                lastMonthNewCredits.Col(x => x.WeightedInitialEffectiveInterestRate / 100m, ExcelType.Percent, "Initial effective interest"),
                lastMonthNewCredits.Col(x => x.InterestDebt, ExcelType.Number, "Current interest debt", nrOfDecimals: 2, includeSum: true),

                //<--These are for testing so maybe only keep in test or have setting to turn on/off
                lastMonthNewCredits.Col(x => x.Count, ExcelType.Number, "Count", nrOfDecimals: 0, includeSum: true),
                lastMonthNewCredits.Col(x => x.ExampleCreditNr, ExcelType.Text, "Example"));
        }

        private static void SetRenegotiatedCreditsQuarterColumns(DocumentClientExcelRequest request, List<RATIReportDataWarehouseModel.RenegotiatedCreditsRatiData> renegotiatedCredits)
        {
            request.Sheets[2].SetColumnsAndData(renegotiatedCredits,
                 renegotiatedCredits.Col(x => x.Maturity, ExcelType.Number, "Maturity", nrOfDecimals: 0),
                 renegotiatedCredits.Col(x => x.CapitalDebt, ExcelType.Number, "Capital", nrOfDecimals: 2, includeSum: true),
                 renegotiatedCredits.Col(x => x.WeightedInterestRate / 100m, ExcelType.Percent, "Initial nominal interest"),
                 renegotiatedCredits.Col(x => x.WeightedEffectiveInterestRate / 100m, ExcelType.Percent, "Initial effective interest"),
                 renegotiatedCredits.Col(x => x.InterestDebt, ExcelType.Number, "Current interest debt", nrOfDecimals: 2, includeSum: true),

                 //<--These are for testing so maybe only keep in test or have setting to turn on/off
                 renegotiatedCredits.Col(x => x.Count, ExcelType.Number, "Count", nrOfDecimals: 0, includeSum: true),
                 renegotiatedCredits.Col(x => x.ExampleCreditNr, ExcelType.Text, "Example"));
        }

        private static void SetRenegotiatedCreditsLastMonthColumns(DocumentClientExcelRequest request, List<RATIReportDataWarehouseModel.RenegotiatedCreditsRatiData> lastMonthRenegotiatedCredits)
        {
            request.Sheets[3].SetColumnsAndData(lastMonthRenegotiatedCredits,
                  lastMonthRenegotiatedCredits.Col(x => x.Maturity, ExcelType.Number, "Maturity", nrOfDecimals: 0),
                  lastMonthRenegotiatedCredits.Col(x => x.CapitalDebt, ExcelType.Number, "Capital", nrOfDecimals: 2, includeSum: true),
                  lastMonthRenegotiatedCredits.Col(x => x.WeightedInterestRate / 100m, ExcelType.Percent, "Initial nominal interest"),
                  lastMonthRenegotiatedCredits.Col(x => x.WeightedEffectiveInterestRate / 100m, ExcelType.Percent, "Initial effective interest"),
                  lastMonthRenegotiatedCredits.Col(x => x.InterestDebt, ExcelType.Number, "Current interest debt", nrOfDecimals: 2, includeSum: true),

                  //<--These are for testing so maybe only keep in test or have setting to turn on/off
                  lastMonthRenegotiatedCredits.Col(x => x.Count, ExcelType.Number, "Count", nrOfDecimals: 0, includeSum: true),
                  lastMonthRenegotiatedCredits.Col(x => x.ExampleCreditNr, ExcelType.Text, "Example"));
        }

        private DocumentClientExcelRequest GetDocumentRequest(RATIReportDataWarehouseModel.RATIQuarter q)
        {
            return new DocumentClientExcelRequest
            {
                Sheets = new DocumentClientExcelRequest.Sheet[]
                        {
                            new DocumentClientExcelRequest.Sheet
                            {
                                AutoSizeColumns = true,
                                Title = $"New ({q.ToString()})"
                            },
                            new DocumentClientExcelRequest.Sheet
                            {
                                AutoSizeColumns = true,
                                Title = $"New ({q.LastMonthName})"
                            },
                            new DocumentClientExcelRequest.Sheet
                            {
                                AutoSizeColumns = true,
                                Title = $"Renegotiated"
                            },
                            new DocumentClientExcelRequest.Sheet
                            {
                                AutoSizeColumns = true,
                                Title = $"Renegotiated ({q.LastMonthName})"
                            },
                            new DocumentClientExcelRequest.Sheet
                            {
                                AutoSizeColumns = true,
                                Title = $"Portfolio"
                            },
                            new DocumentClientExcelRequest.Sheet
                            {
                                AutoSizeColumns = true,
                                Title = $"Raw Credits"
                            },
                            new DocumentClientExcelRequest.Sheet
                            {
                                AutoSizeColumns = true,
                                Title = $"Raw events"
                            },
                        }
            };
        }
    }
}