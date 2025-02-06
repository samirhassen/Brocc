using Newtonsoft.Json;
using nPreCredit.Code;
using NTech;
using NTech.Core;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    [NTechAuthorizeCreditMiddle]
    public class DocumentCheckController : NController
    {
        private const string ConfirmedIncomePerMonthAmountPropertyName = "confirmedIncomePerMonthAmount";

        [Route("DocumentCheck/New")]
        public ActionResult New(string applicationNr)
        {
            var repo = DependancyInjection.Services.Resolve<IPartialCreditApplicationModelRepository>();

            var appModel = repo.Get(applicationNr, new PartialCreditApplicationModelRequest
            {
                ApplicationFields = new List<string>() { "documentCheckStatus" },
                ApplicantFields = new List<string> { ConfirmedIncomePerMonthAmountPropertyName, "incomePerMonthAmount", "employer", "employment" },
                ErrorIfGetNonLoadedField = true,
            });

            //Restart if needed
            var documentCheckStatus = appModel.Application.Get("documentCheckStatus").StringValue.Optional ?? "Initial";

            if (documentCheckStatus != "Initial")
            {
                SetDocumentCheckStatus(applicationNr, null, null);
                documentCheckStatus = "Initial";
            }

            using (var context = new PreCreditContextExtended(this.CurrentUserId, this.Clock, this.InformationMetadata))
            {
                Func<int, object> getApplicantData = applicantNr =>
                {
                    if (applicantNr > appModel.NrOfApplicants)
                        return null;

                    return new
                    {
                        confirmedIncome = appModel.Applicant(applicantNr).Get(ConfirmedIncomePerMonthAmountPropertyName).DecimalValue.Optional,
                        statedIncome = appModel.Applicant(applicantNr).Get("incomePerMonthAmount").DecimalValue.Optional,
                        employer = appModel.Applicant(applicantNr).Get("employer").StringValue.Optional,
                        employment = appModel.Applicant(applicantNr).Get("employment").StringValue.Optional,
                        documents = GetDocuments(applicationNr, applicantNr, null, context)
                    };
                };

                var model = new
                {
                    isViewMode = false,
                    documentCheckStatus = documentCheckStatus,
                    translation = GetTranslations(),
                    applicationNr = applicationNr,
                    applicant1 = getApplicantData(1),
                    applicant2 = getApplicantData(2),
                    acceptUrl = Url.ActionStrict("Accept", "DocumentCheck"),
                    rejectUrl = Url.ActionStrict("Reject", "DocumentCheck"),
                    setConfirmedIncomeUrl = Url.ActionStrict("SetConfirmedIncome", "DocumentCheck"),
                    attachDocumentUrl = Url.ActionStrict("AttachDocument", "DocumentCheck")
                };

                SetInitialData(model);

                return View("NewOrView");
            }
        }

        [Route("DocumentCheck/View")]
        public ActionResult DocumentCheckView(string applicationNr)
        {
            var repo = DependancyInjection.Services.Resolve<IPartialCreditApplicationModelRepository>();

            var appModel = repo.Get(applicationNr, new PartialCreditApplicationModelRequest
            {
                ApplicationFields = new List<string>() { "documentCheckStatus", "documentCheckRejectionReasons" },
                ApplicantFields = new List<string> { ConfirmedIncomePerMonthAmountPropertyName, "incomePerMonthAmount", "employer", "employment" },
                ErrorIfGetNonLoadedField = true,
            });

            var documentCheckStatus = appModel.Application.Get("documentCheckStatus").StringValue.Optional ?? "Initial";

            List<string> documentCheckRejectionReasons = null;
            if (documentCheckStatus == "Rejected")
            {
                var dRaw = appModel.Application.Get("documentCheckRejectionReasons").StringValue.Optional;
                documentCheckRejectionReasons = dRaw == null ? null : JsonConvert.DeserializeObject<List<string>>(dRaw);
            }

            using (var context = new PreCreditContextExtended(this.CurrentUserId, this.Clock, this.InformationMetadata))
            {
                Func<int, object> getApplicantData = applicantNr =>
                {
                    if (applicantNr > appModel.NrOfApplicants)
                        return null;

                    return new
                    {
                        confirmedIncome = appModel.Applicant(applicantNr).Get(ConfirmedIncomePerMonthAmountPropertyName).DecimalValue.Optional,
                        statedIncome = appModel.Applicant(applicantNr).Get("incomePerMonthAmount").DecimalValue.Optional,
                        employer = appModel.Applicant(applicantNr).Get("employer").StringValue.Optional,
                        employment = appModel.Applicant(applicantNr).Get("employment").StringValue.Optional,
                        documents = GetDocuments(applicationNr, applicantNr, null, context)
                    };
                };

                var model = new
                {
                    isViewMode = true,
                    documentCheckStatus = documentCheckStatus,
                    documentCheckRejectionReasons,
                    translation = GetTranslations(),
                    applicationNr = applicationNr,
                    applicant1 = getApplicantData(1),
                    applicant2 = getApplicantData(2),
                    acceptUrl = Url.ActionStrict("Accept", "DocumentCheck"),
                    rejectUrl = Url.ActionStrict("Reject", "DocumentCheck"),
                    setConfirmedIncomeUrl = Url.ActionStrict("SetConfirmedIncome", "DocumentCheck"),
                    attachDocumentUrl = Url.ActionStrict("AttachDocument", "DocumentCheck")
                };

                SetInitialData(model);

                return View("NewOrView");
            }
        }

        [Route("DocumentCheck/ArchiveDocument")]
        [HttpGet()]
        public ActionResult ArchiveDocument(string key)
        {
            var c = new nDocumentClient();
            string contentType;
            string filename;
            var b = c.FetchRawWithFilename(key, out contentType, out filename);
            var r = new FileStreamResult(new MemoryStream(b), contentType);
            r.FileDownloadName = filename;
            return r;
        }

        [HttpPost]
        [Route("api/DocumentCheck/Accept")]
        [NTechApi]
        public ActionResult Accept(string applicationNr)
        {
            if (string.IsNullOrWhiteSpace(applicationNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing applicationNr");

            SetDocumentCheckStatus(applicationNr, true, null);

            return Json2(new { });
        }

        [HttpPost]
        [Route("api/DocumentCheck/Reject")]
        [NTechApi]
        public ActionResult Reject(string applicationNr, List<string> rejectionReasons)
        {
            if (string.IsNullOrWhiteSpace(applicationNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing applicationNr");

            if (rejectionReasons == null || rejectionReasons.Count == 0)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing rejectionReasons");

            SetDocumentCheckStatus(applicationNr, false, rejectionReasons);

            return Json2(new { });
        }

        [HttpPost]
        [Route("api/DocumentCheck/AttachDocument")]
        [NTechApi]
        public ActionResult AttachDocument(string applicationNr, int? applicantNr, int? customerId, string dataUrl, string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing filename");
            if (string.IsNullOrWhiteSpace(applicationNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing applicationNr");

            byte[] filedata;
            string mimeType;
            if (!FileUtilities.TryParseDataUrl(dataUrl, out mimeType, out filedata))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid file");

            var dc = new nDocumentClient();
            var archiveKey = dc.ArchiveStore(filedata, mimeType, filename);
            using (var context = new PreCreditContextExtended(this.CurrentUserId, this.Clock, this.InformationMetadata))
            {
                context.CreateAndAddApplicationDocument(archiveKey, filename, CreditApplicationDocumentTypeCode.DocumentCheck, applicantNr: applicantNr, applicationNr: applicationNr, customerId: customerId);

                context.SaveChanges();

                var documents = context
                    .CreditApplicationDocumentHeaders
                    .Where(x => x.ApplicationNr == applicationNr && x.DocumentType == CreditApplicationDocumentTypeCode.DocumentCheck.ToString() && !x.RemovedByUserId.HasValue)
                    .OrderByDescending(x => x.Id)
                    .Select(x => new
                    {
                        x.Id,
                        x.DocumentArchiveKey,
                        x.DocumentFileName,
                    })
                    .ToList();

                return Json2(GetDocuments(applicationNr, applicantNr, customerId, context));
            }
        }

        private class DocumentCheckStatusData : PartialCreditApplicationModelExtendedCustomDataBase
        {
            public bool IsActive { get; set; }
            public bool IsPartiallyApproved { get; set; }
            public string AgreementStatus { get; set; }
            public int NrOfApplicantsWithUploadedDocuments { get; set; }
        }

        [HttpPost]
        [Route("api/DocumentCheck/FetchStatus")]
        [NTechApi]
        public ActionResult FetchStatus(string applicationNr)
        {
            if (string.IsNullOrWhiteSpace(applicationNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing applicationNr");

            var repo = DependancyInjection.Services.Resolve<IPartialCreditApplicationModelRepositoryExtended>();
            var model = repo.GetExtended(applicationNr,
                new PartialCreditApplicationModelRequest
                {
                    ApplicationFields = new List<string> { "documentCheckStatus", "documentCheckRejectionReasons" },
                    ErrorIfGetNonLoadedField = true
                },
                (an, context) => context
                        .CreditApplicationHeadersQueryable
                        .Where(x => x.ApplicationNr == an)
                        .Select(x => new DocumentCheckStatusData
                        {
                            IsActive = x.IsActive,
                            IsPartiallyApproved = x.IsPartiallyApproved,
                            AgreementStatus = x.AgreementStatus,
                            NrOfApplicants = x.NrOfApplicants,
                            NrOfApplicantsWithUploadedDocuments = x
                                .Documents
                                .Where(y => !y.RemovedByUserId.HasValue && y.ApplicantNr.HasValue)
                                .Select(y => y.ApplicantNr)
                                .Distinct()
                                .Count()
                        })
                        .Single());
            var documentCheckStatus = model.Application.Get("documentCheckStatus").StringValue.Optional ?? "Initial";

            List<string> rejectionReasons = null;
            if (documentCheckStatus == "Rejected")
            {
                rejectionReasons = JsonConvert.DeserializeObject<List<string>>(model.Application.Get("documentCheckRejectionReasons").StringValue.Required);
            }

            return Json2(new
            {
                isApplicationActive = model.CustomData.IsActive,
                isApplicationPartiallyApproved = model.CustomData.IsPartiallyApproved,
                isAccepted = documentCheckStatus == "Accepted",
                isRejected = documentCheckStatus == "Rejected",
                allApplicantsHaveSignedAgreement = model.CustomData.AgreementStatus == "Accepted",
                allApplicantsHaveAttachedDocuments = model.CustomData.NrOfApplicantsWithUploadedDocuments >= model.NrOfApplicants,
                rejectionReasons = rejectionReasons
            });
        }

        [HttpPost]
        [Route("api/DocumentCheck/SetConfirmedIncome")]
        [NTechApi]
        public ActionResult SetConfirmedIncome(SetConfirmedIncomeRequest request)
        {
            using (var context = new PreCreditContextExtended(this.CurrentUserId, this.Clock, this.InformationMetadata))
            {
                return context.UsingTransaction(() =>
                {
                    if (string.IsNullOrWhiteSpace(request?.ApplicationNr))
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing applicationNr");

                    var app = context
                        .CreditApplicationHeaders
                        .Include("Items")
                        .SingleOrDefault(x => x.ApplicationNr == request.ApplicationNr);

                    if (app == null)
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such application");

                    decimal? confirmedIncome1 = Numbers.ParseDecimalOrNull(request?.ConfirmedIncome1);
                    decimal? confirmedIncome2 = Numbers.ParseDecimalOrNull(request?.ConfirmedIncome2);

                    if (!confirmedIncome1.HasValue || (app.NrOfApplicants > 1 && !confirmedIncome2.HasValue))
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing or invalid confirmedIncome1 or confirmedIncome2");

                    var items = new List<PreCreditContextExtended.CreditApplicationItemModel>();

                    items.Add(new PreCreditContextExtended.CreditApplicationItemModel
                    {
                        GroupName = "applicant1",
                        Name = ConfirmedIncomePerMonthAmountPropertyName,
                        IsEncrypted = false,
                        Value = confirmedIncome1.Value.ToString(CultureInfo.InvariantCulture)
                    });
                    if (app.NrOfApplicants > 1)
                        items.Add(new PreCreditContextExtended.CreditApplicationItemModel
                        {
                            GroupName = "applicant2",
                            Name = ConfirmedIncomePerMonthAmountPropertyName,
                            IsEncrypted = false,
                            Value = confirmedIncome2.Value.ToString(CultureInfo.InvariantCulture)
                        });

                    context.AddOrUpdateCreditApplicationItems(app, items, "DocumentCheck");

                    context.SaveChanges();

                    return Json2(new
                    {
                        confirmedIncome1 = confirmedIncome1,
                        confirmedIncome2 = confirmedIncome2
                    });
                });
            }
        }

        private void SetDocumentCheckStatus(string applicationNr, bool? isAccepted, List<string> rejectionReasons)
        {
            using (var context = new PreCreditContextExtended(this.CurrentUserId, this.Clock, this.InformationMetadata))
            {
                context.DoUsingTransaction(() =>
                {
                    var header = context.CreditApplicationHeaders.Include("Items").Single(x => x.ApplicationNr == applicationNr);

                    context.SetDocumentCheckStatus(header, isAccepted, rejectionReasons);
                    CreditApplicationEventCode eventCode;
                    string commentText;
                    if (!isAccepted.HasValue)
                    {
                        commentText = "Document check restarted";
                        eventCode = CreditApplicationEventCode.CreditApplicationDocumentCheckRestarted;
                    }
                    else if (isAccepted.Value)
                    {
                        commentText = "Document check accepted";
                        eventCode = CreditApplicationEventCode.CreditApplicationDocumentCheckAccepted;
                    }
                    else
                    {
                        commentText = "Document check rejected";
                        eventCode = CreditApplicationEventCode.CreditApplicationDocumentCheckRejected;
                    }

                    context.CreateAndAddComment(commentText, eventCode.ToString(), creditApplicationHeader: header);
                    context.CreateAndAddEvent(eventCode, creditApplicationHeader: header);

                    HandlePauseOnDocumentCheck(applicationNr, isAccepted, context, header.Items.Where(x => x.Name == "customerId").Select(x => int.Parse(x.Value)).ToList());

                    context.SaveChanges();
                });
            }
        }

        public const string DocumentCheckPauseReasonName = "documentCheck";

        private void HandlePauseOnDocumentCheck(string applicationNr, bool? isAccepted, PreCreditContextExtended context, List<int> customerIds)
        {
            var activePauseItems = context
                .CreditApplicationPauseItems
                .Where(x => x.ApplicationNr == applicationNr && !x.RemovedBy.HasValue && x.PauseReasonName == DocumentCheckPauseReasonName)
                .ToList();

            foreach (var a in activePauseItems)
            {
                a.RemovedBy = this.CurrentUserId;
                a.RemovedDate = Clock.Now;
            }

            if (isAccepted.HasValue && !isAccepted.Value)
            {
                foreach (var customerId in customerIds)
                {
                    context.CreditApplicationPauseItems.Add(context.FillInfrastructureFields(new CreditApplicationPauseItem
                    {
                        ApplicationNr = applicationNr,
                        CustomerId = 1,
                        PauseReasonName = DocumentCheckPauseReasonName,
                        PausedUntilDate = Clock.Today.AddDays(30)
                    }));
                }
            }
        }

        private object GetDocuments(string applicationNr, int? applicantNr, int? customerId, PreCreditContextExtended context)
        {
            var pre = context
                .CreditApplicationDocumentHeaders
                .Where(x => x.ApplicationNr == applicationNr && x.DocumentType == CreditApplicationDocumentTypeCode.DocumentCheck.ToString() && !x.RemovedByUserId.HasValue);
            if (applicantNr.HasValue)
            {
                pre = pre.Where(x => x.ApplicantNr == applicantNr.Value);
            }
            if (customerId.HasValue)
            {
                pre = pre.Where(x => x.CustomerId == customerId.Value);
            }
            return pre
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.DocumentArchiveKey,
                    x.DocumentFileName,
                })
                .ToList()
                .Select(x => new
                {
                    x.Id,
                    x.DocumentArchiveKey,
                    x.DocumentFileName,
                    DocumentUrl = Url.ActionStrict("ArchiveDocument", "DocumentCheck", new { key = x.DocumentArchiveKey })
                })
                .ToList();
        }

        public class SetConfirmedIncomeRequest
        {
            public string ApplicationNr { get; set; }
            public string ConfirmedIncome1 { get; set; }
            public string ConfirmedIncome2 { get; set; }
        }
    }
}