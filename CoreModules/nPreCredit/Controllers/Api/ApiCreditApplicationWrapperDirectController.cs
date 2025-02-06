using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.LegacyUnsecuredLoans;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.PreCredit.Shared.Services.UlLegacy;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api/creditapplication-wrapper-direct")]
    public class ApiCreditApplicationWrapperDirectController : NController
    {
        private readonly AgreementSigningProvider agreementSigningProvider;
        private readonly UlLegacyAdditionalQuestionsService additionalQuestionsService;

        public ApiCreditApplicationWrapperDirectController(AgreementSigningProvider agreementSigningProvider, UlLegacyAdditionalQuestionsService ulLegacyApplyAdditionalQuestionsService)
        {
            this.agreementSigningProvider = agreementSigningProvider;
            this.additionalQuestionsService = ulLegacyApplyAdditionalQuestionsService;
        }

        [Route("update-application-document-source-state")]
        [HttpPost]
        public ActionResult UpdateApplicationDocumentSourceState(DocumentSourceRequest request)
        {
            if (!(request?.sourceCode == "shareAccount" || request?.sourceCode == "manual"))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid sourceCode");

            var contextFactory = Service.Resolve<IPreCreditContextFactoryService>();
            using (var c = contextFactory.CreateExtended())
            {
                var repo = Service.Resolve<CreditManagementWorkListService>();
                var listStates = repo.GetSearchModel(c, true);

                var app = additionalQuestionsService.ApplicationsByTokenQuery(c, request.token)
                    .Select(x => new
                    {
                        x.ApplicationNr,
                        ApplicationHeader = x
                    })
                    .SingleOrDefault();

                if (app == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such session active");
                }

                var newItems = new List<UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem>
                           {
                               new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem
                               {
                                   GroupName = $"applicant{request.applicantNr}",
                                   Name = "documentSourceStatus",
                                   Value = request.sourceCode
                               },
                           }.ToList();

                var r = DependancyInjection.Services.Resolve<UpdateCreditApplicationRepository>();
                r.UpdateApplication(app.ApplicationNr, new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest
                {
                    Items = newItems
                }
                );

                var applicationState = additionalQuestionsService.GetApplicationState(request.token);
               
                c.CreateAndAddEvent(
                      CreditApplicationEventCode.CreditApplicationUserChoseDocumentSource, applicationNr: app.ApplicationNr, app.ApplicationHeader);
                c.CreateAndAddComment(
                      applicationState.IsForcedBankAccountDataSharing ? 
                      $"Applicant {request.applicantNr} chose document source '{request?.sourceCode}' (application has forced bank account data sharing)."
                      : $"Applicant {request.applicantNr} chose document source '{request?.sourceCode}'.", CreditApplicationEventCode.CreditApplicationUserChoseDocumentSource.ToString(), applicationNr: app.ApplicationNr);

                c.SaveChanges();

                return Json2(new { state = applicationState });
            }
        }



        [Route("update-signature-state")]
        [HttpPost]
        public ActionResult UpdateSignatureState(string token, string signatureSessionKey)
        {
            var state = additionalQuestionsService.GetApplicationState(token);

            object agreementAddedEventData = null;

            if (!(state.IsActive && state.ActiveState.ShouldSignAgreements))
                return Json2(new { });

            var now = Clock.Now;

            var s = this.agreementSigningProvider.GetSignatureStatusOnCallback(state.ApplicationNr, token, signatureSessionKey);

            if (s.IsMissingSession)
            {
                return Json2(new { isFailed = true });
            }
            else if (s.IsPendingSignature)
            {
                if (s.ApplicantsNrsThatHaveSigned.Count > 0)
                {
                    using (var context = new PreCreditContextExtended(this.NTechUser, this.Clock))
                    {
                        context.CreateAndAddComment($"Agreement signed by applicant {string.Join(", ", s.ApplicantsNrsThatHaveSigned.Select(x => x.ToString()))}. Waiting for other applicants to sign before storing the signed agreement.", "AgreementPartiallySigned", applicationNr: state.ApplicationNr);
                    }
                }
                return Json2(new { });
            }
            else if (s.IsFailed)
            {
                using (var c = new PreCreditContext())
                {
                    c.CreditApplicationComments.Add(new CreditApplicationComment
                    {
                        ApplicationNr = state.ApplicationNr,
                        ChangedById = CurrentUserId,
                        CommentById = CurrentUserId,
                        CommentDate = now,
                        ChangedDate = now,
                        Attachment = null,
                        CommentText = $"Agreement signature failed: {s.FailedMessage}",
                        EventType = "AgreementSignatureFailed"
                    });

                    c.SaveChanges();
                }

                return Json2(new { isFailed = true });
            }
            else if (s.IsSuccess)
            {
                string documentKey = null;
                string documentFileName = null;
                if (s.SignedDocumentArchiveKey != null)
                {
                    documentFileName = $"SignedInitialCreditAgreement-{s.ApplicantNr.Value}-{now:yyyy-MM-dd}.pdf";
                    documentKey = s.SignedDocumentArchiveKey; 
                }
                else if (s.SignedDocumentUrl != null)
                {
                    var dc = new nDocumentClient();
                    documentFileName = $"SignedInitialCreditAgreement-{s.ApplicantNr.Value}-{now:yyyy-MM-dd}.pdf";
                    documentKey = dc.ArchiveStore(
                        new Uri(s.SignedDocumentUrl),
                        documentFileName);
                }

                var newItems = documentKey != null ? s.ApplicantsNrsThatHaveSigned.SelectMany(applicantNr => new List<UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem>
                    {
                        new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem
                        {
                            GroupName = "document" + applicantNr,
                            Name = "signed_initial_agreement_key",
                            Value = documentKey
                        },
                        new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem
                        {
                            GroupName = "document" + applicantNr,
                            Name = "signed_initial_agreement_date",
                            Value = s.SuccessDate.ToString("o")
                        }
                    }).ToList() : new List<UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem>();

                var r = DependancyInjection.Services.Resolve<UpdateCreditApplicationRepository>();
                r.UpdateApplication(state.ApplicationNr, new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest
                {
                    InformationMetadata = InformationMetadata,
                    StepName = "SignedInitialAgreement",
                    UpdatedByUserId = CurrentUserId,
                    Items = newItems
                },
                c2 =>
                {
                    var nrSignedBefore = 0;
                    if (state.ActiveState.AgreementsData.HasApplicant1SignedAgreement)
                        nrSignedBefore += 1;
                    if (state.ActiveState.AgreementsData.HasApplicant2SignedAgreement)
                        nrSignedBefore += 1;
                    var allOthersSigned = nrSignedBefore == (state.NrOfApplicants - 1);

                    if (allOthersSigned)
                    {
                        var h = c2.CreditApplicationHeadersQueryable.Single(x => x.ApplicationNr == state.ApplicationNr);
                        h.AgreementStatus = "Accepted";
                        h.ChangedById = CurrentUserId;
                        h.ChangedDate = Clock.Now;
                    }

                    agreementAddedEventData = new
                    {
                        applicationNr = state.ApplicationNr,
                        applicantNr = s.ApplicantNr.Value,
                        allApplicantsHaveNowSigned = allOthersSigned
                    };

                    c2.AddCreditApplicationComments(new CreditApplicationComment
                    {
                        ApplicationNr = state.ApplicationNr,
                        ChangedById = CurrentUserId,
                        CommentById = CurrentUserId,
                        CommentDate = now,
                        ChangedDate = now,
                        Attachment = documentKey != null ? JsonConvert.SerializeObject(new { archiveKey = documentKey, filename = documentFileName, mimeType = "application/pdf" }) : null,
                        CommentText = $"Agreement signed by applicant {string.Join(", ", s.ApplicantsNrsThatHaveSigned.Select(x => x.ToString()))}",
                        EventType = "AgreementSigned"
                    });
                });
                UpdateCustomerCheckStatus(state.ApplicationNr);
            }
            else
            {
                throw new NotImplementedException();
            }

            if (agreementAddedEventData != null)
            {
                NTech.Services.Infrastructure.Eventing.NTechEventHandler.PublishEvent("SignedAgreementAdded", JsonConvert.SerializeObject(agreementAddedEventData));
            }
            return Json2(new { });
        }

        [Route("fetch-application-state")]
        [HttpPost]
        public ActionResult GetApplicationState(string token)
        {
            var state = additionalQuestionsService.GetApplicationState(token);

            return Json2(new { state = state });
        }

        [Route("create-application-signature-link")]
        [HttpPost]
        public ActionResult CreateApplicationSignatureLink(string token, int? applicantNr)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing token");

                if (!applicantNr.HasValue)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing applicantNr");

                string applicationNr;
                var contextFactory = Service.Resolve<IPreCreditContextFactoryService>();
                using (var c = contextFactory.CreateExtended())
                {
                    var app = additionalQuestionsService.ApplicationsByTokenQuery(c, token)
                            .Select(x => new
                            {
                                x.IsActive,
                                x.ApplicationNr,
                                x.NrOfApplicants,
                                x.AgreementStatus,
                                x.CreditCheckStatus
                            })
                            .SingleOrDefault();

                    if (app == null)
                        return HttpNotFound();

                    if (!app.IsActive)
                    {
                        Log.Warning("CreateApplicationSignatureLink: application is not active {applicationNr}", app.ApplicationNr);
                        return HttpNotFound();
                    }

                    if (app.AgreementStatus == "Accepted")
                    {
                        Log.Warning("CreateApplicationSignatureLink: not allowing since already accepted on {applicationNr}", app.ApplicationNr);
                        return HttpNotFound();
                    }

                    if (app.CreditCheckStatus != "Accepted")
                    {
                        Log.Warning("CreateApplicationSignatureLink: not allowing since credit check is not accepted on {applicationNr}", app.ApplicationNr);
                        return HttpNotFound();
                    }

                    if (applicantNr > app.NrOfApplicants || applicantNr <= 0)
                    {
                        Log.Warning("CreateApplicationSignatureLink: not allowing since there is no such applicant on {applicationNr}", app.ApplicationNr);
                        return HttpNotFound();
                    }
                    applicationNr = app.ApplicationNr;
                }

                var a = DependancyInjection.Services.Resolve<AgreementSigningProvider>();

                byte[] pdfBytes;
                bool isAdditionalLoanOffer;
                string failedMessage;
                string agreementDataHash = null;

                if (!a.TryCreateAgreementPdf(applicationNr, applicantNr.Value, out pdfBytes, out isAdditionalLoanOffer, out failedMessage, observeAgreementDataHash: x => agreementDataHash = x))
                {
                    Log.Warning("CreateApplicationSignatureLink: not allowing since an agreement could not be created {applicationNr}. {failedMessage}", applicationNr, failedMessage);
                    return HttpNotFound();
                }

                if (string.IsNullOrWhiteSpace(NEnv.ApplicationWrapperUrlPattern))
                {
                    throw new Exception("ntech.credit.applicationwrapper.urlpattern required for direct flow");
                }

                string signUrl;
                if (!a.TryCreateAndPossiblySendAgreementLink(pdfBytes, agreementDataHash, applicationNr, applicantNr.Value, out failedMessage, out signUrl, token, allowInvalidEmail: true))
                {
                    Log.Warning("CreateApplicationSignatureLink: not allowing since an agreement link could not be created {applicationNr}. {failedMessage}", applicationNr, failedMessage);
                    return HttpNotFound();
                }

                return Json2(new
                {
                    ApplicationNr = applicationNr,
                    ApplicantNr = applicantNr,
                    IsAdditionalLoanOffer = isAdditionalLoanOffer,
                    SignatureUrl = signUrl
                });
            }
            catch (SignatureMustRestartFromFirstUserException)
            {
                //When the first user has already signed but the agreement or signature provider has changed so what we actually want it so resart the entire session from user 1
                if (applicantNr.HasValue && applicantNr.Value > 1)
                {
                    return CreateApplicationSignatureLink(token, 1);
                }
                else
                    throw;
            }
        }

        [Route("apply-additionalquestion-answers")]
        [HttpPost]
        public ActionResult ApplyAdditionalQuestionAnswers(string token, UlLegacyKycAnswersModel answers, string userLanguage)
        {
            try
            {
                var state = additionalQuestionsService.ApplyAdditionalQuestionAnswers(token, answers, userLanguage);

                return Json2(new { state });
            }
            catch (NTechCoreWebserviceException ex)
            {
                if (ex.IsUserFacing)
                {
                    return NTechWebserviceMethod.ToFrameworkErrorActionResult(
                           NTechWebserviceMethod.CreateErrorResponse(ex.Message, errorCode: ex.ErrorCode ?? "generic", httpStatusCode: ex.ErrorHttpStatusCode ?? 400));
                }
                else
                    throw;
            }
        }

        public class DocumentCheckAttachRequest
        {
            public string token { get; set; }
            public List<File> Files { get; set; }
            public class File
            {
                public int ApplicantNr { get; set; }
                public string FileName { get; set; }
                public string MimeType { get; set; }
                public string ArchiveKey { get; set; }
            }
        }

        public class DocumentSourceRequest
        {
            public string token { get; set; }
            public string applicantNr { get; set; }
            public string sourceCode { get; set; }
        }

        [Route("attach-useradded-documentcheck-documents")]
        [HttpPost]
        public ActionResult AttachUserAddedDocumentCheckDocuments(DocumentCheckAttachRequest request)
        {
            var state = additionalQuestionsService.GetApplicationState(request.token);

            if (state.IsActive && state.ActiveState.IsWatingForDocumentUpload)
            {
                using (var context = new PreCreditContextExtended(this.CurrentUserId, this.Clock, this.InformationMetadata))
                {
                    foreach (var f in request.Files)
                    {
                        context.CreateAndAddApplicationDocument(f.ArchiveKey, f.FileName, CreditApplicationDocumentTypeCode.DocumentCheck, applicantNr: f.ApplicantNr, applicationNr: state.ApplicationNr);
                    }

                    context.CreateAndAddEvent(
                        CreditApplicationEventCode.CreditApplicationUserAddedDocuments, applicationNr: state.ApplicationNr);
                    context.CreateAndAddComment(
                        $"User commited {request.Files.Count} document check documents using the web interface", CreditApplicationEventCode.CreditApplicationUserAddedDocuments.ToString(), applicationNr: state.ApplicationNr);

                    context.SaveChanges();
                }
            }

            return Json2(new { state = additionalQuestionsService.GetApplicationState(request.token) });
        }

        [Route("get-is-sat-account-data-sharing-enabled")]
        [HttpPost]
        public ActionResult GetSettings(string token)
        {
            var customerClient = new PreCreditCustomerClient();
            var psd2Settings = customerClient.LoadSettings("psd2Settings");
            var isSatAccountDataSharingEnabled = psd2Settings.Opt("isEnabled") == "true";

            return Json2(new { isSatAccountDataSharingEnabled });
        }
    }
}
