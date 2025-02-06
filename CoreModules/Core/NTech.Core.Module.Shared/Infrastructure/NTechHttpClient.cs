using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace NTech.Services.Infrastructure
{
    public class NTechHttpClient : HttpClient
    {
        private NTechHttpClient(Action<string> log) : base(new LoggingHandler(log))
        {

        }

        private NTechHttpClient() : base()
        {

        }

        public static NTechHttpClient Create() => Create(logFile: null);
        
        public static NTechHttpClient Create(NTechRotatingLogFile logFile = null)
        {
            return logFile == null ? new NTechHttpClient() : new NTechHttpClient(x => logFile.Log(x));
        }

        public static NTechHttpClient Create(Action<string> log = null)
        {
            return log == null ? new NTechHttpClient() : new NTechHttpClient(log);
        }

        public void SetBasicAuthentication(string userName, string password)
        {
            DefaultRequestHeaders.Authorization = new NTechBasicAuthenticationHeaderValue(userName, password);
        }

        private class NTechBasicAuthenticationHeaderValue : AuthenticationHeaderValue
        {
            public NTechBasicAuthenticationHeaderValue(string userName, string password)
                : base("Basic", EncodeCredential(userName, password))
            {
            }

            private static string EncodeCredential(string userName, string password)
            {
                Encoding uTF = Encoding.UTF8;
                string s = $"{userName}:{password}";
                return Convert.ToBase64String(uTF.GetBytes(s));
            }
        }
    }
}
