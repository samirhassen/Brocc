using nCredit;
using nCredit.Code.Services;
using Newtonsoft.Json;
using NTech.Core.Host.IntegrationTests.MlStandard.Utilities;
using NTech.Core.Host.IntegrationTests.UlLegacy;

namespace NTech.Core.Host.IntegrationTests.MlStandard
{
    internal class UcCreditRegistryAcceptanceTests
    {
        /*
         This test is used to do the required AT testing when starting up
         a client with uc. Add a setup file connected to their AT environment
         with the clients username/password/creditorid and ask uc to verify.
         To preview what will be sent point it to something like:
         http://localhost:19727/Api/LoggedRequest/uc-creditregistry
         And look at the result at:
         http://localhost:19727/Ui/LoggedRequest

         */

        //[Test]
        public void SendTestFiles()
        {
            var testSetup = LoadTestSetup();
            if (testSetup == null)
            {
                Assert.Inconclusive("No test setup present. Skipping");
                return;
            }
            MlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                var ucService = new UcCreditRegistryService(() => testSetup.Settings, support.Clock, support.CreditEnvSettings.IsMortgageLoansEnabled,
                    () => TestPersons.CreateRealisticCustomerClient(support).Object,
                    () => new Banking.CivicRegNumbers.CivicRegNumberParser(support.ClientConfiguration.Country.BaseCountry),
                    support.CreateCreditContextFactory());

                support.Now = DateTime.Now.AddDays(-5).AddHours(-3);
                void MoveForwardOneDayAndFiveMinutes() => support.Now = support.Now.AddDays(1).AddMinutes(5);
                int testPersonSeed = 1;

                /*
                UC Desired test scheme
                The following needs to be tested in SHARK AT:
                - Lifecycle of 5-10 normal credits e.g. starting, changing, ending.
                - Above but with co-applicants. 
                - "Delete" function - a function that triggers when you put same start & end Date (we dont support this)
                - "noDeliveryToday" function
                - Transfer of a credit from 1 person to new SSN (we dont support this)
                - Change of accountnum, CreditType & if used refNum in the reporting by using the "prior" fields. (we dont support this)
                - If possible, the baseload that contains the active credit stock (will have to be tested separately if we have such clients)
                 */

                //"noDeliveryToday" function
                ucService.ReportCreditsChangedSinceLastReport(support.CurrentUser);

                //Lifecycle of 5 normal credits e.g. starting (with one applicant)
                MoveForwardOneDayAndFiveMinutes();
                const decimal OneMillion = 1000000m;
                var creditNrsWithLoanAmount = new Dictionary<string, decimal>();
                Enumerable.Range(1, 5).ToList().ForEach(creditIndex =>
                {
                    var mainApplicantCustomerId = TestPersons.EnsureTestPerson(support, testPersonSeed++);
                    var loanAmount = creditIndex * OneMillion;
                    var credit = CreditsMlStandard.CreateCredit(support, creditIndex,
                        mainApplicantCustomerId: mainApplicantCustomerId, skipCoApplicant: true,
                        loanAmount: loanAmount);
                    creditNrsWithLoanAmount[credit.CreditNr] = loanAmount;

                });
                ucService.ReportCreditsChangedSinceLastReport(support.CurrentUser);

                //Change 2, end 2
                MoveForwardOneDayAndFiveMinutes();
                var payments = creditNrsWithLoanAmount
                    .Keys.Take(2).Select(x => new { CreditNr = x, Amount = 100m })
                    .Concat(creditNrsWithLoanAmount.Keys.Skip(2).Take(2).Select(x => new { CreditNr = x, Amount = creditNrsWithLoanAmount[x] }))
                    .ToDictionary(x => x.CreditNr, x => x.Amount);
                Credits.CreateAndPlaceUnplacedPayments(support, payments);
                foreach(var settledCreditNr in creditNrsWithLoanAmount.Keys.Skip(2).Take(2).ToArray())
                {
                    Credits.AssertIsSettled(support, settledCreditNr);
                }
                ucService.ReportCreditsChangedSinceLastReport(support.CurrentUser);

                //Lifecycle of 5-10 normal credits e.g. starting (with co applicant)
                MoveForwardOneDayAndFiveMinutes();
                creditNrsWithLoanAmount = new Dictionary<string, decimal>();
                Enumerable.Range(6, 5).ToList().ForEach(creditIndex =>
                {
                    var mainApplicantCustomerId = TestPersons.EnsureTestPerson(support, testPersonSeed++);
                    var coApplicantCustomerId = TestPersons.EnsureTestPerson(support, testPersonSeed++);
                    var loanAmount = creditIndex * OneMillion;
                    var credit = CreditsMlStandard.CreateCredit(support, creditIndex,
                        mainApplicantCustomerId: mainApplicantCustomerId, skipCoApplicant: false,
                        loanAmount: loanAmount);
                    creditNrsWithLoanAmount[credit.CreditNr] = loanAmount;

                });
                ucService.ReportCreditsChangedSinceLastReport(support.CurrentUser);

                //Change 2, end 2
                MoveForwardOneDayAndFiveMinutes();
                payments = creditNrsWithLoanAmount
                    .Keys.Take(2).Select(x => new { CreditNr = x, Amount = 100m })
                    .Concat(creditNrsWithLoanAmount.Keys.Skip(2).Take(2).Select(x => new { CreditNr = x, Amount = creditNrsWithLoanAmount[x] }))
                    .ToDictionary(x => x.CreditNr, x => x.Amount);
                Credits.CreateAndPlaceUnplacedPayments(support, payments);
                foreach (var settledCreditNr in creditNrsWithLoanAmount.Keys.Skip(2).Take(2).ToArray())
                {
                    Credits.AssertIsSettled(support, settledCreditNr);
                }
                ucService.ReportCreditsChangedSinceLastReport(support.CurrentUser);
            });
        }

        private class TestSetup
        {
            public UcCreditRegistrySettingsModel? Settings { get; set; }
            public string? SharkEndpoint { get; set; }
        }

        private static TestSetup? LoadTestSetup()
        {
            const string FileName = @"C:\temp\96f6e60e-3f2a-4187-ad1f-aeb7af12492b\uc-creditregistry-integrationtest-setup.json";
            if (!File.Exists(FileName))
            {
                return null;
            }
            var result = JsonConvert.DeserializeObject<TestSetup>(File.ReadAllText(FileName));
            result!.Settings!.SharkEndpoint = new Uri(result.SharkEndpoint!);
            return result;
        }
        /*
         Example setup file:

            {
                "settings": {
                     "sharkUsername": "<someusername>",
                     "sharkPassword": "<somepassword>",
                     "sharkCreditorId": "Z9ZZ9",
                     "sharkSourceSystemId": "Magellan",
                     "sharkDeliveryUniqueId": "MortgageLoansSe",
                     "logFolder": "C:\\Naktergal\\Logs\\UcCreditRegistry"
                },    
                "sharkEndpointUcAt": "https://kreditregistret-at.uc.se",
                "sharkEndpoint": "http://localhost:19727/Api/LoggedRequest/uc-creditregistry"
            }

         */
    }
}
