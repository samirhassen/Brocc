using Newtonsoft.Json;
using NTech.Core.Module.Shared.Clients;
using NTech.Services.Infrastructure;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NTech.Core.Module.Shared.Infrastructure;
using System.Xml.Linq;

namespace NTech.Core.PreCredit.Shared.Code.PetrusOnlyScoringService
{
    public class RealPetrusOnlyScoringService : IPetrusOnlyScoringService
    {
        private readonly Uri baseUrl;
        private readonly string username;
        private readonly string password;
        private readonly IServiceClientSyncConverter asyncConverter;
        private readonly NTechRotatingLogFile requestLogfile;

        public RealPetrusOnlyScoringService(Uri baseUrl, string username, string password, IServiceClientSyncConverter asyncConverter, NTechRotatingLogFile requestLogfile)
        {
            //Url from settings will be something like https://brocc.staging.api.internal.brocc.se/loan/v1/loan
            //Authority is then https://brocc.staging.api.internal.brocc.se
            var authority = baseUrl.GetLeftPart(UriPartial.Authority);
            this.baseUrl = new Uri(authority);
            this.username = username;
            this.password = password;
            this.asyncConverter = asyncConverter;
            this.requestLogfile = requestLogfile;
        }

        public PetrusOnlyCreditCheckResponse NewCreditCheck(PetrusOnlyCreditCheckRequest request) => asyncConverter.ToSync(() => NewCreditCheckAsync(request));

        private async Task<PetrusOnlyCreditCheckResponse> NewCreditCheckAsync(PetrusOnlyCreditCheckRequest request)
        {
            try
            {
                var webserviceRequest = PetrusOnlyRequestBuilder.CreateWebserviceRequest(request);

                var client = NTechHttpClient.Create(requestLogfile);

                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(baseUrl, "loan/v2/loan"),
                    Content = new StringContent(webserviceRequest.ToString(), Encoding.UTF8, "application/json")
                };

                client.SetBasicAuthentication(username, password);

                var response = await client.SendAsync(httpRequestMessage);
                if (!response.IsSuccessStatusCode)
                {
                    await HandlePetrusError(response);
                }

                var responseContent = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<PetrusOnlyCreditCheckResponse>(responseContent);
            }
            catch(NTechCoreWebserviceException)
            {
                throw;
            }
            catch(Exception ex)
            {
                throw new NTechCoreWebserviceException($"Petrus only credit check failed for {request?.DataContext?.ApplicationNr}", ex) { ErrorCode = "petrusTwoCreditCheckFailed" };
            }
        }

        private async Task HandlePetrusError(HttpResponseMessage response)
        {
            /*
             Example of error that can happen:
StatusCode: 400, ReasonPhrase: 'Bad request', Version: 1.1, Content: System.Net.Http.StreamContent, Headers:
{
  Connection: keep-alive
  x-amzn-RequestId: 2faf63b9-c266-43fd-a8d6-1b93b8d8b4e1
  Referrer-Policy: no-referrer
  X-XSS-Protection: 0
  x-amzn-Remapped-Content-Length: 34
  X-Frame-Options: DENY
  x-amzn-Remapped-Connection: keep-alive
  x-amz-apigw-id: F1PosEh3gi0FtbA=
  X-Content-Type-Options: nosniff
  Pragma: no-cache
  x-amzn-Remapped-Date: Thu, 01 Jun 2023 09:07:32 GMT
  Cache-Control: no-store, must-revalidate, no-cache, max-age=0
  Date: Thu, 01 Jun 2023 09:07:32 GMT
  Content-Length: 34
  Content-Type: text/plain; charset=UTF-8
  Expires: 0
}
Application is already in progress
             */
            try
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                throw CreatePetrusErrorException(errorMessage);
            }
            catch(NTechCoreWebserviceException)
            {
                throw;
            }
            catch(Exception)
            {
                //If we fail to parse the error it's better that we propagate the real error rather than masking it with our parsing error
                response.EnsureSuccessStatusCode();
            }
        }

        public static NTechCoreWebserviceException CreatePetrusErrorException(string errorMessage) => new NTechCoreWebserviceException(errorMessage) { ErrorCode = "petrusError", IsUserFacing = true, ErrorHttpStatusCode = 400 };

        public XDocument GetPetrusLog(string applicationId) => 
            asyncConverter.ToSync(() => GetPetrusLogAsync(applicationId));

        private async Task<XDocument> GetPetrusLogAsync(string applicationId)
        {
            try
            {
                var client = NTechHttpClient.Create(requestLogfile);

                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    //NOTE: This should be v1 even for petrus v2
                    RequestUri = new Uri(baseUrl, $"loan/v1/loan/{applicationId}/audit-trail")
                };

                client.SetBasicAuthentication(username, password);

                var response = await client.SendAsync(httpRequestMessage);

                response.EnsureSuccessStatusCode();

                //NOTE: This api is a bit strange. It will respond with 200 OK evern for id's that dont exist and then just dont return content so we check the content length
                if(response.Content.Headers.ContentLength == 0)
                {
                    throw new NTechCoreWebserviceException($"Missing petrus audit trail for {applicationId}") { ErrorHttpStatusCode = 400, IsUserFacing = true, ErrorCode = "auditTrailMissing" };
                }

                var responseContent = await response.Content.ReadAsStringAsync();

                return XDocument.Parse(responseContent);
            }
            catch (NTechCoreWebserviceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new NTechCoreWebserviceException($"Petrus only audit trail failed for {applicationId}", ex) { ErrorCode = "petrusTwoAuditTrailFailed" };
            }
        }
    }
}
