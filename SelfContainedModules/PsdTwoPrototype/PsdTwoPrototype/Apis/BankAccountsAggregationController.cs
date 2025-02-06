using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PsdTwoPrototype.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PsdTwoPrototype.Apis
{
    [Route("api/")]
    [ApiController]
    public class BankAccountsAggregationsController : ControllerBase
    {
        [HttpGet]
        [Route("BankAccountsAggregations")]
        public async Task<ActionResult<ReturnTokens>> GetBankAccountsAggregationsReturnTokens(string internalRequestId)
        {
            var bankAccountData = new BankAccountModel
            {
                Service = new Service
                {
                    Name = "consumerFinland",
                    LenderName = "Naktergal", //Shown as "Abort and go back to #lenderName#" in signing process
                    NoRawData = false
                },
                Integration = new Integration
                {
                    AccountDataCallback = "https://psd2-prototype.naktergaltech.se/api/AccountDataCallback/" + internalRequestId,
                    CalculationResultCallback = "https://psd2-prototype.naktergaltech.se/api/CalculationResultCallback/" + internalRequestId,
                    EndUserRedirectSuccess = "https://psd2-prototype.naktergaltech.se/api/SuccessRedirect/" + internalRequestId,
                    EndUserRedirectError = "https://psd2-prototype.naktergaltech.se/" //Add Error handling 
                },
                Basis = new Basis
                {
                    CompanyName = "Balanzia",
                    PurposeCode = "6",
                    Locale = "fi_FI",
                    RequestId = "1"
                }
            };
            var client = new HttpClient();
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://test.asiakastieto.fi/services/psd2-api/bankAccountsAggregations"),
                Headers = {
                {"app-id", "8a028d5c" },
                { "app-key", "376441000011-37e3ed9ef8ad23adcd7aad0eef22d294" },
                { HttpRequestHeader.Accept.ToString(), "application/json" },
                { HttpRequestHeader.ContentType.ToString(), "application/json" }
            },
                Content = new StringContent(JsonConvert.SerializeObject(bankAccountData), Encoding.UTF8, "application/json")
            };
            var response = client.SendAsync(httpRequestMessage).Result;
            var responseContent = await response.Content.ReadAsStringAsync();

            var returnTokens = JsonConvert.DeserializeObject<ReturnTokens>(responseContent);

            return returnTokens; 
        }

    }


    public class ReturnTokens
    {
        [JsonProperty("nonce")]
        public string nonce { get; set; }
        [JsonProperty("sessionToken")]
        public string sessionToken { get; set; }
    }
}

