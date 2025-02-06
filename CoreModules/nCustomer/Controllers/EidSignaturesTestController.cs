using NTech.Banking.CivicRegNumbers;
using NTech.Core.Module.Shared.Clients;
using NTech.ElectronicSignatures;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace nCustomer.Controllers
{
    //Functionality to be able to start isolated instances of Eid signatures 
    //Instructions: 
    //machinesettings => ntech.directsignaturetest.enabled = "true" 
    //Browse to [nCustomer]/Ui/EidSignatures-DirectTest/Begin?civicRegNr=<testpersonnr>
    public class EidSignaturesTestController : NController
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!NTechEnvironment.Instance.OptBoolSetting("ntech.directsignaturetest.enabled"))
            {
                filterContext.Result = HttpNotFound();
            }
            base.OnActionExecuting(filterContext);
        }

        private ICustomerClient CreateCustomerClient() => LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);
        private IDocumentClient CreateDocumentClient() => LegacyServiceClientFactory.CreateDocumentClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);

        [Route("Ui/EidSignatures-DirectTest/Begin")]
        public ActionResult BeginTest(string civicRegNr)
        {
            var civicRegNrParser = new CivicRegNumberParser(NEnv.ClientCfg.Country.BaseCountry);
            if (!civicRegNrParser.TryParse(civicRegNr, out var parsedCivicRegNr))
            {
                return Content("Missing or invalid civicRegNr");
            }

            var pdfBytesToSign = GetMinimalPdfBytes($"Test document to sign for {parsedCivicRegNr.NormalizedValue} on {DateTime.Now}");
            var documentClient = CreateDocumentClient();
            var testDocumentSourceId = Guid.NewGuid().ToString();
            var archiveKey = documentClient.ArchiveStoreWithSource(pdfBytesToSign, "application/pdf", "test.pdf", "DirectSignatureTestPdf", Guid.NewGuid().ToString());

            var customerClient = CreateCustomerClient();
            var session = customerClient.CreateElectronicIdSignatureSession(new SingleDocumentSignatureRequestUnvalidated
            {
                DocumentToSignArchiveKey = archiveKey,
                CustomData = new System.Collections.Generic.Dictionary<string, string> { { "sessionType", "directTest" } },
                DocumentToSignFileName = "test.pdf",
                SigningCustomers = new System.Collections.Generic.List<NTech.ElectronicSignatures.SingleDocumentSignatureRequestUnvalidated.SigningCustomer>
                {
                    new SingleDocumentSignatureRequestUnvalidated.SigningCustomer
                    {
                        CivicRegNr = parsedCivicRegNr.NormalizedValue,
                        SignerNr = 1
                    }
                },
                RedirectAfterSuccessUrl = NEnv.ServiceRegistry.Internal.ServiceUrl("nCustomer", "Ui/EidSignatures-DirectTest/{localSessionId}/Success").ToString(),
                RedirectAfterFailedUrl = NEnv.ServiceRegistry.Internal.ServiceUrl("nCustomer", "Ui/EidSignatures-DirectTest/{localSessionId}/Fail").ToString(),
            });
            var url = session.GetActiveSignatureUrlBySignerNr().Values.Single();
            return Redirect(url);
        }


        [Route("Ui/EidSignatures-DirectTest/{sessionId}/Success")]
        public ActionResult SuccessRedirect(string sessionId) => HandleRedirect(sessionId, true);

        [Route("Ui/EidSignatures-DirectTest/{sessionId}/Fail")]
        public ActionResult FailRedirect(string sessionId) => HandleRedirect(sessionId, false);


        private ActionResult HandleRedirect(string sessionId, bool isSuccess)
        {
            var client = CreateCustomerClient();
            try
            {
                if (finalResults.TryGetValue(sessionId, out var r))
                    return PresentResult(sessionId, r);

                var sessionResult = client.GetElectronicIdSignatureSession(sessionId, !isSuccess); //auto close if not success
                if (sessionResult == null)
                    return Content("No such session exists");

                return PresentResult(sessionResult.Value.Session);
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.ToString());
            }
        }

        private ActionResult PresentResult(CommonElectronicIdSignatureSession session)
        {
            if (session.IsFailed())
            {
                var finalResult = new SignatureResult
                {
                    IsSuccess = false,
                    FailedMessage = session.ClosedMessage
                };

                return PresentResult(session.Id, finalResult);
            }
            else if (session.SignedPdf != null)
            {
                var documentClient = CreateDocumentClient();
                var customerClient = CreateCustomerClient();

                var signedPdfData = documentClient.TryFetchRaw(session.SignedPdf.ArchiveKey).FileData;
                var finalResult = new SignatureResult
                {
                    IsSuccess = true,
                    SignedPdfData = signedPdfData
                };

                //Clean up
                documentClient.DeleteArchiveFile(session.UnsignedPdf.ArchiveKey);
                documentClient.DeleteArchiveFile(session.SignedPdf.ArchiveKey);
                customerClient.GetElectronicIdSignatureSession(session.Id, true);

                return PresentResult(session.Id, finalResult);
            }
            else
            {
                return Content("Waiting for signed pdf");
            }
        }

        private ActionResult PresentResult(string sessionId, SignatureResult result)
        {
            if (!finalResults.ContainsKey(sessionId))
                finalResults[sessionId] = result;

            if (result.IsSuccess)
                return File(result.SignedPdfData, "application/pdf");
            else
                return Content("Failed: " + result.FailedMessage);
        }

        private static string GenerateMinimalPdfContent(string text) => $"%PDF-1.2\r\n9 0 obj\r\n<<\r\n>>\r\nstream\r\nBT/ 9 Tf({text})' ET\r\nendstream\r\nendobj\r\n4 0 obj\r\n<<\r\n/Type /Page\r\n/Parent 5 0 R\r\n/Contents 9 0 R\r\n>>\r\nendobj\r\n5 0 obj\r\n<<\r\n/Kids [4 0 R ]\r\n/Count 1\r\n/Type /Pages\r\n/MediaBox [ 0 0 300 50 ]\r\n>>\r\nendobj\r\n3 0 obj\r\n<<\r\n/Pages 5 0 R\r\n/Type /Catalog\r\n>>\r\nendobj\r\ntrailer\r\n<<\r\n/Root 3 0 R\r\n>>\r\n%%EOF";
        public static byte[] GetMinimalPdfBytes(string text) => Encoding.UTF8.GetBytes(GenerateMinimalPdfContent(text));

        private static ConcurrentDictionary<string, SignatureResult> finalResults = new ConcurrentDictionary<string, SignatureResult>();

        private class SignatureResult
        {
            public bool IsSuccess { get; set; }
            public string FailedMessage { get; set; }
            public byte[] SignedPdfData { get; set; }
        }
    }
}