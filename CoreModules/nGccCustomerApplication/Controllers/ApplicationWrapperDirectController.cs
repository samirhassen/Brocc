using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Linq;
using nGccCustomerApplication.Code;
using System.Text;
using nGccCustomerApplication.Code.AccountDataSharing;

namespace nGccCustomerApplication.Controllers
{
    public class ApplicationWrapperDirectController : NController
    {
        [Route("application-wrapper-direct")]
        public ActionResult Index(string token, string eventName, int? testDelayMs)
        {
            var client = new PreCreditClientWrapperDirectPart();
            var state = client.GetApplicationState(token);

            if (state == null)
            {
                return HttpNotFound(); //Or not authorized maybe
            }

            var svCountries = ISO3166.GetCountryCodesAndNames("sv").ToDictionary(x => x.code, x => x.name);
            var fiCountries = ISO3166.GetCountryCodesAndNames("fi").ToDictionary(x => x.code, x => x.name);
            var countries = svCountries.Keys.Select(x => new { key = x, sv = svCountries[x], fi = fiCountries[x] }).ToList();
            var kycQuestions = NEnv.KycQuestions.ToString();

            ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.GetEncoding("iso-8859-1").GetBytes(JsonConvert.SerializeObject(new
            {
                state = state,
                initialEventName = eventName,
                isProduction = NEnv.IsProduction,
                translateUrl = Url.Action("Translation", "Common"),
                svTranslations = Translations.FetchTranslation("sv"),
                fiTranslations = Translations.FetchTranslation("fi"),
                kycQuestionsXml = kycQuestions,
                kycQuestions = KycQuestions.FetchJsonResource(kycQuestions),
                countries = countries,
                documentCheckDocumentUrlPattern = Url.Action("AttachedFile", "ApplicationWrapperDirect", new { token = token, id = "xxxxxx" }),
                addAttachedFileUrl = Url.Action("AddAttachedFile", "ApplicationWrapperDirect"),
                removeAttachedFileUrl = Url.Action("RemoveAttachedFile", "ApplicationWrapperDirect"),
                getStateUrl = Url.Action("GetState", "ApplicationWrapperDirect"),
                commitDocumentCheckDocumentsUrl = Url.Action("CommitDocumentCheckDocuments", "ApplicationWrapperDirect"),
                commitShouldChooseDocumentSource = Url.Action("CommitShouldChooseDocumentSource", "ApplicationWrapperDirect"),
                applicant1AccountDataShareUrl = Url.Action("BeginShareAccountData", "ApplicationWrapperDirect", new { token, applicantNr = 1, testDelayMs }),
                applicant2AccountDataShareUrl = Url.Action("BeginShareAccountData", "ApplicationWrapperDirect", new { token, applicantNr = 2, testDelayMs }),
                isBankAccountDataSharingEnabled = NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.bankAccountDataSharing"),
                isForcedBankAccountDataSharing = state.IsForcedBankAccountDataSharing,
            })));

            return View();
        }

