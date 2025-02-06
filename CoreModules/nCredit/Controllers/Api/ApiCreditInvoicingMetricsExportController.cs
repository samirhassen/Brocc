using nCredit.Code;
using Newtonsoft.Json;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    [RoutePrefix("Api/CreditInvoicingMetricsExport")]
    [NTechAuthorizeCreditHigh(ValidateAccessToken = true)]
    public class ApiCreditInvoicingMetricsExportController : NController
    {
        [Route("Run")]
        [HttpPost()]
        public ActionResult RunCreditInvoicingMetricsExport(IDictionary<string, string> schedulerData = null)
        {
            Func<string, string> getSchedulerData = s => (schedulerData != null && schedulerData.ContainsKey(s)) ? schedulerData[s] : null;

            return CreditContext.RunWithExclusiveLock("ntech.scheduledjobs.creditinvoicingmetricsexport",
                    () => RunCreditInvoicingMetricsExportI(),
                    () => new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Job is already running"));
        }

        private static Lazy<NTechSelfRefreshingBearerToken> telemetryUser = new Lazy<NTechSelfRefreshingBearerToken>(() =>
            NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistryNormal, NEnv.ApplicationAutomationUsernameAndPassword));

        private ActionResult RunCreditInvoicingMetricsExportI()
        {
            List<string> errors = new List<string>();
            
            //Used by nScheduler
            var warnings = new List<string>();

            var w = Stopwatch.StartNew();
            try
            {
                var items = FetchCreditInvoicingMetricsData();

                var user = new LegacyHttpServiceBearerTokenUser(telemetryUser);
                var auditClient = LegacyServiceClientFactory.CreateAuditClient(user, NEnv.ServiceRegistry);

                auditClient.LogTelemetryData("InvoicingMetricsData", JsonConvert.SerializeObject(items));
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "CreditInvoicingMetricsExport crashed");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
            finally
            {
                w.Stop();
            }

            NLog.Information("CreditInvoicingMetricsExport finished TotalMilliseconds={totalMilliseconds}", w.ElapsedMilliseconds);

            errors?.ForEach(x => warnings.Add(x));

            return Json2(new { errors, totalMilliseconds = w.ElapsedMilliseconds, warnings = warnings });
        }

        private InvoicingMetricItem[] FetchCreditInvoicingMetricsData()
        {
            using (var context = new CreditContext())
            {
                var lastDayOfLastMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddDays(-1);

                var credits = context.CreditHeaders.Select(x => new
                {
                    x.CreditNr,
                    CurrentCreditStatus = x
                        .DatedCreditStrings
                        .Where(y => y.Name == "CreditStatus" && y.TransactionDate <= lastDayOfLastMonth)
                        .OrderByDescending(y => y.Id)
                        .Select(y => y.Value)
                        .FirstOrDefault(),
                    CurrentBalance = x
                        .Transactions
                        .Where(y => y.AccountCode == "CapitalDebt" && y.TransactionDate <= lastDayOfLastMonth)
                        .Sum(y => (decimal?)y.Amount) ?? 0m
                }).Where(x => x.CurrentCreditStatus == "Normal");

                decimal sumCapitalBalance = credits.Sum(x => x.CurrentBalance);

                return new InvoicingMetricItem[]
                {
                    new InvoicingMetricItem
                    {
                        Date = DateTimeOffset.Now,
                        TelemetryDate = DateTimeOffset.Now,
                        ClientName = NEnv.ClientCfg.ClientName,
                        Module = "nCredit",
                        Metric = "CreditVolume",
                        Value = sumCapitalBalance.ToString()
                    }
                };
            }
        }
        public class InvoicingMetricItem
        {
            public DateTimeOffset Date { get; set; }
            public DateTimeOffset TelemetryDate { get; set; }
            public string ClientName { get; set; }
            public string Module { get; set; }
            public string Metric { get; set; }
            public string Value { get; set; }
        }

    }



}




