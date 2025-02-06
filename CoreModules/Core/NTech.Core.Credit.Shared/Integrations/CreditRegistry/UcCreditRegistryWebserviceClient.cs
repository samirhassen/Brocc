using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Xml.Serialization;

namespace nCredit.Code.Uc.CreditRegistry
{
    public class UcCreditRegistryWebserviceClient
    {
        private readonly Lazy<Uri> serviceEndpoint;
        private readonly Lazy<Tuple<string, string>> userNameAndPassword;
        private readonly Lazy<string> logFolder;

        public UcCreditRegistryWebserviceClient(Lazy<Uri> serviceEndpoint, Lazy<Tuple<string, string>> userNameAndPassword, Lazy<string> logFolder)
        {
            this.serviceEndpoint = serviceEndpoint;
            this.userNameAndPassword = userNameAndPassword;
            this.logFolder = logFolder;
        }

        public void Send(CreditRegister request)
        {
            var encoding = Encoding.UTF8;
            NTechRotatingLogFile logFile = null;
            if (logFolder.Value != null)
            {
                logFile = new NTechRotatingLogFile(logFolder.Value, "ucCreditRegistry");
            }

            using (var client = NTechHttpClient.Create(logFile: logFile))
            {
                client.BaseAddress = serviceEndpoint.Value;
                client.SetBasicAuthentication(userNameAndPassword.Value.Item1, userNameAndPassword.Value.Item2);

                var serializer = new XmlSerializer(typeof(CreditRegister));
                using (var ms = new MemoryStream())
                {
                    serializer.Serialize(ms, request);
                    var content = new ByteArrayContent(ms.ToArray());
                    content.Headers.Remove("Content-Type");
                    content.Headers.Add("Content-Type", "application/xml");
                    var result = client.PostAsync("", content).Result;
                    result.EnsureSuccessStatusCode();
                }
            }
        }
    }
}