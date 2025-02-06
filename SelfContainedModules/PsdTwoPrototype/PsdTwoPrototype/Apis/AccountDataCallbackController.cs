using Microsoft.AspNetCore.Mvc;
using PsdTwoPrototype.Controllers;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PsdTwoPrototype.Apis
{
    [Route("api/")]
    [ApiController]
    public class AccountDataCallbackController : ControllerBase
    {
        [HttpPost]
        [Route("AccountDataCallback/{internalRequestId}")]
        public async Task<HttpResponseMessage> GetPDFViaAccountDataCallback()
        {
            using var reader = new StreamReader(HttpContext.Request.Body);
            var body = await reader.ReadToEndAsync(); //jsonstring with values returned here
            string internalRequestId = (string)this.RouteData.Values["internalRequestId"];

            //try and get pdf
            var client = new HttpClient();
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://test.asiakastieto.fi/services/psd2-api/bankAccountsAggregations/createRulePDF/" + HomeController.sessionHandler[internalRequestId]),
                Headers = {
                    { "app-id", "8a028d5c" },
                    { "app-key", "376441000011-37e3ed9ef8ad23adcd7aad0eef22d294" },
                    { HttpRequestHeader.Accept.ToString(), "application/pdf" }
                },
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

            var response = client.SendAsync(httpRequestMessage).Result;
            Stream stream = await response.Content.ReadAsStreamAsync();
            using (FileStream file = new FileStream("PDFs/" + internalRequestId + ".pdf", FileMode.Create, System.IO.FileAccess.Write))
            {
                byte[] bytes = new byte[stream.Length];
                stream.Read(bytes, 0, (int)stream.Length);
                file.Write(bytes, 0, bytes.Length);
                stream.Close();
            }
            //pdf end

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}