        [Route("application-wrapper-direct-begin-share-accountdata")]
        public async Task<ActionResult> BeginShareAccountData(string token, int applicantNr, int? testDelayMs)
        {
          if(!NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.bankAccountDataSharing"))
            return HttpNotFound();

            var client = new PreCreditClientWrapperDirectPart();
            var isSatAccountDataSharingEnabled = await client.GetIsSatAccountDataSharingEnabled(token);
            
            if (!NEnv.IsProduction && !isSatAccountDataSharingEnabled)
            {
                AccountDataSharingMockFlow.BeginShare(token, applicantNr, testDelayMs.HasValue ? TimeSpan.FromMilliseconds(testDelayMs.Value) : TimeSpan.FromSeconds(5));
                var externalUrlToIndex = NEnv.ServiceRegistry.External.ServiceUrl("nGccCustomerApplication", "application-wrapper-direct",
                    Tuple.Create("token", token),
                    Tuple.Create("eventName", "returningFromAccountShare"),
                    //NOTE: Dont forward testDelayMs in the production case
                    Tuple.Create("testDelayMs", testDelayMs.HasValue ? testDelayMs.Value.ToString() : null));
                return Redirect(externalUrlToIndex.ToString());
            }
            else
            {
                var state = await client.GetApplicationStateAsync(token);
                var externalUrlToIndex = NEnv.ServiceRegistry.External.ServiceUrl("nGccCustomerApplication", "application-wrapper-direct",
                    Tuple.Create("token", token),
                    Tuple.Create("eventName", "returningFromAccountShare"));
                var service = new PSD2Service();
                var sessionKey = $"A_{Guid.NewGuid().ToString()}";
                var callbackUrl = NEnv.ServiceRegistry.External.ServiceUrl("nGccCustomerApplication", $"Api/AccountSharing-Application/{sessionKey}/CalculationResultCallback");
                var redirectUrl = await service.StartSession(sessionKey, externalUrlToIndex, externalUrlToIndex, callbackUrl);
                service.LogEvent(sessionKey, "application", $"session started for token {token} and applicant {applicantNr} and application {state?.ApplicationNr}");
                PSD2Controller.ApplicationSessionsBySessionKey[sessionKey] = new PSD2Controller.ApplicationSession
                {
                    ApplicantNr = applicantNr,
                    ApplicationDirectToken = token,
                    SessionKey = sessionKey,
                    ApplicationNr = state.ApplicationNr
                };
                return Redirect(redirectUrl.ToString());
            }
        }


        [Route("application-wrapper-direct-sign")]
        public ActionResult Sign(string token, int applicantNr)
        {
            var client = new PreCreditClientWrapperDirectPart();
            var result = client.CreateApplicationSignatureLink(token, applicantNr);
            if(result == null)
                return HttpNotFound();
            if (result.ApplicantNr.HasValue && result.ApplicantNr.Value != applicantNr)
            {
                //Can happen if the agreement changes or the signature provider changes when one but not both customers have signed 
                //If we just redirect to the first user here it will be wierd for the user since the "wrong" person is suddenly signing.
                //The state will have been reset by CreateApplicationSignatureLink so the Index page should now display applicant 1 instead
                return RedirectToAction("Index", new { token, eventName = SignatureNeedsToRestartEventName });
            }
            else if (result?.SignatureUrl == null)
                return HttpNotFound();
            else
                return Redirect(result.SignatureUrl);
        }

        private const string SignatureNeedsToRestartEventName = "RestartSignature";

        [Route("application-wrapper-direct-apply-additionalquestions")]
        [HttpPost]
        public ActionResult ApplyAdditionalQuestions(string token, PreCreditClient.AnswersModel answers, string userLanguage)
        {
            var client = new PreCreditClientWrapperDirectPart();
            var state = client.ApplyAdditionalQuestionAnswers(token, answers, userLanguage);
            return Json2(new { state = state });
        }

        [Route("application-wrapper-direct-attachedfile")]
        [HttpGet]
        public ActionResult AttachedFile(string token, string id)
        {
            var file = DocumentCheckRepository.SharedInstance.GetFile(token, id);
            if (file == null)
                return HttpNotFound();
            else
            {
                var dc = new DocumentClient();
                string cn;
                string fn;
                var data = dc.FetchRawWithFilename(file.ArchiveKey, out cn, out fn);
                var f = new FileStreamResult(new System.IO.MemoryStream(data), cn);
                f.FileDownloadName = fn;
                return f;
            }
        }

        private class AddAttachedFileRequest
        {
            public string token { get; set; }
            public int? applicantNr { get; set; }
            public string filename { get; set; }
            public string dataurl { get; set; }
        }

        [Route("application-wrapper-direct-add-attachedfile")]
        [HttpPost]
        public ActionResult AddAttachedFile()
        {
            //The default serializer cant handle files of any reasonable size and doesnt seem to respect any size settings so we just do it manually instead.
            AddAttachedFileRequest request;
            Request.InputStream.Position = 0;
            using (var r = new System.IO.StreamReader(Request.InputStream))
            {
                request = JsonConvert.DeserializeObject<AddAttachedFileRequest>(r.ReadToEnd());
            }

            var id = DocumentCheckRepository.SharedInstance.AddFile(request.token, request.applicantNr.Value, request.filename, request.dataurl, true);
            return Json2(new
            {
                id
            });
        }

        [Route("application-wrapper-direct-remove-attachedfile")]
        [HttpPost]
        public ActionResult RemoveAttachedFile(string token, string fileId)
        {
            try
            {
                DocumentCheckRepository.SharedInstance.RemoveFile(token, fileId, true);
                return Json2(new
                {

                });
            }
            catch (DocumentCheckRepository.UserVisibleException ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        [Route("application-wrapper-direct-commit-shouldchoosedocumentsource")]
        [HttpPost]
        public ActionResult CommitShouldChooseDocumentSource(string token, int applicantNr, string sourceCode)
        {
            var client = new PreCreditClientWrapperDirectPart();
            //TODO: Save source code or does that happen after they are done?
            var state = client.UpdateApplicationDocumentDataSourceState(new PreCreditClientWrapperDirectPart.DocumentSourceRequest
            {
                Token = token, 
                ApplicantNr = applicantNr,
                SourceCode = sourceCode
            });
            return Json2(new
            {
                state = state
            });
        }

        [Route("application-wrapper-direct-commit-documentcheckdocuments")]
        [HttpPost]
        public ActionResult CommitDocumentCheckDocuments(string token)
        {
            var files = DocumentCheckRepository.SharedInstance.GetFiles(token, false);
            if (files == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Session timed out");
            var client = new PreCreditClientWrapperDirectPart();
            var state = client.AttachUserAddedDocumentCheckDocuments(new PreCreditClientWrapperDirectPart.DocumentCheckAttachRequest
            {
                token = token,
                Files = files?.Select(x => new PreCreditClientWrapperDirectPart.DocumentCheckAttachRequest.File
                    {
                        ApplicantNr = x.ApplicantNr,
                        ArchiveKey = x.ArchiveKey,
                        FileName = x.FileName,
                        MimeType = x.MimeType
                    }).ToList()
            });
            DocumentCheckRepository.SharedInstance.RemoveSessionIfExists(token);
            return Json2(new
            {
                state = state
            });
        }

        [Route("application-wrapper-direct-state")]
        [HttpPost]
        public ActionResult GetState(string token)
        {
            var client = new PreCreditClientWrapperDirectPart();
            var state = client.GetApplicationState(token);
            return Json2(new { state = state });
        }

        [Route("application-wrapper-direct-signed-ok")]
        public ActionResult Success(string token, int? applicantNr, string signatureSessionKey, string sessionId)
        {
            //sessionId is the one used for signicat
            var client = new PreCreditClientWrapperDirectPart();
            bool isSessionFailed;
            client.UpdateSignatureState(token, signatureSessionKey ?? sessionId, applicantNr, out isSessionFailed);
            var eventName = isSessionFailed ? $"Applicant{applicantNr}SignatureSessionFailed" : null;

            return RedirectToAction("Index", new { token = token, eventName = eventName });
        }

        [Route("application-wrapper-direct-signed-failed")]
        public ActionResult Failed(string token, int? applicantNr, string signatureSessionKey, string sessionId)
        {
            var client = new PreCreditClientWrapperDirectPart();
            bool isSessionFailed;
            client.UpdateSignatureState(token, signatureSessionKey ?? sessionId, applicantNr, out isSessionFailed);
            var eventName = isSessionFailed ? $"Applicant{applicantNr}SignatureSessionFailed" : null;

            return RedirectToAction("Index", new { token = token, eventName = eventName });
        }
    }
}
