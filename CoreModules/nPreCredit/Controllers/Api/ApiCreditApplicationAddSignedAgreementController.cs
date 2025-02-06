using nPreCredit.Code;
using nPreCredit.Code.Services;
using NTech;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api")]
    public class ApiCreditApplicationAddSignedAgreementController : NController
    {
        [Route("creditapplication/addsignedagreement")]
        [HttpPost]
        public ActionResult AddSignedAgreement(string applicationNr, int applicantNr, string attachedFileAsDataUrl, string attachedFileName, string archiveKey)
        {
            var u = this.User?.Identity as System.Security.Claims.ClaimsIdentity;

            if (u?.FindFirst("ntech.isprovider")?.Value == "true")
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Providers are not allowed access to this function");
            }

            AddCreditNrIfNeeded(applicationNr, "AddSignedAgreement");

            var result = TryAddSignedAgreement(applicationNr, applicantNr, attachedFileAsDataUrl, attachedFileName, archiveKey, false, this.CurrentUserId, this.InformationMetadata, this.Clock, this.Service.Resolve<IApplicationCommentServiceComposable>());

            if (!result.Item1)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, result.Item2);
            }
            else
            {
                UpdateAgreementStatus(applicationNr);
                UpdateCustomerCheckStatus(applicationNr);
                return Json2(new { });
            }
        }

        public static Tuple<bool, string> TryAddSignedAgreement(string applicationNr, int applicantNr, string attachedFileAsDataUrl, string attachedFileName, string archiveKey, bool isManuallyDone, int userId, string informationMetadata, IClock clock, IApplicationCommentServiceComposable commentService)
        {
            using (var context = new PreCreditContext())
            {
                var app = context.CreditApplicationHeaders.SingleOrDefault(x => x.ApplicationNr == applicationNr);

                if (app == null)
                    return Tuple.Create(false, "No such application");
                if (app.NrOfApplicants < applicantNr)
                    return Tuple.Create(false, "Invalid applicantNr");
                if (app.IsFinalDecisionMade)
                    return Tuple.Create(false, "Credit already created");
                if (!app.IsActive)
                    return Tuple.Create(false, "Application is inactive");
            }

            string attachedFileArchiveDocumentKey = null;
            string mimeType = null;
            if (!string.IsNullOrWhiteSpace(attachedFileAsDataUrl) && !string.IsNullOrWhiteSpace(attachedFileName))
            {
                byte[] fileData;
                if (!FileUtilities.TryParseDataUrl(attachedFileAsDataUrl, out mimeType, out fileData))
                {
                    return Tuple.Create(false, "Invalid document file");
                }
                var client = new nDocumentClient();
                attachedFileArchiveDocumentKey = client.ArchiveStore(fileData, mimeType, attachedFileName);
            }
            else if (!string.IsNullOrWhiteSpace(archiveKey))
            {
                var client = new nDocumentClient();
                var meta = client.FetchMetadata(archiveKey);
                if (meta == null)
                    return Tuple.Create(false, "No such key exists in the archive");
                attachedFileArchiveDocumentKey = archiveKey;
                attachedFileName = meta.FileName;
                mimeType = meta.ContentType;
            }

            var now = clock.Now;
            var attachment = attachedFileArchiveDocumentKey == null
                ? null : new { archiveKey = attachedFileArchiveDocumentKey, filename = attachedFileName, mimeType = mimeType };

            var repo = DependancyInjection.Services.Resolve<UpdateCreditApplicationRepository>();
            repo.UpdateApplication(applicationNr, new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest
            {
                InformationMetadata = informationMetadata,
                StepName = isManuallyDone ? "ManuallyAddedAgreement" : "ApiAddedAgreement",
                UpdatedByUserId = userId,
                Items = new List<UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem>
                    {
                        new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem
                        {
                            GroupName = $"document{applicantNr}",
                            IsSensitive = false,
                            Name = "signed_initial_agreement_key",
                            Value = attachedFileArchiveDocumentKey
                        },
                        new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem
                        {
                            GroupName = $"document{applicantNr}",
                            IsSensitive = false,
                            Name = "signed_initial_agreement_date",
                            Value = now.ToString("o")
                        }
                    }
            },
            context =>
            {
                var commentText = isManuallyDone
                        ? $"Agreement document manually added for applicant {applicantNr}"
                        : $"Agreement document added by webservice for applicant {applicantNr}";
                var eventCode = isManuallyDone ? "ManuallyAddedAgreement" : "ApiAddedAgreement";
                var a = attachment == null ? null : CommentAttachment.CreateFileFromArchiveKey(attachment.archiveKey, attachment.mimeType, attachment.filename);
                string em;
                if (!commentService.TryAddCommentComposable(applicationNr, commentText, eventCode, a, out em, context))
                    throw new Exception(em);
                if (attachment != null)
                {
                    var previousSignedAgreements = context
                        .CreditApplicationDocumentHeadersQueryable
                        .Where(x => x.ApplicationNr == applicationNr && x.DocumentType == CreditApplicationDocumentTypeCode.SignedAgreement.ToString() && x.ApplicantNr == applicantNr && !x.RemovedByUserId.HasValue)
                        .ToList();
                    foreach (var p in previousSignedAgreements)
                    {
                        p.RemovedByUserId = context.CurrentUserId;
                        p.RemovedDate = context.CoreClock.Now;
                    }
                    context.CreateAndAddApplicationDocument(attachment.archiveKey, attachment.filename, CreditApplicationDocumentTypeCode.SignedAgreement, applicationNr: applicationNr, applicantNr: applicantNr);
                }
            });
            return Tuple.Create(true, (string)null);
        }
    }
}