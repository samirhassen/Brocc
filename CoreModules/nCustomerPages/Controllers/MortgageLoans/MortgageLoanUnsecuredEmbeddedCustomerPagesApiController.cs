using nCustomerPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace nTest.Controllers
{
    [NTechApi]
    [RoutePrefix("api/mortgage-loans")]
    public class MortgageLoanUnsecuredEmbeddedCustomerPagesApiController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!NEnv.IsEmbeddedMortageLoanCustomerPagesEnabled)
            {
                filterContext.Result = HttpNotFound();
            }
            base.OnActionExecuting(filterContext);
        }

        private ActionResult ApiError(string code, string message)
        {
            return new RawJsonActionResult
            {
                CustomHttpStatusCode = 400,
                CustomStatusDescription = code,
                JsonData = JsonConvert.SerializeObject(new
                {
                    errorCode = code,
                    errorMessage = message
                })
            };
        }

        public class StartAdditionalQuestionsSessionRequest
        {
            [Required]
            public string LoginSessionDataToken { get; set; }

            public string ApplicationNr { get; set; }

            public string TokenType { get; set; }
        }

        [HttpPost]
        [Route("start-additional-questions-session")]
        public ActionResult StartAdditionalQuestionsSession(StartAdditionalQuestionsSessionRequest request)
        {
            var validator = new NTechWebserviceRequestValidator();
            var validationErrors = validator.Validate(request ?? new StartAdditionalQuestionsSessionRequest());
            if (validationErrors.Any())
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing: " + string.Join(", ", validationErrors.Select(x => x.Path)));
            }

            if (string.IsNullOrWhiteSpace(request.ApplicationNr) && request.TokenType != "afterSignApplicationToken")
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing: ApplicationNr");
            }

            string verifyConnectedCivicRegNr = null;
            string applicationNr = null;
            var pc = new nCustomerPages.Code.PreCreditClient(() => NEnv.SystemUserBearerToken);

            if (request.TokenType == "afterSignApplicationToken")
            {
                if (!TryGetApplicationSignatureStatus(request.LoginSessionDataToken, out var response, out var errorCodeAndMessage))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, errorCodeAndMessage.Item1);
                }
                applicationNr = response.ApplicationNr;
            }
            else
            {
                if (!nCustomerPages.Controllers.SignicatController.TryConsumeLoginSessionDataToken(request.LoginSessionDataToken, pc, out var sessionData))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }
                verifyConnectedCivicRegNr = sessionData.UserInfo.CivicRegNr;
                applicationNr = request.ApplicationNr;
            }

            try
            {
                var p = NHttp
                    .Begin(NEnv.ServiceRegistry.Internal.ServiceRootUri("nPreCredit"), NEnv.SystemUserBearerToken, TimeSpan.FromMinutes(5))
                    .PostJson($"api/MortgageLoan/Fetch-AdditionalQuestions-Status", new { ApplicationNr = applicationNr, VerifyConnectedCivicRegNr = verifyConnectedCivicRegNr });

                if (p.IsSuccessStatusCode)
                {
                    var r = JObject.Parse(p.ParseAsRawJson());

                    //Ensure that the user can only post back on the same application they started from
                    var submissionToken = pc.StoreTemporarilyEncryptedData(JsonConvert.SerializeObject(new
                    {
                        applicationNr = request.ApplicationNr
                    }), 4);
                    r.AddOrReplaceJsonProperty("SubmissionToken", new JValue(submissionToken), true);

                    return new RawJsonActionResult
                    {
                        JsonData = r.ToString()
                    };
                }
                else
                    return new HttpStatusCodeResult(p.StatusCode, "Failed");
            }
            catch (NTechWebserviceMethodException ex)
            {
                if (ex.ErrorCode == "notFound")
                {
                    //Handled differently since this will happen when the wrong person consumes the link so we want a semi nice error message while not leaking that this exists
                    return ApiError("notFound", "No such application found");
                }
                throw;
            }
        }

        [HttpPost]
        [Route("complete-additional-questions-session")]
        public ActionResult CompleteAdditionalQuestionsSession()
        {
            //We reuse the request to not have to remap the entire structure to pass it forward
            Request.InputStream.Position = 0;
            JObject request;
            using (var r = new StreamReader(Request.InputStream))
            {
                request = JObject.Parse(r.ReadToEnd());
            }
            const string TokenPropertyName = "SubmissionToken";

            var questionsSubmissionToken = request.GetStringPropertyValue(TokenPropertyName, true);

            if (questionsSubmissionToken == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing: QuestionsSubmissionToken");

            var pc = new nCustomerPages.Code.PreCreditClient(() => NEnv.SystemUserBearerToken);

            //Never allow this in production as it's quite the security hole but it simplifies testing alot since you dont have to redo the entire application to test a bugfix
            var removeTokenAfter = NEnv.IsProduction || !request.GetBooleanPropertyValue("SkipRemoveToken", true).GetValueOrDefault();

            if (!pc.TryGetTemporarilyEncryptedData(questionsSubmissionToken, out var sessionData, removeAfter: removeTokenAfter))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such session exists");

            var applicationNr = JsonConvert.DeserializeAnonymousType(sessionData, new { applicationNr = "" }).applicationNr;

            request.RemoveJsonProperty(TokenPropertyName, true);
            request.AddOrReplaceJsonProperty("ApplicationNr", new JValue(applicationNr), true);
            request.AddOrReplaceJsonProperty("CustomerIpAddress", new JValue(this.HttpContext?.GetOwinContext()?.Request?.RemoteIpAddress), true);

            var p = NHttp
                .Begin(NEnv.ServiceRegistry.Internal.ServiceRootUri("nPreCredit"), NEnv.SystemUserBearerToken, TimeSpan.FromMinutes(5))
                .PostJsonRaw($"api/MortgageLoan/Submit-AdditionalQuestions", request.ToString());

            return p.HandlingApiError(x =>
            {
                return new RawJsonActionResult
                {
                    JsonData = JsonConvert.SerializeObject(new
                    {
                    })
                };
            }, x => ApiError(x.ErrorCode, x.ErrorMessage));
        }

        [HttpPost]
        [Route("get-agreement-signature-status-by-token")]
        public ActionResult GetAgreementSignatureStatusByToken()
        {
            Request.InputStream.Position = 0;
            using (var r = new StreamReader(Request.InputStream))
            {
                var request = JsonConvert.DeserializeObject<PartialSignatureRequest>(r.ReadToEnd());

                var p = NHttp
                    .Begin(NEnv.ServiceRegistry.Internal.ServiceRootUri("nPreCredit"), NEnv.SystemUserBearerToken, TimeSpan.FromMinutes(5))
                    .PostJson("api/MortgageLoan/Fetch-Dual-Agreement-SignatureStatus", new { Token = request?.Token, ReturnTokenSigningUrl = true });

                return p.HandlingApiError(x =>
                {
                    var rr = x.ParseJsonAs<AgreementSignatureResponse>();
                    return new RawJsonActionResult
                    {
                        JsonData = JsonConvert.SerializeObject(rr)
                    };
                }, x => ApiError(x.ErrorCode, x.ErrorMessage));
            }
        }

        [HttpPost]
        [Route("get-application-signature-status-by-token")]
        public ActionResult GetApplicationSignatureStatusByToken()
        {
            Request.InputStream.Position = 0;
            using (var r = new StreamReader(Request.InputStream))
            {
                var request = JsonConvert.DeserializeObject<PartialSignatureRequest>(r.ReadToEnd());

                if (TryGetApplicationSignatureStatus(request?.Token, out var response, out var errMsgAndCode))
                    return new RawJsonActionResult
                    {
                        JsonData = JsonConvert.SerializeObject(response)
                    };
                else
                    return ApiError(errMsgAndCode.Item1, errMsgAndCode.Item2);
            }
        }

        private bool TryGetApplicationSignatureStatus(string token, out ApplicationSignatureResponse response, out Tuple<string, string> failedCodeAndMessage)
        {
            var p = NHttp
                .Begin(NEnv.ServiceRegistry.Internal.ServiceRootUri("nPreCredit"), NEnv.SystemUserBearerToken, TimeSpan.FromMinutes(5))
                .PostJson("api/MortgageLoan/Fetch-Dual-Application-SignatureStatus", new { Token = token, ReturnTokenSigningUrl = true });
            ApplicationSignatureResponse responseLocal = null;
            Tuple<string, string> failedCodeAndMessageLocal = null;
            var isOk = p.HandlingApiError(x =>
            {
                responseLocal = p.ParseJsonAs<ApplicationSignatureResponse>();
                return true;
            }, x =>
            {
                failedCodeAndMessageLocal = Tuple.Create(x.ErrorCode, x.ErrorMessage);
                return false;
            });

            response = responseLocal;
            failedCodeAndMessage = failedCodeAndMessageLocal;
            return isOk;
        }

        private class AgreementSignatureResponse
        {
            public bool IsSignatureStepAccepted { get; set; }
            public string TokenSigningUrl { get; set; }
            public bool IsPendingSignatures { get; set; }
            public bool CustomerHasAlreadySigned { get; set; }
        }

        private class ApplicationSignatureResponse
        {
            public bool IsPendingSignatures { get; set; }
            public Dictionary<int, bool> HasSignedByCustomerId { get; set; }
            public Dictionary<int, string> SignatureTokenByCustomerId { get; set; }
            public string TokenSigningUrl { get; set; }
            public string ApplicationNr { get; set; }
        }

        private class PartialSignatureRequest
        {
            public string Token { get; set; }
            public bool ReturnTokenSigningUrl { get; set; }
        }
    }
}