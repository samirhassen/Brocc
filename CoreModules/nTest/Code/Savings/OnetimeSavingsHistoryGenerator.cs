using Newtonsoft.Json;
using nTest.Controllers;
using nTest.RandomDataSource;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace nTest.Code
{
    public class OnetimeSavingsHistoryGenerator
    {
        public void Generate(Action<string> log)
        {
            log("Starting");
            var c = new SavingsDriverSavingsClient();
            var scheduler = new CreditDriverSchedulerClient();
            Func<bool, Dictionary<string, string>> schedulerData = isLastDayOfMonth =>
            {
                var skippedJobNames = new List<string>();
                skippedJobNames.Add("KycScreenActiveSavingsCustomers");
                if (!isLastDayOfMonth)
                {
                    skippedJobNames.Add("CreateSavingsTrapetsAmlExport");
                    skippedJobNames.Add("SavingsBookkeeping");
                    skippedJobNames.Add("SavingsPeriodicMaintenance");
                }

                return new Dictionary<string, string>
                {
                    { "onlyRunForTheseServiceNames", "nSavings" },
                    { "skippedJobNames", JsonConvert.SerializeObject(skippedJobNames) }
                };
            };

            var currentSystemTime = TimeMachine.SharedInstance.GetCurrentTime();

            var currentSavingsDate = currentSystemTime.AddDays(-500);
            c.SetTime(currentSavingsDate);
            try
            {
                //Set an interest rate
                c.ChangeInterestRate("StandardAccount", currentSavingsDate.Date.AddDays(1), 0.43m);

                //Move time so the new rate is valid
                currentSavingsDate = currentSavingsDate.AddDays(2);
                c.SetTime(currentSavingsDate);

                //Generate history
                var r = new RandomnessSource(31958546); //Any number, just to make it deterministic
                var g = new TestSavingsAccountApplicationGenerator();
                var p = new TestPersonRepository(NEnv.ClientCfg.Country.BaseCountry, DbSingleton.SharedInstance.Db);
                var pf = new TestPaymentFileCreator();

                var accounts = new List<AccountTemplate>();
                int dayCount = 0;
                while (currentSavingsDate < currentSystemTime)
                {
                    if (dayCount > 0 && (dayCount % 5 == 0))
                    {
                        log($"Days done: {dayCount} of ~500");
                    }

                    dayCount++;

                    var isLastDayOfMonth = currentSavingsDate.Month != currentSavingsDate.AddDays(1).Month;

                    //Morning
                    c.SetTime(new DateTimeOffset(currentSavingsDate.Year, currentSavingsDate.Month,
                        currentSavingsDate.Day, 6, 0, 0, currentSavingsDate.Offset));

                    scheduler.TriggerTimeslot("Morning", schedulerData: schedulerData(isLastDayOfMonth));

                    //Midday
                    c.SetTime(new DateTimeOffset(currentSavingsDate.Year, currentSavingsDate.Month,
                        currentSavingsDate.Day, 12, 0, 0, currentSavingsDate.Offset));

                    var newCount = r.NextIntBetween(0, 4);
                    if (newCount > 0)
                    {
                        var newAccounts = Enumerable.Range(1, newCount).ToList()
                            .Select(x => GenerateAccountTemplate(currentSavingsDate, r, p)).ToList();
                        foreach (var newAccount in newAccounts)
                        {
                            try
                            {
                                var cr = c.CreateSavingsAccount(g.CreateApplicationJson(newAccount.Owner, false, r,
                                    generateIban: true));
                                newAccount.SavingsAccountNr = cr.SavingsAccountNr;
                                newAccount.InitialStatus = cr.Status;
                                newAccount.OcrPaymentReference = cr.OcrPaymentReference;
                                accounts.Add(newAccount);
                            }
                            catch (Exception ex)
                            {
                                log(
                                    $"Failed to create account: {ex.ToString()}. Template: {JsonConvert.SerializeObject(newAccount)}");
                            }

                            //TODO: Deal with frozen and remove the block from withdrawals/deposits
                        }
                    }

                    //Deposits
                    var deposits = new List<TestPaymentFileCreator.Payment>();
                    foreach (var a in accounts.Where(x =>
                                 !x.WasCreatedFrozen() && x.Deposits.ContainsKey(currentSavingsDate.Date)))
                    {
                        var depositAmount = a.Deposits[currentSavingsDate.Date];
                        deposits.Add(new TestPaymentFileCreator.Payment
                        {
                            Amount = depositAmount,
                            BookkeepingDate =
                                currentSavingsDate.Date.AddDays(r.ShouldHappenWithProbability(0.5m) ? -1 : 0),
                            OcrReference = a.OcrPaymentReference,
                            PayerName = a.Owner.Properties["firstName"] + " " + a.Owner.Properties["lastName"]
                        });
                    }

                    if (deposits.Any())
                    {
                        var paymentFileAsDataUrl = pf.ToDataUrl(pf.Create_Camt_054_001_02File(deposits));
                        c.ImportPaymentFile("camt.054.001.02",
                            $"nTestIncomingPaymentFile_{currentSavingsDate.ToString("yyyy-MM-dd")}_{Guid.NewGuid().ToString()}.xml",
                            paymentFileAsDataUrl, true);
                    }

                    //Withdrawals
                    foreach (var a in accounts.Where(x =>
                                 !x.WasCreatedFrozen() && x.Withdrawals.ContainsKey(currentSavingsDate.Date)))
                    {
                        var withdrawalAmount = a.Withdrawals[currentSavingsDate.Date];
                        c.NewWithdrawal(a.SavingsAccountNr, withdrawalAmount);
                    }

                    if (r.ShouldHappenWithProbability(0.02m))
                    {
                        var newRate = Math.Round(r.NextDecimal(0.1m, 10m), 2);
                        log($"Interest rate changed to: {newRate.ToString(CultureInfo.InvariantCulture)}");
                        c.ChangeInterestRate("StandardAccount", currentSavingsDate.Date.AddDays(1), newRate);
                    }

                    c.SetTime(new DateTimeOffset(currentSavingsDate.Year, currentSavingsDate.Month,
                        currentSavingsDate.Day, 18, 0, 0, currentSavingsDate.Offset));
                    scheduler.TriggerTimeslot("Evening", schedulerData: schedulerData(isLastDayOfMonth));

                    currentSavingsDate = currentSavingsDate.AddDays(1);
                    c.SetTime(currentSavingsDate);
                }
            }
            finally
            {
                c.SetTime(currentSystemTime);
            }
        }

        private AccountTemplate GenerateAccountTemplate(DateTimeOffset currentDate, IRandomnessSource r,
            TestPersonRepository p)
        {
            var t = new AccountTemplate
            {
                CreationDate = currentDate.Date,
                Owner = p.GenerateNewTestPerson(true, r, currentDate.Date, reuseExisting: true),
                ClosedDate = currentDate.Date.AddDays(r.NextIntBetween(0, 600)),
                Deposits = new Dictionary<DateTime, decimal>(),
                Withdrawals = new Dictionary<DateTime, decimal>()
            };

            var d = t.CreationDate.AddDays(2);
            var balance = 0m;
            while (d <= t.ClosedDate)
            {
                if (r.ShouldHappenWithProbability(0.5m))
                {
                    //Deposit
                    var amount = Math.Round(r.NextDecimal(30, 10000));
                    if (amount + balance < 100000)
                    {
                        t.Deposits[d] = amount;
                        balance += amount;
                    }
                }
                else if (balance > 10)
                {
                    //Withdraw
                    var amount = Math.Round(r.NextDecimal(10, balance));
                    t.Withdrawals[d] = amount;
                    balance -= amount;
                }

                d = d.AddDays(r.NextIntBetween(3,
                    90)); //Dont make the 3 shorter unless you want to handle a bunch of date stuff
            }

            return t;
        }

        private class AccountTemplate
        {
            public string SavingsAccountNr { get; set; }
            public string OcrPaymentReference { get; set; }
            public string InitialStatus { get; set; }
            public StoredPerson Owner { get; set; }
            public DateTime CreationDate { get; set; }
            public DateTime? ClosedDate { get; set; }
            public IDictionary<DateTime, decimal> Deposits { get; set; }
            public IDictionary<DateTime, decimal> Withdrawals { get; set; }

            public bool WasCreatedFrozen()
            {
                return InitialStatus != "Active";
            }
        }
    }
}