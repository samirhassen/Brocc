using nPreCredit.Code;
using nPreCredit.Code.Agreements;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.LegacyUnsecuredLoans;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.PreCredit.Shared.Code.PetrusOnlyScoringService;
using NTech.Core.PreCredit.Shared.Services.UlLegacy;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using Polly;
using Serilog;
using System;
using System.Linq;
using System.Net;
using System.Web.Http.Results;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api")]
    public class ApiCreditManagementController : NController
    {
        private class CreditReportFindHit
        {
            public DateTimeOffset RequestDate { get; set; }
            public int CreditReportId { get; set; }
        }

        private class CreditReportBuyNewResult
        {
            public bool Success { get; set; }
            public bool IsInvalidCredentialsError { get; set; }
            public int CreditReportId { get; set; }
        }

        [Route("creditapplication/agreement/createpdf")]
        [HttpPost]
        public ActionResult CreateAgreementPdf(string applicationNr, string archiveStoreFilename)
        {
            try
            {
                bool isAdditionalLoanOffer;
                using (var context = this.Service.GetService<IPreCreditContextFactoryService>().CreateExtended())
                {
                    string notApplicableMsg;
                    var tmp = AdditionalLoanSupport.HasAdditionalLoanOffer(applicationNr, context, out notApplicableMsg);
                    if (!tmp.HasValue)
                        return Json2(new { success = false, failedMessage = notApplicableMsg });

                    isAdditionalLoanOffer = tmp.Value;
                }

                var pdfBuilder = Service.Resolve<LoanAgreementPdfBuilderFactory>().Create(isAdditionalLoanOffer);
                byte[] pdfBytes;
                string pdfErrorMessage;

                if (!isAdditionalLoanOffer)
                {
                    AddCreditNrIfNeeded(applicationNr, "CreateAgreementPdf");
                }

                if (!pdfBuilder.TryCreateAgreementPdf(out pdfBytes, out pdfErrorMessage, applicationNr))
                {
                    NLog.Error("Failed to create agreement {error}", pdfErrorMessage);
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, pdfErrorMessage);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(archiveStoreFilename))
                    {
                        var dc = new nDocumentClient();
                        var key = dc.ArchiveStore(pdfBytes, "application/pdf", archiveStoreFilename);
                        return Json2(new
                        {
                            Success = true,
                            ArchiveKey = key
                        });
                    }
                    else
                    {
                        return Json2(new
                        {
                            Success = true,
                            AgreementPdfBytesBase64 = Convert.ToBase64String(pdfBytes)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "CreateAgreementPdf");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Route("creditapplication/fraudcheck/automatic")]
        [HttpPost]
        public ActionResult AutomaticFraudCheck(string applicationNr, bool? wasDoneExternally)
        {
            try
            {
                if (wasDoneExternally.HasValue && wasDoneExternally.Value)
                {
                    //TODO: Demand any fields here?
                    using (var context = new PreCreditContext())
                    {
                        var h = context.CreditApplicationHeaders.Single(x => x.ApplicationNr == applicationNr);

                        if (!h.IsActive || h.IsCancelled || h.IsFinalDecisionMade)
                            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Wrong application status");

                        h.FraudCheckStatus = "Accepted";
                        h.ChangedById = CurrentUserId;
                        h.ChangedDate = Clock.Now;

                        h.Comments.Add(new CreditApplicationComment
                        {
                            ChangedById = CurrentUserId,
                            ChangedDate = h.ChangedDate,
                            CommentById = CurrentUserId,
                            CommentDate = h.ChangedDate,
                            CommentText = "Fraud check done externally",
                            EventType = "ExternalFraudCheck",
                            InformationMetaData = InformationMetadata
                        });

                        context.SaveChanges();
                    }

                    return Json2(new { });
                }
                else
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Can only be used to signal that an external fraud check has been done at the moment");
                }
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "AutomaticFraudCheck");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }


        [Route("creditapplication/creditcheck/automatic")]
        [HttpPost]
        public ActionResult AutomaticCreditCheck(string applicationNr)
        {
            var retryPolicy = Policy
                .Handle<NTechCoreWebserviceException>()
                .Or<Exception>()
                .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(5),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        NLog.Warning(exception, $"AutomaticCreditCheck Retry {retryCount} due to: {exception.Message}");
                    }
                );

            var p2 = Service.Resolve<PetrusOnlyCreditCheckService>();

            try
            {
                retryPolicy.Execute(() => p2.AutomaticCreditCheck(applicationNr, false));
                return new HttpStatusCodeResult(HttpStatusCode.Accepted);
            }
            catch (NTechCoreWebserviceException ex)
            {
                if (ex.IsUserFacing)
                {
                    if(ex.ErrorCode == "petrusError")
                        Service.Resolve<IApplicationCommentService>().TryAddComment(applicationNr, $"Petrus returned an error: {ex.Message}", "petrusError", null, out var _);

                    return NTechWebserviceMethod.ToFrameworkErrorActionResult(
                            NTechWebserviceMethod.CreateErrorResponse(ex.Message, errorCode: ex.ErrorCode ?? "generic", httpStatusCode: ex.ErrorHttpStatusCode ?? 400));
                }
                else
                    throw;
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "AutomaticCreditCheck");
                return NTechWebserviceMethod.ToFrameworkErrorActionResult(
                       NTechWebserviceMethod.CreateErrorResponse("Internal server error", errorCode: "generic", httpStatusCode: 500));
            }
        }

        [Route("creditapplication/automation/setstate")]
        [HttpPost]
        public ActionResult SetAutomationState(bool isSuspended)
        {
            CreditCheckAutomationHandler.IsAutomationSuspended = isSuspended;
            return Json2(new { });
        }
    }
}