using Newtonsoft.Json;
using NTech.Core;
using nTest.Code;
using nTest.RandomDataSource;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace nTest.Controllers
{
    public class CreditDriver : ICoreClock
    {
        private DateTimeOffset currentTime;        
        private Action<string> logCall;
        private readonly IRandomnessSource random;
        private readonly DateTime? stopAtDate;

        protected void Log(string text)
        {
            logCall?.Invoke(text);
        }

        public enum TimeOfDay
        {
            Morning,
            Midday,
            Evening
        }


        public DateTimeOffset Now => currentTime;
        public DateTime Today => Now.Date;        

        private CreditDriver(IRandomnessSource random, Action<string> logCall = null, DateTime? stopAtDate = null)
        {
            currentTime = TimeMachine.SharedInstance.GetCurrentTime();
            this.logCall = logCall ?? (x => Debug.WriteLine(x));
            this.random = random;
            this.stopAtDate = stopAtDate;
        }

        private T CallWithLoggingF<T>(Func<T> f, string text)
        {
            var w = Stopwatch.StartNew();
            var t = currentTime.ToString("yyyy-MM-dd HH:mm");
            try
            {
                var result = f();
                logCall($"[{t}]{text} - OK - {w.ElapsedMilliseconds}ms");
                return result;
            }
            catch
            {
                logCall($"[{t}]{text} - FAILED - {w.ElapsedMilliseconds}ms");
                throw;
            }
        }

        private void CallWithLoggingA(Action a, string text)
        {
            CallWithLoggingF<object>(() => { a(); return null; }, text);
        }

        internal void SimulateOneDay(bool skipPayments = false, bool isSimple = false)
        {
            CallWithLoggingA(() =>
                {
                    var unpaidInvoices = GetAllUnpaidInvoices().Select(x => new Flaggable<InvoiceModel>(x)).ToList();
                    SimluateOneDayI(() => unpaidInvoices, false, false, skipPayments: skipPayments, isSimple: isSimple);
                }, "SimulateOneDay");
        }

        internal string AddCustomApplication2(string scenarioData, Dictionary<string, string> outputDataContext)
        {
            var data = JsonConvert.DeserializeAnonymousType(
                System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(scenarioData)),
                new { applicationJson = "" });

            var repo = new TestPersonRepository(NEnv.ClientCfg.Country.BaseCountry, DbSingleton.SharedInstance.Db);

            string createdApplicationNr = null;
            CallWithLoggingA(() =>
            {
                //NOTE: If you change automate to true here and the user changes the application json to remove DisableAutomation you will get concurrency issues
                var applicationNr = CreateApplication(null, false, TimeMachine.SharedInstance.GetCurrentTime().Date, customApplicationJson: data.applicationJson);
                if (outputDataContext != null)
                    outputDataContext["applicationNr"] = applicationNr;
                logCall($"ApplicationNr: {applicationNr}");
            }, "AddCustomApplication2");
            return createdApplicationNr;
        }

        internal void CreateMortgageLoan(string scenarioData, Dictionary<string, string> outputDataContext)
        {
            var data = JsonConvert.DeserializeAnonymousType(
               System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(scenarioData)),
               new { applicationNr = "" });

            CallWithLoggingA(() =>
            {
                var c = new CreditDriverPreCreditClient();
                c.CreateMortgageLoan(data.applicationNr);
            }, "CreateMortgageLoan");
        }

        internal void FlagCustomersAsExternallyOnboarded(string scenarioData, Dictionary<string, string> outputDataContext)
        {
            var data = JsonConvert.DeserializeAnonymousType(
               System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(scenarioData)),
               new { applicationNr = "" });

            CallWithLoggingA(() =>
            {
                var c = new CreditDriverPreCreditClient();
                c.FlagCustomersAsExternallyOnboarded(data.applicationNr);
                logCall($"FlagCustomersAsExternallyOnboarded: {data.applicationNr}");
            }, "FlagCustomersAsExternallyOnboarded");
        }

        internal void SimulateOneYear()
        {
            CallWithLoggingA(() =>
            {
                Repeat(c => SimulateOneMonthI(true, c.IsLast), 12);
            }, "SimulateOneYear");
        }

        public void SimulateOneMonth()
        {
            CallWithLoggingA(() => SimulateOneMonthI(false, true), "SimulateOneMonth");
        }

        public void SimulateOneMonthSimple(bool skipPayments = false)
        {
            CallWithLoggingA(() => SimulateOneMonthI(true, true, isSimple: true, skipPayments: skipPayments), "SimulateOneMonthSimple");
        }

        private class RepeatContext
        {
            public bool IsFirst { get; set; }
            public bool IsLast { get; set; }
        }

        private void Repeat(Action<RepeatContext> a, int nrOfTimes)
        {
            var c = new RepeatContext();
            for (var i = 0; i < nrOfTimes; ++i)
            {
                c.IsFirst = i == 0;
                c.IsLast = i == (nrOfTimes - 1);
                a(c);
            }
        }

        private static Lazy<NTech.Banking.BankAccounts.Se.BankGiroNumberSe> randomSwedishBankgiroNr = new Lazy<NTech.Banking.BankAccounts.Se.BankGiroNumberSe>(() =>
        {
            var b = new BankAccountGenerator();

            return b.GenerateSwedishBankGiroNr(new RandomnessSource(null)) as NTech.Banking.BankAccounts.Se.BankGiroNumberSe;
        });

        private void AddSomeIncomingPayments(List<Flaggable<InvoiceModel>> unpaidInvoices)
        {
            var payments = new List<Tuple<decimal, string>>();
            
            var today = Today;
            foreach (var creditGroup in unpaidInvoices.Where(x => !x.IsFlagged).GroupBy(x => x.Item.CreditNr).ToList())
            {
                if(creditGroup.Any(x => x.Item.DueDate == today))
                {
                    foreach(var notificationToPay in creditGroup)
                    {
                        payments.Add(Tuple.Create(notificationToPay.Item.UnpaidAmount, notificationToPay.Item.OcrPaymentReference));
                        notificationToPay.IsFlagged = true;
                    }
                }
            }

            if (payments.Any())
            {
                var pf = new TestPaymentFileCreator();
                string formatName;
                string fileName;
                string dataUrl;
                if (NEnv.ClientCfg.Country.BaseCountry == "FI")
                {
                    dataUrl = pf.ToDataUrl(pf.Create_Camt_054_001_02File(payments, bookkeepingDate: today));
                    formatName = "camt.054.001.02";
                    fileName = $"nTestIncomingPaymentFile_{currentTime.ToString("yyyy-MM-dd")}_{Guid.NewGuid().ToString()}.xml";
                }
                else if (NEnv.ClientCfg.Country.BaseCountry == "SE")
                {
                    dataUrl = pf.ToDataUrl(pf.Create_BgMax_File(currentTime.DateTime, payments.Select(x => new TestPaymentFileCreator.Payment
                    {
                        Amount = x.Item1,
                        BookkeepingDate = today,
                        OcrReference = x.Item2,
                        PayerName = "N " + x.Item2
                    }).ToList(), clientBankGiroNr: randomSwedishBankgiroNr.Value.NormalizedValue));
                    formatName = "bgmax";
                    fileName = $"nTestIncomingPaymentFile_{currentTime.ToString("yyyy-MM-dd")}_{Guid.NewGuid().ToString()}.txt";
                }
                else
                    throw new NotImplementedException();

                var creditClient = new CreditDriverCreditClient();
                CallWithLoggingA(
                    () => creditClient.ImportPaymentFile(
                        formatName,
                        fileName,
                        dataUrl,
                        true),
                    $"Importing payments");
            }
        }

        private void Init()
        {
            if (NEnv.ServiceRegistry.ContainsService("nCredit"))
            {
                var creditClient = new CreditDriverCreditClient();
                var maxCreditTransactionDate = creditClient.GetMaxTransactionDate();

                if (maxCreditTransactionDate.HasValue && TimeMachine.SharedInstance.GetCurrentTime().Date.Date < maxCreditTransactionDate.Value.Date)
                {
                    NLog.Warning("CreditDriver.Init moved time forward to the latest credit transaction");
                    MoveTime(maxCreditTransactionDate.Value.AddDays(1), TimeOfDay.Morning);
                }
            }
        }

        public static CreditDriver Begin(IRandomnessSource random, Action<string> logCall = null, DateTime? stopAtDate = null)
        {
            var d = new CreditDriver(random, logCall: logCall, stopAtDate: stopAtDate);
            d.Init();
            return d;
        }

        private static bool IsLastDayOfMonth(DateTimeOffset d)
        {
            var dd = d.Date.Date;
            return dd.AddDays(1).Month != dd.Month;
        }

        internal string CreateSavingsAccount(string scenarioData, Dictionary<string, string> outputDataContext)
        {
            var data = JsonConvert.DeserializeAnonymousType(
                System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(scenarioData)),
                new { applicationJson = "" });

            var repo = new TestPersonRepository(NEnv.ClientCfg.Country.BaseCountry, DbSingleton.SharedInstance.Db);

            string createdApplicationNr = null;
            CallWithLoggingA(() =>
            {
                var s = new SavingsDriverSavingsClient();
                var savingsAccountNr = s.CreateSavingsAccount(data.applicationJson)?.SavingsAccountNr;
                if (outputDataContext != null)
                    outputDataContext["savingsAccountNr"] = savingsAccountNr;
                logCall($"savingsAccountNr: {savingsAccountNr}");
            }, "CreateSavingsAccount");
            return createdApplicationNr;
        }

        //simple means nothing "new" is added. So no new credits or applications.
        private void SimluateOneDayI(Func<List<Flaggable<InvoiceModel>>> getUnpaidInvoices, bool isPartOfLongRun, bool isLastDayOfRun, bool isSimple = false, bool skipPayments = false)
        {
            MoveTime(currentTime, TimeOfDay.Morning);

            RunMorningSchedule(isPartOfLongRun, isLastDayOfRun);
            if (currentTime.Day == 5 && !isSimple && !NEnv.IsMortgageLoansEnabled && !NEnv.IsStandardUnsecuredLoansEnabled)
            {
                //TODO: This should be an option and be related to the dates of termination letters and not run if scheduled
                CallWithLoggingA(() => SendAllEligableToDebtCollection(), "Send to debt collection");
            }

            MoveTime(currentTime, TimeOfDay.Midday);

            //New approved applications
            if (!isSimple && !NEnv.IsStandardUnsecuredLoansEnabled)
            {
                var nrApproved = random.NextIntBetween(1, 3);
                var nrRejected = random.NextIntBetween(0, 3);
                Repeat(c => CallWithLoggingA(() => CreateApplication(true, true, currentTime.Date), "Creating approved application"), nrApproved);
                Repeat(c => CallWithLoggingA(() => CreateApplication(false, true, currentTime.Date), "Creating rejected application"), nrRejected);

                if (nrApproved > 0)
                {
                    CallWithLoggingA(() =>
                    {
                        var c = new CreditDriverPreCreditClient();
                        c.CreateCredits();
                    }, "Creating credits");
                }
            }

            if (!NEnv.IsMortgageLoansEnabled)
            {
                //Daily outbound payments
                CallWithLoggingA(() =>
                {
                    var c = new CreditDriverCreditClient();
                    c.CreateOutgoingPaymentFile();
                }, "Creating outgoing payment file");
            }

            //Daily inbound payments
            if (!skipPayments)
            {
                var unpaidInvoices = getUnpaidInvoices();

                AddSomeIncomingPayments(unpaidInvoices);
            }

            MoveTime(currentTime, TimeOfDay.Evening);
            RunEveningSchedule(isPartOfLongRun, isLastDayOfRun);

            MoveTime(currentTime.AddDays(1), TimeOfDay.Morning);
        }

        public bool HasFutureStopDate() => stopAtDate.HasValue && currentTime.Date.Date < stopAtDate.Value;

        private void SimulateOneMonthI(bool isPartOfLongRun, bool isLastMonthOfRun, bool isSimple = false, bool skipPayments = false)
        {
            MoveTime(currentTime.AddDays(1), TimeOfDay.Morning);

            var unpaidInvoices = GetAllUnpaidInvoices().Select(x => new Flaggable<InvoiceModel>(x)).ToList();
            var m = currentTime.Month;
            while (currentTime.Month == m)
            {
                if (stopAtDate.HasValue && currentTime.Date.Date >= stopAtDate.Value)
                    return;
                
                var isLastDayOfMonth = IsLastDayOfMonth(currentTime);
                var date = currentTime.Date;
                SimluateOneDayI(() => unpaidInvoices, isPartOfLongRun, isLastMonthOfRun && isLastDayOfMonth, isSimple: isSimple, skipPayments: skipPayments);
                var newInvoices = GetAllUnpaidInvoices(notificationDate: date).Select(x => new Flaggable<InvoiceModel>(x)).ToList();
                if(newInvoices.Count > 0)
                {
                    Log($"{newInvoices.Count} invoices created");
                    unpaidInvoices.AddRange(newInvoices);
                }
            }
        }

        /// <summary>
        /// Synchronously set the current time in nCredit
        /// </summary>
        /// <param name="currentTime"></param>
        private void MoveTime(DateTimeOffset newTime, TimeOfDay td)
        {
            currentTime = MoveTimeTo(newTime, td);
        }

        //TODO: Move away from here
        public static DateTimeOffset MoveTimeTo(DateTimeOffset newTime, TimeOfDay td)
        {
            Func<DateTimeOffset> withTimeOfDay = () =>
            {
                switch (td)
                {
                    case TimeOfDay.Morning: return new DateTimeOffset(newTime.Year, newTime.Month, newTime.Day, 6, 0, 0, 0, newTime.Offset);
                    case TimeOfDay.Midday: return new DateTimeOffset(newTime.Year, newTime.Month, newTime.Day, 12, 0, 0, 0, newTime.Offset);
                    case TimeOfDay.Evening: return new DateTimeOffset(newTime.Year, newTime.Month, newTime.Day, 18, 0, 0, 0, newTime.Offset);
                    default: throw new NotImplementedException();
                }
            };
            var t = TimeMachine.SharedInstance.SetTime(withTimeOfDay(), true);
            var c = new CreditDriverCreditClient();
            c.SetTime(t);
            return t;
        }

        private class Flaggable<T>
        {
            public Flaggable(T item)
            {
                Item = item;
            }

            public T Item { get; set; }
            public bool IsFlagged { get; set; }

            public static Flaggable<U> Create<U>(U item)
            {
                return new Flaggable<U>(item);
            }
        }

        private IList<InvoiceModel> GetAllUnpaidInvoices(DateTime? notificationDate = null)
        {
            var c = new CreditDriverCreditClient();
            return c.GetAllUnpaidNotifications(notificationDate);
        }

        private static IDictionary<string, string> CreateSchedulerData(bool isPartOfLongRun, bool isLastDayOfMonth, bool isLastDayOfRun)
        {
            var d = new Dictionary<string, string>();
            var skippedJobNames = new HashSet<string>();

            //The test trigger doesnt handle super slow jobs well so we disable this job until that is solved.
            skippedJobNames.Add("CreateDailyScheduledExcelExports");
            skippedJobNames.Add("ArchiveOldApplications");
            skippedJobNames.Add("UcCreditRegistryReportChanges");

            if (isPartOfLongRun)
            {
                if (!NEnv.HasPerLoanDueDay)
                {
                    d["skipDeliveryExport"] = "true";
                }

                d["useDelayedDocuments"] = "true";

                if (isPartOfLongRun && !isLastDayOfMonth)
                {
                    //Only run these once a month instead of daily. Seems like a reasonable compromise of still testing it a bit without making the testrun take forever
                    skippedJobNames.Add("KycScreenActiveCreditCustomers");
                    skippedJobNames.Add("KycScreenActiveSavingsCustomers");
                    skippedJobNames.Add("CreateSatExport");
                    skippedJobNames.Add("CreateSavingsTrapetsAmlExport");
                    skippedJobNames.Add("CreateCreditTrapetsAmlExport");
                    skippedJobNames.Add("CreditBookkeeping");
                    skippedJobNames.Add("SavingsBookkeeping");
                    skippedJobNames.Add("CreditPeriodicMaintenance");
                    skippedJobNames.Add("PreCreditPeriodicMaintenance");
                    skippedJobNames.Add("CustomerPeriodicMaintenance");
                }

                if (isPartOfLongRun)
                {
                    //These jobs are just too slow, never run these when generating testdata. Only add things here when there are no other options since these might break and never be tested.
                    skippedJobNames.Add("CreateDailyScheduledExcelExports");
                }

                if (isPartOfLongRun && !isLastDayOfRun)
                {
                    //Only update this once when the long run is done
                    skippedJobNames.Add("UpdateDataWarehousePreCredit");
                    skippedJobNames.Add("UpdateDataWarehouseCredit");
                    skippedJobNames.Add("UpdateDataWarehouseCreditReport");
                }
            }

            if (skippedJobNames.Any())
            {
                d.Add("skippedJobNames", JsonConvert.SerializeObject(skippedJobNames.ToList()));
            }

            return d;
        }

        public void RunMorningSchedule(bool isPartOfLongRun, bool isLastDayOfRun)
        {
            var schedulerData = CreateSchedulerData(isPartOfLongRun, IsLastDayOfMonth(currentTime), isLastDayOfRun);
            CallWithLoggingA(() => new CreditDriverSchedulerClient().TriggerTimeslot("Morning", schedulerData: schedulerData), "RunMorningSchedule");
        }

        public void RunEveningSchedule(bool isPartOfLongRun, bool isLastDayOfRun)
        {
            var schedulerData = CreateSchedulerData(isPartOfLongRun, IsLastDayOfMonth(currentTime), isLastDayOfRun);
            CallWithLoggingA(() => new CreditDriverSchedulerClient().TriggerTimeslot("Evening", schedulerData: schedulerData), "RunEveningSchedule");
        }

        private void SendAllEligableToDebtCollection()
        {
            var c = new CreditDriverCreditClient();
            c.SendAllEligableToDebtCollection();
        }

        public string CreateApplication(
            bool? isAccepted,
            bool automate,
            DateTime now,
            Tuple<string, IDictionary<string, string>, IDictionary<string, string>> customApplicationJsonAndPerson = null,
            string customApplicationJson = null)
        {
            var repo = new TestPersonRepository(NEnv.ClientCfg.Country.BaseCountry, DbSingleton.SharedInstance.Db);

            string appJson;
            Func<StoredPerson> getPerson = null;

            if (customApplicationJsonAndPerson != null)
            {
                appJson = customApplicationJsonAndPerson.Item1;
                var person = repo.AddOrUpdate(customApplicationJsonAndPerson.Item2, true);
                getPerson = () => person;
                if (customApplicationJsonAndPerson.Item3 != null)
                    repo.AddOrUpdate(customApplicationJsonAndPerson.Item3, false);
            }
            else if (customApplicationJson != null)
            {
                appJson = customApplicationJson;
                getPerson = () =>
                {
                    var a = JsonConvert.DeserializeAnonymousType(appJson, new { Items = new[] { new { Name = "", Group = "", Value = "" } } });
                    var civicRegNr = a.Items.Where(x => x.Group == "applicant1" && x.Name == "civicRegNr").Single().Value;
                    return repo.GetI(NEnv.ClientCfg.Country.BaseCountry, civicRegNr);
                };
            }
            else
            {
                var person = repo.GenerateNewTestPerson(isAccepted.Value, random, now);
                getPerson = () => person;
                appJson = new TestApplicationGenerator().CreateApplicationJson(person, null, isAccepted.Value, random, NEnv.DefaultProviderName, false);
            }

            var c = new CreditDriverPreCreditClient();
            var applicationNr = c.CreateApplication(appJson);

            if (automate)
            {
                c.CreditCheckAutomatic(applicationNr);
                if (isAccepted.GetValueOrDefault())
                {
                    var p = getPerson?.Invoke();
                    if (p == null)
                        throw new Exception($"Cannot automate except without an existing person: {applicationNr}");
                    if (p.CivicRegNrTwoLetterCountryIsoCode == "FI")
                    {
                        c.AddIbanToApplication(applicationNr, p.GetProperty("iban"));
                    }
                    else
                    {
                        c.AddBankAccountNrToApplication(applicationNr, p.GetProperty("bankAccountNr"));
                    }

                    var archiveKey = c.CreateAgreementPdfInArchive(applicationNr);
                    c.AddSignedAgreement(applicationNr, 1, archiveKey);
                    c.SignalExternalFraudCheckWasDone(applicationNr);
                    c.ApproveApplication(applicationNr);
                }
            }

            return applicationNr;
        }
    }
}