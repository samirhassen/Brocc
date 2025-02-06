using nDataWarehouse.Code.Clients;
using nDataWarehouse.DbModel;
using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;

namespace nDataWarehouse.Controllers
{
    public class DashboardController : NController
    {
        private T FetchFromService<T>(string serviceName, Func<Func<string>, T> f)
        {
            if (NEnv.ServiceRegistry.ContainsService(serviceName))
            {
                var unp = NEnv.AutomationUsernameAndPassword;
                if (unp != null)
                {
                    return f(() => NHttp.AquireSystemUserAccessTokenWithUsernamePassword(unp.Item1, unp.Item2, NEnv.ServiceRegistry.Internal.ServiceRootUri("nUser")));
                }
            }
            return default(T);
        }

        [AllowAnonymous]
        [Route("Dashboard-Data")]
        [HttpPost]
        [NTechApi]
        public ActionResult FetchData(DateTime? currentDate, bool? skipRealtimeData)
        {
            if (!NEnv.IsDashboardEnabled)
                return HttpNotFound();

            var forDate = currentDate ?? DateTime.Today;

            using (var repo = new DashboardDataRepository())
            {
                DashboardDataRepository.AggregateModel aggregates = null;
                PreCreditClient.AggregatesResponse preCreditData = null;
                if (!skipRealtimeData.GetValueOrDefault())
                {
                    aggregates = FetchFromService("nCredit", x => new CreditClient(x).FetchRealtimeAggregates());
                    preCreditData = FetchFromService("nPreCredit", x => new PreCreditClient(x).FetchRealtimeAggregates(forDate));
                }

                if (aggregates == null)
                    aggregates = repo.FetchAggregates();

                var balances = repo.FetchDailyBalances();
                var dailyApprovedAmount = (preCreditData?.ApprovedAmount) ?? repo.FetchApprovedApplicationAmount(currentDate ?? DateTime.Today).ApprovedAmount;

                int? startMonth;
                int? startYear;
                int[] budgets;

                ReadBudgetSettings(out startMonth, out startYear, out budgets);

                DateTime? budgetStartDate = startMonth.HasValue ? new DateTime?(new DateTime(startYear.Value, startMonth.Value, 1)) : null;

                return Json2(new
                {
                    dailyPaymentRecord = aggregates.MaxDailyPaidAmount,
                    dailyPaymentRecordDate = aggregates.MaxDailyPaidDate,
                    dailyApprovedApplicationsAmount = dailyApprovedAmount,
                    totalBalance = aggregates.CapitalBalance,
                    totalNrOfLoans = aggregates.ActiveLoanCount,
                    avgBalancePerLoan = Math.Round(aggregates.ActiveLoanCount == 0 ? 0 : (aggregates.CapitalBalance / (decimal)aggregates.ActiveLoanCount), 2),
                    avgInterestRatePerLoan = aggregates.AvgActiveLoanInterestRate,
                    balanceHistory = new
                    {
                        balancePerDay = balances.Select(x => (int)Math.Round(x.BalanceAmount)).ToList(),
                        labelPerDay = balances.Select(x => x.TheDate.ToString("yyyy-MM-dd"))
                    },
                    budget = !budgetStartDate.HasValue ? null : new
                    {
                        startDate = new DateTime(startYear.Value, startMonth.Value, 1).ToString("yyyy-MM-dd"),
                        currentDate = currentDate ?? DateTime.Today,
                        results = repo.FetchMonthlyPaidOutAmounts(budgetStartDate.Value).Select(x => x.Amount).ToArray(),
                        budgets = budgets,
                    }
                });
            }
        }

        private static void ReadBudgetSettings(out int? startMonth, out int? startYear, out int[] monthlyBudgets)
        {
            string startMonthSetting = GetSetting(AnalyticsSetting.SettingCodes.budgetVsResultStartMonth.ToString());
            string startYearSetting = GetSetting(AnalyticsSetting.SettingCodes.budgetVsResultStartYear.ToString());
            var budgetsSetting = GetSetting(AnalyticsSetting.SettingCodes.budgets.ToString());

            Func<string, int?> pInt = s => string.IsNullOrWhiteSpace(s) ? null : new int?(int.Parse(s));

            startMonth = pInt(startMonthSetting);
            startYear = pInt(startYearSetting);
            monthlyBudgets = budgetsSetting == null ? null : JsonConvert.DeserializeAnonymousType(budgetsSetting, new int[] { });
        }

