using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace nTest.Controllers
{
    public class CreditDriverDocumentClient
    {
        private HttpClient CreateClient()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(NEnv.ServiceRegistry.Internal["nDocument"]);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("x-ntech-timetravel-time", TimeMachine.SharedInstance.GetCurrentTime().ToString("o"));
            client.SetBearerToken(NEnv.AutomationBearerToken());
            client.Timeout = TimeSpan.FromMinutes(30);
            return client;
        }

        private class StoreResult
        {
            public string Key { get; set; }
        }

        public string Store(string mimeType, string fileName, string base64EncodedFileData)
        {
            using (var client = CreateClient())
            {
                var response = client.PostAsJsonAsync("Archive/Store", new
                {
                    mimeType = mimeType,
                    fileName = fileName,
                    base64EncodedFileData = base64EncodedFileData
                }).Result;
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsAsync<StoreResult>().Result?.Key;
            }
        }

        public string StoreFile(FileInfo file, string fileName, string mimeType)
        {
            using (var client = CreateClient())
            {
                //See: http://stackoverflow.com/questions/20255406/how-to-upload-a-file-and-a-parameter-to-a-remote-server-via-httpclient-postasync
                using (var requestContent = new MultipartFormDataContent())
                using (var fileContent = new StreamContent(file.OpenRead()))
                {
                    fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        Name = "\"file\"",
                        FileName = "\"" + fileName + "\""
                    };
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType);

                    requestContent.Add(fileContent);

                    var result = client.PostAsync("Archive/StoreFile", requestContent).Result;
                    result.EnsureSuccessStatusCode();
                    return result.Content.ReadAsAsync<StoreResult>().Result?.Key;
                }
            }
        }
    }
}