using NTech.Banking.CivicRegNumbers;
using NTech.Banking.OrganisationNumbers;
using NTech.Core;
using NTech.Services.Infrastructure;
using nTest.Code;
using nTest.Code.Credit;
using nTest.RandomDataSource;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Hosting;
using System.Web.Mvc;

namespace nTest.Controllers
{
    [NTechApi]
    [RoutePrefix("Api/TestDriver/Credit")]
    public class CreditDriverController : NController
    {
        private class JobState
        {
            public ConcurrentQueue<string> CallLog { get; set; } = new ConcurrentQueue<string>();
            public bool IsComplete { get; set; }
            public bool IsException { get; set; }
            public Exception Exception { get; set; }
        }

        private static ConcurrentDictionary<string, JobState> jobs = new ConcurrentDictionary<string, JobState>();
        private static void RunAtLeastOnceButRepeatUntilStopDateReached(CreditDriver d, Action a)
        {
            int guard = 0;
            do { a(); } while (guard++ < 100 && d.HasFutureStopDate());
        }

        private static void DoSimulate(IRandomnessSource random, JobState state, string scenario, string scenarioData, Dictionary<string, string> outputDataContext, DateTime? stopAtDate)
        {
            try
            {
                var d = CreditDriver.Begin(random, logCall: x =>
                {
                    state.CallLog.Enqueue(x);
                    Debug.WriteLine(x);
                }, stopAtDate: stopAtDate);
                if (scenario == "OneMonth")
                {
                    RunAtLeastOnceButRepeatUntilStopDateReached(d, 
                        () => d.SimulateOneMonth());
                }
                else if (scenario == "OneMonthSimple" || scenario == "OneMonthSimpleSans")
                {
                    RunAtLeastOnceButRepeatUntilStopDateReached(d, 
                        () => d.SimulateOneMonthSimple(skipPayments: scenario == "OneMonthSimpleSans"));
                }
                else if (scenario.StartsWith("OneDay"))
                {
                    d.SimulateOneDay(skipPayments: scenario.Contains("Sans"), isSimple: scenario.Contains("Simple"));
                }
                else if (scenario == "OneYear")
                {
                    d.SimulateOneYear();
                }
                else if (scenario == "AddCustomApplication2")
                {
                    d.AddCustomApplication2(scenarioData, outputDataContext);
                }
                else if (scenario == "FlagCustomersAsExternallyOnboarded")
                {
                    d.FlagCustomersAsExternallyOnboarded(scenarioData, outputDataContext);
                }
                else if (scenario == "CreateMortgageLoan")
                {
                    d.CreateMortgageLoan(scenarioData, outputDataContext);
                }
                else if (scenario == "AddCustomSavingsApplication")
                {
                    d.CreateSavingsAccount(scenarioData, outputDataContext);
                }
                else if (scenario == "OneTimeCreateSavingsHistory")
                {
                    var g = new OnetimeSavingsHistoryGenerator();
                    g.Generate(state.CallLog.Enqueue);
                }
                else
                {
                    throw new Exception($"Invalid scenario={scenario}");
                }

                state.IsComplete = true;
            }
            catch (AggregateException ex)
            {
                state.CallLog.Enqueue($"Something crashed: {ex.Message}");
                state.CallLog.Enqueue(ex.StackTrace);
                foreach (var e in ex?.InnerExceptions)
                {
                    state.CallLog.Enqueue(e.Message);
                    state.CallLog.Enqueue(e.StackTrace);
                }
                state.IsComplete = true;
                state.IsException = true;
                state.Exception = ex;
            }
            catch (Exception ex)
            {
                state.IsComplete = true;
                state.IsException = true;
                state.Exception = ex;
                state.CallLog.Enqueue($"Something crashed: {ex.Message}");
                state.CallLog.Enqueue(ex.StackTrace);
            }
            finally
            {
                if (state != null)
                    state.IsComplete = true;
            }
            LogJob(state);
        }

        private void HandleReset(DateTime? resetStartDate)
        {
            if (resetStartDate.HasValue)
            {
                //Use this when the databases are reset right before to also reset the test system
                DbSingleton.DeleteDatabase();
                CreditDriver.MoveTimeTo(resetStartDate.Value, CreditDriver.TimeOfDay.Morning);
            }
        }

