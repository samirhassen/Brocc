using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace nGccCustomerApplication.Controllers
{
    public class Psd2DirectController : NController
    {
        private PSD2Service service = new PSD2Service();
        
        private string DirectAccessKey
        {
            get
            {
                if (!service.IsThereAPsd2SettingFileThatExists)
                    return null;

                return service.GetPsd2Setting("directAccessKey", isRequired: false);
            }
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if(DirectAccessKey == null)
                filterContext.Result = HttpNotFound();
            base.OnActionExecuting(filterContext);
        }

        [Route("Ui/AccountSharing-Direct/{accessKey}/Begin")]
        public async Task<ActionResult> BeginDirect()
        {
            string accessKey = (string)this.RouteData.Values["accessKey"];
            if (accessKey != DirectAccessKey)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            var sessionKey = $"L_{Guid.NewGuid().ToString()}";

            var errorUrl = NEnv.ServiceRegistry.External.ServiceUrl("nGccCustomerApplication", $"Ui/AccountSharing-Direct/{sessionKey}/Error");
            var successUrl = NEnv.ServiceRegistry.External.ServiceUrl("nGccCustomerApplication", $"Ui/AccountSharing-Direct/{sessionKey}/Poll");
            var callbackUrl = NEnv.ServiceRegistry.External.ServiceUrl("nGccCustomerApplication", $"Api/AccountSharing-Direct/{sessionKey}/CalculationResultCallback");
            await service.StartSession(sessionKey, successUrl, errorUrl, callbackUrl);
            service.LogEvent(sessionKey, "direct", "session started");
            directSessions[sessionKey] = new DirectSession
            {
                SessionKey = sessionKey,
                IsPending = true
            };
            var redirectUrl = service.GetRedirectCustomerUrl(sessionKey);
            return Redirect(redirectUrl.ToString());
        }

        [Route("Ui/AccountSharing-Direct/{sessionKey}/Error")]
        public ActionResult ErrorDirect()
        {
            string sessionKey = (string)this.RouteData.Values["sessionKey"];
            directSessions.TryRemove(sessionKey, out var _);
            return Content($"Customer redirected to error page");
        }

        [HttpPost]
        [Route("Api/AccountSharing-Direct/{sessionKey}/CalculationResultCallback")]
        public async Task<HttpResponseMessage> CalculationResultCallback()
        {
            string sessionKey = (string)this.RouteData.Values["sessionKey"];
            if (!directSessions.TryGetValue(sessionKey, out var localSession))
            {
                service.LogEvent(sessionKey, "direct", "calculation-callback on session that does not exist");
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            try
            {
                var result = await service.HandleCalculationCallback(sessionKey, Request, true, true, "direct");
                localSession.RuleEngineData = result.RuleEngineData;
                localSession.DataPdfData = result.DataPdf;
                localSession.RulePdfData = result.RulePdf;
                localSession.IsPending = false;

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                NLog.Error(ex, $"Direct-Sharing session callback error {sessionKey}");
                service.LogEvent(sessionKey, "direct", "calculation-callback error" + Environment.NewLine + ex.ToString());
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("Internal server error")
                };
            }
        }

        [Route("Ui/AccountSharing-Direct/{sessionKey}/Poll")]
        public ActionResult PollDirect()
        {

            string sessionKey = (string)this.RouteData.Values["sessionKey"];
            if (!directSessions.TryGetValue(sessionKey, out var session))
                return Content("Session missing");
            if (session.ErrorMessage != null)
                return Content($"Error: {session.ErrorMessage}");

            string body;
            if(session.IsPending)
            {
                body = $"<p>Still waiting for data from SAT. Refresh this page in a bit and it should hopefully arrive. If not, use the sesion key {sessionKey} to debug together with them.</p>";
            }
            else
            {
                string GetUrl<T>(T data, string relativeUrl) where T : class =>
                    data == null ? "#" : NEnv.ServiceRegistry.External.ServiceUrl("nGccCustomerApplication", relativeUrl).ToString();

                var rulePdfUrl = GetUrl(session.RulePdfData, $"Ui/AccountSharing-Direct/{sessionKey}/RulePdf");
                var rawPdfUrl = GetUrl(session.DataPdfData, $"Ui/AccountSharing-Direct/{sessionKey}/DataPdf");
                var dataUrl = GetUrl(session.RuleEngineData, $"Ui/AccountSharing-Direct/{sessionKey}/Data");
                var rawDataFormatted = session.RuleEngineData == null ? "missig" : JObject.Parse(session.RuleEngineData).ToString(Formatting.Indented);
                body = $@"<p>Note that this data is not stored so it will be gone when you close this page.</p>
    <ul>
        <li><a href=""{rulePdfUrl}"" target=""_blank"">Download Rule Pdf</a>
        <li><a href=""{rawPdfUrl}"" target=""_blank"">Download Data Pdf</a>
        <li><a href=""{dataUrl}"" target=""_blank"">Download Raw Account Data</a>
    <ul>
    <h2>Raw Account Data</h2>
    <pre>{rawDataFormatted}</pre>
";
            }

            var pageHtml = $@"<!doctype html>
<html lang=en>
<head>
<meta charset=utf-8>
<title>Account sharing session</title>
</head>
<body>
    <h2> Account sharing session {sessionKey}</h2>
    {body}
</body>
</html>";
            return Content(pageHtml, "text/html; charset=utf-8", Encoding.UTF8);
        }

        [Route("Ui/AccountSharing-Direct/{sessionKey}/RulePdf")]
        public ActionResult RulePdf()
        {
            string sessionKey = (string)this.RouteData.Values["sessionKey"];
            if (!directSessions.TryGetValue(sessionKey, out var session))
                return Content("Session missing");
            if (session.RulePdfData == null)
                return Content("Pdf missing");

            return File(session.RulePdfData, "application/pdf");
        }

        [Route("Ui/AccountSharing-Direct/{sessionKey}/DataPdf")]
        public ActionResult DataPdf()
        {
            string sessionKey = (string)this.RouteData.Values["sessionKey"];
            if (!directSessions.TryGetValue(sessionKey, out var session))
                return Content("Session missing");
            if (session.DataPdfData == null)
                return Content("Pdf missing");

            return File(session.DataPdfData, "application/pdf");
        }

        [Route("Ui/AccountSharing-Direct/{sessionKey}/Data")]
        public ActionResult Data()
        {
            string sessionKey = (string)this.RouteData.Values["sessionKey"];
            if (!directSessions.TryGetValue(sessionKey, out var session))
                return Content("Session missing");
            if (session.RuleEngineData == null)
                return Content("Data missing");

            return Content(session.RuleEngineData, "text/plain");
        }

        private class DirectSession
        {
            public string SessionKey { get; set; }
            public bool IsPending { get; set; }
            public string ErrorMessage { get; set; }
            public string RuleEngineData { get; set; }
            public byte[] RulePdfData;
            public byte[] DataPdfData;
        }
        private static readonly ConcurrentDictionary<string, DirectSession> directSessions = new ConcurrentDictionary<string, DirectSession>();
    }
}