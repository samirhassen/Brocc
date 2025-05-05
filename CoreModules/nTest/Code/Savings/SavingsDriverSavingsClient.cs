using Duende.IdentityModel.Client;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace nTest.Controllers
{
    public class SavingsDriverSavingsClient
    {
        private HttpClient CreateClient()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(NEnv.ServiceRegistry.Internal["nSavings"]);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("X-Ntech-TimetravelTo", TimeMachine.SharedInstance.GetCurrentTime().ToString("o"));
            client.SetBearerToken(NEnv.AutomationBearerToken());
            client.Timeout = TimeSpan.FromMinutes(30);
            return client;
        }

        public class CreateApplicationResult
        {
            public string SavingsAccountNr { get; set; }
            public string Status { get; set; }
            public string OcrPaymentReference { get; set; }
        }

        public CreateApplicationResult CreateSavingsAccount(string json)
        {
            using (var c = CreateClient())
            {
                var r = c.PostAsync("Api/SavingsAccount/Create", new StringContent(json, Encoding.UTF8, "application/json")).Result;
                r.EnsureSuccessStatusCode();
                return r.Content.ReadAsAsync<CreateApplicationResult>().Result;
            }
        }

        public class SavingsAccountDetailsResult
        {
            public DetailsResult Details { get; set; }
            public class DetailsResult
            {
                public string OcrDepositReference { get; set; }
                public int MainCustomerId { get; set; }
            }
        }

        public SavingsAccountDetailsResult GetSavingsAccountDetails(string savingsAccountNr)
        {
            using (var c = CreateClient())
            {
                var r = c.PostAsJsonAsync("Api/SavingsAccount/Details", new { savingsAccountNr = savingsAccountNr }).Result;
                r.EnsureSuccessStatusCode();
                return r.Content.ReadAsAsync<SavingsAccountDetailsResult>()?.Result;
            }
        }

        public void SetTime(DateTimeOffset time)
        {
            using (var client = CreateClient())
            {
                var response = client.PostAsJsonAsync("Api/Common/TimeTravel", new
                {
                    date = time.Date,
                    time = time.ToString("HH:mm")
                }).Result;
                response.EnsureSuccessStatusCode();
            }
        }

        public void ChangeInterestRate(string savingsAccountTypeCode, DateTime? validFromDate, decimal? interestRatePercent)
        {
            using (var client = CreateClient())
            {
                var response = client.PostAsJsonAsync("Api/InterestRateChange/DirectlyChangeInterestRate", new
                {
                    newInterestRatePercent = interestRatePercent,
                    allAccountsValidFromDate = validFromDate?.ToString("yyyy-MM-dd"),
                    newAccountsValidFromDate = (string)null
                }).Result;
                response.EnsureSuccessStatusCode();
            }
        }

        public void ImportPaymentFile(string fileFormatName, string fileName, string fileAsDataUrl, bool overrideIbanCheck)
        {
            using (var client = CreateClient())
            {
                var response = client.PostAsJsonAsync("Api/IncomingPayments/ImportFile", new
                {
                    fileFormatName = fileFormatName,
                    fileName = fileName,
                    fileAsDataUrl = fileAsDataUrl,
                    overrideIbanCheck = overrideIbanCheck
                }).Result;
                response.EnsureSuccessStatusCode();
            }
        }

        private class WithdrawalInitialDataResult
        {
            public string UniqueOperationToken { get; set; }
            public decimal WithdrawableBalance { get; set; }
        }

        public void NewWithdrawal(string savingsAccountNr, decimal amount)
        {
            using (var client = CreateClient())
            {
                var r1 = client.PostAsJsonAsync("Api/SavingsAccount/WithdrawalInitialData", new
                {
                    savingsAccountNr = savingsAccountNr
                }).Result;
                r1.EnsureSuccessStatusCode();
                var rr1 = r1.Content.ReadAsAsync<WithdrawalInitialDataResult>()?.Result;

                var r = client.PostAsJsonAsync("Api/SavingsAccount/NewWithdrawal", new
                {
                    savingsAccountNr = savingsAccountNr,
                    amount = amount,
                    uniqueOperationToken = rr1.UniqueOperationToken
                }).Result;
                r.EnsureSuccessStatusCode();
            }
        }
    }
}