        [AllowAnonymous]
        [Route("Dashboard")]
        public ActionResult Index(DateTime? currentDate)
        {
            if (!NEnv.IsDashboardEnabled)
                return HttpNotFound();

            ViewBag.JsonInitialData = this.EncodeInitialData(new
            {
                currentDate = (currentDate ?? DateTime.Now).ToString("yyyy-MM-dd"),
                chosenGraph = GetSetting(AnalyticsSetting.SettingCodes.chosenGraph.ToString()),
            });

            return View();
        }

        [Route("Dashboard/Settings")]
        public ActionResult Settings(DateTime? currentDate)
        {
            if (!NEnv.IsDashboardEnabled)
                return HttpNotFound();
            using (var context = new AnalyticsContext())
            {
                var budgetsSetting = GetSetting(AnalyticsSetting.SettingCodes.budgets.ToString());

                Func<string, int?> pInt = s => string.IsNullOrWhiteSpace(s) ? null : new int?(int.Parse(s));

                ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.GetEncoding("iso-8859-1").GetBytes(JsonConvert.SerializeObject(new
                {
                    currentDate = currentDate ?? DateTime.Today,
                    updateChosenGraphUrl = Url.Action("UpdateChosenGraph"),
                    chosenGraph = GetSetting(AnalyticsSetting.SettingCodes.chosenGraph.ToString()),
                    budgetGraph = new
                    {
                        chosenMonth = pInt(GetSetting(AnalyticsSetting.SettingCodes.budgetVsResultStartMonth.ToString())),
                        chosenYear = pInt(GetSetting(AnalyticsSetting.SettingCodes.budgetVsResultStartYear.ToString())),
                        budgets = budgetsSetting == null ? null : JsonConvert.DeserializeAnonymousType(budgetsSetting, new int[] { }),
                        updateBudgetVsResultStartUrl = Url.Action("UpdateBudgetVsResultStart"),
                    }
                })));
            }

            return View();
        }

        [Route("UpdateChosenGraph")]
        [HttpPost]
        public ActionResult UpdateChosenGraph(string name)
        {
            UpdateSetting(AnalyticsSetting.SettingCodes.chosenGraph.ToString(), name);
            return new EmptyResult();
        }

        [Route("UpdateBudgetVsResultStart")]
        [HttpPost]
        public ActionResult UpdateBudgetVsResultStart(int? startMonth, int? startYear, int[] budgets)
        {
            if (!startMonth.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing startMonth");
            if (!startYear.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing startYear");
            if (budgets == null || budgets.Length != 12)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing or invalid budgets");
            UpdateSetting(AnalyticsSetting.SettingCodes.budgetVsResultStartMonth.ToString(), startMonth.Value.ToString());
            UpdateSetting(AnalyticsSetting.SettingCodes.budgetVsResultStartYear.ToString(), startYear.Value.ToString());
            UpdateSetting(AnalyticsSetting.SettingCodes.budgets.ToString(), JsonConvert.SerializeObject(budgets));
            return new EmptyResult();
        }

        private static string GetSetting(string key)
        {
            using (var context = new AnalyticsContext())
            {
                AnalyticsSetting setting = context
                   .AnalyticsSettings
                   .Where(x => x.Key == key)
                   .SingleOrDefault();

                return setting?.Value;
            }
        }

        private void UpdateSetting(string key, string value)
        {
            using (var context = new AnalyticsContext())
            {
                AnalyticsSetting setting = context
                    .AnalyticsSettings
                    .Where(x => x.Key == key)
                    .SingleOrDefault();

                if (setting == null)
                {
                    setting = new AnalyticsSetting
                    {
                        Key = key,
                        Value = value
                    };
                    context.AnalyticsSettings.Add(setting);
                }
                else
                {
                    setting.Key = key;
                    setting.Value = value;
                }
                context.SaveChanges();
            }
        }
    }
}