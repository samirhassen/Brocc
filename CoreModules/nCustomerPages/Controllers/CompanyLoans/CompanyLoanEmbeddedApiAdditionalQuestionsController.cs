using nCustomerPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nTest.Controllers
{
    [NTechApi]
    [RoutePrefix("api/company-loans")]
    public class CompanyLoanEmbeddedApiAdditionalQuestionsController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!NEnv.IsCompanyLoansEnabled)
            {
                filterContext.Result = HttpNotFound();
            }
            base.OnActionExecuting(filterContext);
        }

        public class StartAdditionalQuestionsSessionRequest
        {
            [Required]
            public string LoginSessionDataToken { get; set; }

            [Required]
            public string ApplicationNr { get; set; }

            public bool? ReturnCompanyInformation { get; set; }
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

            var pc = new nCustomerPages.Code.PreCreditClient(() => NEnv.SystemUserBearerToken);

            if (!nCustomerPages.Controllers.SignicatController.TryConsumeLoginSessionDataToken(request.LoginSessionDataToken, pc, out var sessionData))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            try
            {
                var status = pc.GetCompanyLoanAdditionalQuestionsStatus(request.ApplicationNr, sessionData.UserInfo.CivicRegNr, request.ReturnCompanyInformation.GetValueOrDefault());
                var submissionKey = status.IsPendingAnswers ? pc.StoreTemporarilyEncryptedData(JsonConvert.SerializeObject(new
                {
                    applicationNr = status.ApplicationNr
                }), 4) : null;

                return new RawJsonActionResult
                {
                    JsonData = JsonConvert.SerializeObject(new
                    {
                        status.IsPendingAnswers,
                        status.Offer,
                        status.CompanyInformation,
                        QuestionsSubmissionToken = submissionKey,
                    })
                };
            }
            catch (NTechWebserviceMethodException ex)
            {
                if (ex.ErrorCode == "notFound")
                {
                    //Handled differently since this will happen when the wrong person consumes the link so we want a semi nice error message while not leaking that this exists
                    return new RawJsonActionResult
                    {
                        CustomHttpStatusCode = 400,
                        CustomStatusDescription = "No such application found",
                        JsonData = JsonConvert.SerializeObject(new
                        {
                            errorCode = "notFound"
                        })
                    };
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
            const string TokenPropertyName = "QuestionsSubmissionToken";

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

            pc.SubmitCompanyLoanAdditionalQuestions(request);

            return new RawJsonActionResult
            {
                JsonData = JsonConvert.SerializeObject(new
                {
                })
            };
        }
    }
}