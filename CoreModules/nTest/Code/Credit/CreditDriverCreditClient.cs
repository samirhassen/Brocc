using NTech.Core.Module.Shared.Clients;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace nTest.Controllers
{
    public class CreditDriverCreditClient
    {
        private readonly IServiceClientSyncConverter syncConverter = new ServiceClientSyncConverterLegacy();

        private HttpClient CreateClient(bool useNTechHost = false)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(NEnv.ServiceRegistry.Internal[useNTechHost ? "NTechHost" : "nCredit"]);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("X-Ntech-TimetravelTo", TimeMachine.SharedInstance.GetCurrentTime().ToString("o"));
            client.SetBearerToken(NEnv.AutomationBearerToken());
            client.Timeout = TimeSpan.FromMinutes(30);
            return client;
        }

        public void ImportPaymentFile(string fileFormatName, string fileName, string fileAsDataUrl, bool overrideIbanCheck)
        {
            using (var client = CreateClient(useNTechHost: true))
            {
                var response = syncConverter.ToSync(() => client.PostAsJsonAsync("Api/Credit/PaymentPlacement/Import-PaymentFile", new
                {
                    fileFormatName = fileFormatName,
                    fileName = fileName,
                    fileAsDataUrl = fileAsDataUrl,
                    overrideIbanCheck = overrideIbanCheck
                }));
                response.EnsureSuccessStatusCode();
            }
        }

        private class GetAllUnpaidNotificationsResult
        {
            public List<InvoiceModel> Notifications { get; set; }
        }

        public List<InvoiceModel> GetAllUnpaidNotifications(DateTime? notificationDate)
        {
            using (var client = CreateClient())
            {
                var response = client.PostAsJsonAsync("Api/Credit/AllUnpaidNotifications", new { notificationDate }).Result;
                response.EnsureSuccessStatusCode();
                return syncConverter.ToSync(() => response.Content.ReadAsAsync<GetAllUnpaidNotificationsResult>())?.Notifications;
            }
        }

        public void CreateOutgoingPaymentFile()
        {
            using (var client = CreateClient())
            {
                var response = client.PostAsJsonAsync("Api/OutgoingPayments/CreateBankFile", new
                {

                }).Result;
                response.EnsureSuccessStatusCode();
            }
        }

        public void SendAllEligableToDebtCollection()
        {
            using (var client = CreateClient())
            {
                var response = client.PostAsJsonAsync("Api/Credit/SendAllEligableToDebtCollection", new
                {

                }).Result;
                response.EnsureSuccessStatusCode();
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

        private class GetMaxTransactionDateResult
        {
            public DateTime? MaxTransactionDate { get; set; }
        }

        public DateTime? GetMaxTransactionDate()
        {
            using (var client = CreateClient())
            {
                var response = client.PostAsJsonAsync("Api/Credit/MaxTransactionDate", new { }).Result;
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsAsync<GetMaxTransactionDateResult>()?.Result?.MaxTransactionDate;
            }
        }

        public class GetNotificationsSummaryData
        {
            public string OcrPaymentReference { get; set; }
            public decimal TotalUnpaidAmount { get; set; }
        }

        public GetNotificationsSummaryData GetNotificationsSummary(string creditNr)
        {
            using (var client = CreateClient())
            {
                var response = client.PostAsJsonAsync("Api/Credit/Notifications", new { creditNr = creditNr }).Result;
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    return null;
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsAsync<GetNotificationsSummaryData>()?.Result;
            }
        }

        public class CreditDetails
        {
            public class D
            {
                public decimal NotNotifiedCapitalAmount { get; set; }
            }

            public D Details { get; set; }
        }

        public CreditDetails GetCreditDetails(string creditNr)
        {
            using (var client = CreateClient())
            {
                var response = client.PostAsJsonAsync("Api/Credit/Details", new { creditNr = creditNr }).Result;
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    return null;
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsAsync<CreditDetails>()?.Result;
            }
        }
    }
}