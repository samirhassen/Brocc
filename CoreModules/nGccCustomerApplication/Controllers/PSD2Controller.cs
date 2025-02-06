using nGccCustomerApplication.Code;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Serilog;
using System.Collections.Concurrent;

namespace nGccCustomerApplication.Controllers
{
    public class PSD2Controller : NController
    {
        private PSD2Service service = new PSD2Service();

        [HttpPost]
        [Route("Api/AccountSharing-Application/{sessionKey}/CalculationResultCallback")]
        public async Task<HttpResponseMessage> CalculationResultCallback()
        {
            string sessionKey = (string)this.RouteData.Values["sessionKey"];

            if (!ApplicationSessionsBySessionKey.TryRemove(sessionKey, out var session))
            {
                service.LogEvent(sessionKey, "application", "calculation-callback on session that does not exist");
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            try
            {
                service.LogEvent(sessionKey, "application", $"downloading shared data for token {session.ApplicationDirectToken} and applicant {session.ApplicantNr}");
                var result = await service.HandleCalculationCallback(sessionKey, Request, downloadDataPdf: false, downloadRulePdf: true, "application");
                service.LogEvent(sessionKey, "application", $"saving shared data for token {session.ApplicationDirectToken} and applicant {session.ApplicantNr} to application");
                var preCreditClient = new PreCreditClientWrapperDirectPart();
                var documentClient = new DocumentClient();
                var pdfArchiveKey = documentClient.ArchiveStore(result.RulePdf, "application/pdf", $"psd2-rule-{session.ApplicationNr}-{session.ApplicantNr}.pdf");
                var rawDataArchiveKey = documentClient.ArchiveStore(Encoding.UTF8.GetBytes(result.RuleEngineData), "application/json", $"psd2-{session.ApplicationNr}-{session.ApplicantNr}.json");
                preCreditClient.UpdateBankAccountDataShareData(session.ApplicationNr, session.ApplicantNr, rawDataArchiveKey, pdfArchiveKey);

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                NLog.Error(ex, $"Direct-Sharing session callback error {sessionKey}");
                service.LogEvent(sessionKey, "application", "calculation-callback error" + Environment.NewLine + ex.ToString());
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("Internal server error")
                };
            }
        }

        internal class ApplicationSession
        {
            public string SessionKey { get; set; }
            public int ApplicantNr { get; set; }
            public string ApplicationNr { get; set; }
            public string ApplicationDirectToken { get; set; }
        }
        internal static readonly ConcurrentDictionary<string, ApplicationSession> ApplicationSessionsBySessionKey = new ConcurrentDictionary<string, ApplicationSession>();
    }
}