        [Route("Simulate")]
        [HttpPost]
        public ActionResult Simulate(int? seed, string scenario, DateTime? resetStartDate, string scenarioData, bool? returnCallLog, DateTime? stopAtDate)
        {
            HandleReset(resetStartDate);

            var state = new JobState();
            var r = new RandomnessSource(seed);
            var outputDataContext = new Dictionary<string, string>();
            DoSimulate(r, state, scenario, scenarioData, outputDataContext, stopAtDate);
            if (state.IsException)
            {
                var hex = state.Exception as System.Net.Http.HttpRequestException;
                if (hex != null && hex.Message.Contains("400"))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, state?.Exception?.Message);
                }
                else
                {
                    NLog.Error(state.Exception, "Simulate died with an exception");
                    return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, state?.Exception?.Message);
                }
            }
            else
            {
                return Json2(new { currentTime = TimeMachine.SharedInstance.GetCurrentTime(), callLog = (returnCallLog ?? false) ? state?.CallLog?.ToArray() : null, outputDataContext = (returnCallLog ?? false) ? outputDataContext : null });
            }
        }

        private static void LogJob(JobState s)
        {
            try
            {
                var logFolder = NEnv.LogFolder;
                if (logFolder == null)
                    return;

                var p = System.IO.Path.Combine(logFolder.FullName, @"Test\SimulationLogs");
                System.IO.Directory.CreateDirectory(p);
                System.IO.File.WriteAllLines(System.IO.Path.Combine(p, Guid.NewGuid().ToString() + ".txt"), s.CallLog.ToArray());
            }
            catch (Exception ex)
            {
                /* Ignored*/
                NLog.Warning("Failed to save simulation logs: {message}", ex.ToString());
            }
        }

        [Route("BeginSimulate")]
        [HttpPost]
        public ActionResult BeginSimulate(int? seed, string scenario, DateTime? resetStartDate, string scenarioData, DateTime? stopAtDate)
        {
            HandleReset(resetStartDate);

            var state = new JobState();
            var jobId = Guid.NewGuid().ToString();

            var r = new RandomnessSource(seed);
            HostingEnvironment.QueueBackgroundWorkItem(ct =>
                {
                    DoSimulate(r, state, scenario, scenarioData, null, stopAtDate);
                });
            jobs[jobId] = state;
            return Json2(new { currentTime = TimeMachine.SharedInstance.GetCurrentTime(), jobId = jobId });
        }

        [Route("CreateCustomSavingsApplicationJson")]
        [HttpPost]
        public ActionResult CreateCustomSavingsApplicationJson(int? seed, string applicant1CivicRegNr, bool? hasRemarks)
        {
            if (string.IsNullOrWhiteSpace(applicant1CivicRegNr))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing applicant 1 civic regnr");
            }
            var repo = new TestPersonRepository(NEnv.ClientCfg.Country.BaseCountry, DbSingleton.SharedInstance.Db);
            var applicant1 = repo.GetI(NEnv.ClientCfg.Country.BaseCountry, applicant1CivicRegNr);

            var r = new RandomnessSource(seed);

            var result = new TestSavingsAccountApplicationGenerator().CreateApplicationJson(applicant1, hasRemarks ?? false, r);

            return Json2(new
            {
                applicationJson = result
            });
        }

        [Route("CreateCustomApplicationJson")]
        [HttpPost]
        public ActionResult CreateCustomApplicationJson(int? seed, bool? isAccepted, string applicant1CivicRegNr, string applicant2CivicRegNr, bool? includeAdditionalQuestionFields, string providerName)
        {
            if (string.IsNullOrWhiteSpace(applicant1CivicRegNr))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing applicant 1 civic regnr");
            }
            var repo = new TestPersonRepository(NEnv.ClientCfg.Country.BaseCountry, DbSingleton.SharedInstance.Db);
            var applicant1 = repo.GetI(NEnv.ClientCfg.Country.BaseCountry, applicant1CivicRegNr);
            var applicant2 = string.IsNullOrWhiteSpace(applicant2CivicRegNr)
                ? null
                : repo.GetI(NEnv.ClientCfg.Country.BaseCountry, applicant2CivicRegNr);

            var r = new RandomnessSource(seed);

            providerName = providerName ?? NEnv.DefaultProviderName;
            string externalApplicationId = null;
            if (providerName != NEnv.DefaultProviderName)
            {
                externalApplicationId = r.NextIntBetween(10000, int.MaxValue - 1000).ToString();
            }

            var result = new TestApplicationGenerator().CreateApplicationJson(applicant1, applicant2, isAccepted ?? true, r, (providerName ?? NEnv.DefaultProviderName), includeAdditionalQuestionFields ?? false, externalApplicationId: externalApplicationId);

            return Json2(new
            {
                applicationJson = result
            });
        }

        [Route("PollSimulate")]
        [HttpPost]
        public ActionResult PollSimulate(string jobId, bool? normalResponseOnError = false)
        {
            if (!jobs.ContainsKey(jobId))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such job exists");
            }
            var s = jobs[jobId];
            var newEvents = new List<string>();
            string e;
            while (s.CallLog.TryDequeue(out e))
                newEvents.Add(e);

            if (s.IsComplete && s.IsException && !normalResponseOnError.GetValueOrDefault())
            {
                NLog.Error(s.Exception, "Error in simulate");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "See nAudit error logs for details");
            }
            else
            {
                return Json2(new
                {
                    currentTime = TimeMachine.SharedInstance.GetCurrentTime(),
                    isComplete = s.IsComplete,
                    isException = s.IsException,
                    newEvents = newEvents
                });
            }
        }

        [Route("BuySatFiCreditReport")]
        [HttpPost()]
        public ActionResult BuyCreditReport(string civicRegNr)
        {
            var civicRegNrParsed = new CivicRegNumberParser("FI").Parse(civicRegNr);

            var customerClient = new CustomerClient();
            var creditReportClient = new CreditReportClient();
            var customerId = customerClient.GetCustomerId(civicRegNrParsed);

            var result = creditReportClient.BuyCreditReport(civicRegNrParsed, customerId, new List<string> { "*" }, "SatFiCreditReport", true, true, new Dictionary<string, string>());

            if (result.CreditReportId.HasValue)
            {
                var tabledValues = creditReportClient.FetchTabledValues(result.CreditReportId.Value);
                return Json2(new
                {
                    Id = result.CreditReportId,
                    IsFromSat = true,
                    Items = tabledValues.Select(x => new { Name = x.Title, x.Value })
                });
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Failed");
            }
        }

        [Route("BuyCreditReport")]
        [HttpPost()]
        public ActionResult BuyCreditReport(string civicRegNr, string orgnr, string template)
        {
            if (NEnv.IsProduction)
                throw new Exception("Not allowed");

            var customerClient = new CustomerClient();
            var creditReportClient = new CreditReportClient();

            var additionalParameters = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(template))
                additionalParameters.Add("template", template);

            CreditReportClient.ReportResponse result;
            if (!string.IsNullOrWhiteSpace(civicRegNr))
            {
                var civicRegNrParsed = new CivicRegNumberParser(NEnv.ClientCfg.Country.BaseCountry).Parse(civicRegNr);
                var customerId = customerClient.GetCustomerId(civicRegNrParsed);
                string providerName;
                if (NEnv.ClientCfg.Country.BaseCountry == "SE")
                    providerName = "UcSe";
                else if (NEnv.IsUnsecuredLoansEnabled && NEnv.ClientCfg.Country.BaseCountry == "FI")
                    providerName = "BisnodeFi";
                else
                    throw new NotImplementedException();

                result = creditReportClient.BuyCreditReport(civicRegNrParsed, customerId, new List<string> { "*" }, providerName, true, true, additionalParameters);
            }
            else if (!string.IsNullOrWhiteSpace(orgnr))
            {
                var orgnrParsed = new OrganisationNumberParser(NEnv.ClientCfg.Country.BaseCountry).Parse(orgnr);
                var customerId = customerClient.GetCustomerId(orgnrParsed);
                string providerName;
                if (NEnv.ClientCfg.Country.BaseCountry == "SE")
                    providerName = "UcBusinessSe";
                else
                    throw new NotImplementedException();

                result = creditReportClient.BuyCompanyCreditReport(orgnrParsed, customerId, new List<string> { "*" }, providerName, true, true, additionalParameters);
            }
            else
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing orgnr and civicRegNr");

            if (result.CreditReportId.HasValue)
            {
                return Json2(new
                {
                    Id = result.CreditReportId,
                    Items = result.Items
                });
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Failed");
            }
        }

        [Route("ArchiveDocument")]
        [HttpGet()]
        public ActionResult ArchiveDocument(string key)
        {
            var c = new DocumentClient();
            string contentType;
            string filename;
            var b = c.FetchRawWithFilename(key, out contentType, out filename, allowHtml: true);
            var r = new FileStreamResult(new MemoryStream(b), contentType);
            r.FileDownloadName = filename;
            return r;
        }
    }
}