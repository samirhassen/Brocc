using nPreCredit.Code;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    public partial class CreditManagementController
    {
        [Route("CreditApplication")]
        public ActionResult CreditApplication(string applicationNr, string onLoadMessage, bool? wasReload = false)
        {
            if (!NEnv.IsUnsecuredLoansEnabled)
                throw new Exception("Unsecured loans are not enabled"); //Not 404 like we usually do since we are highly likely to hit this alot during refactoring.
            else if (NEnv.IsStandardUnsecuredLoansEnabled)
            {
                //TODO: Maybe better to have exception or not found here.
                return Redirect(NEnv.ServiceRegistry.Internal.ServiceUrl("nBackOffice", $"/s/unsecured-loan-application/application/{applicationNr}").ToString());
            }

            UpdateCustomerCheckStatus(applicationNr);

            using (var context = new PreCreditContext())
            {
                var hitPre = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == applicationNr)
                    .Select(x => new
                    {
                        x.ApplicationNr,
                        x.ApplicationType,
                        x.ApplicationDate,
                        x.ArchivedDate,
                        x.ProviderName,
                        MortgageLoan = x.MortgageLoanExtension == null ? null : new
                        {
                            CustomerOfferStatus = x.MortgageLoanExtension.CustomerOfferStatus
                        },
                        DocumentCheckStatus = (x
                            .Items
                            .Where(y => y.Name == "documentCheckStatus")
                            .Select(y => y.Value)
                            .FirstOrDefault() ?? "Initial")
                    })
                    .ToList()
                    .SingleOrDefault();

                if (hitPre == null)
                    return HttpNotFound();

                if (hitPre.ArchivedDate.HasValue)
                    return RedirectToAction("Index", "ArchivedUnsecuredLoanApplication", new { applicationNr });

                var applicationInfo = Service.Resolve<ApplicationInfoService>().GetApplicationInfo(applicationNr);

                if (hitPre.MortgageLoan != null)
                    throw new Exception($"{applicationNr} is a mortgage loan application");

                var navigationTargetToHere = NTechNavigationTarget.CreateCrossModuleNavigationTargetCode(
                            "UnsecuredLoanApplication", new Dictionary<string, string> { { "applicationNr", applicationNr } });

                var hit =
                    new[] { hitPre }
                    .Select(x => new
                    {
                        x.ApplicationNr,
                        x.ApplicationType,
                        x.ApplicationDate,
                        DocumentCheckStatus = x.DocumentCheckStatus,
                        Provider = new
                        {
                            ProviderName = NEnv.GetAffiliateModel(x.ProviderName).DisplayToEnduserName,
                            IsSendingRejectionEmails = NEnv.GetAffiliateModel(x.ProviderName).IsSendingRejectionEmails,
                            IsUsingDirectLinkFlow = NEnv.GetAffiliateModel(x.ProviderName).IsUsingDirectLinkFlow,
                            IsSendingAdditionalQuestionsEmail = NEnv.GetAffiliateModel(x.ProviderName).IsSendingAdditionalQuestionsEmail
                        },
                        WarningMessage = onLoadMessage,
                        RejectApplicationUrl = Url.Action("RejectApplication", new { applicationNr }),
                        ApproveApplicationUrl = Url.Action("ApproveApplication", new { applicationNr }),
                        CancelApplicationUrl = Url.Action("CancelApplication", "CreditManagement", new { applicationNr }),
                        ReactivateApplicationUrl = Url.Action("ReactivateApplication", "CreditManagement", new { applicationNr }),
                        MortgageLoan = x.MortgageLoan,
                        ApplicationInfo = applicationInfo,
                        NavigationTargetToHere = navigationTargetToHere,
                        IsTest = !NEnv.IsProduction,
                        ApplicationBasisUrl = NEnv.ServiceRegistry.Internal.ServiceUrl("nBackOffice", $"s/loan-application/application-basis/{applicationNr}", 
                        Tuple.Create("backTarget", navigationTargetToHere)).ToString()
                    })
                    .SingleOrDefault();

                if (hit == null)
                    return HttpNotFound();

                SetInitialData(hit);

                return View();
            }
        }

        [NTechApi]
        [Route("AddComment")]
        [HttpPost]
        public ActionResult AddComment(string applicationNr, string commentText, string attachedFileAsDataUrl, string attachedFileName, string eventType)
        {
            string failedMessage;
            CreditApplicationCommentModel c = null;
            CommentAttachment a = null;
            if (!string.IsNullOrWhiteSpace(attachedFileAsDataUrl) && !string.IsNullOrWhiteSpace(attachedFileName))
                a = CommentAttachment.CreateFileFromDataUrl(attachedFileAsDataUrl, attachedFileName);
            if (this.Service.Resolve<IApplicationCommentService>().TryAddComment(applicationNr, commentText, eventType, a, out failedMessage, observeCreatedComment: x => c = x))
            {
                return Json2(new
                {
                    newComment = new
                    {
                        c.Id,
                        c.CommentDate,
                        c.CommentText,
                        c.AttachmentFilename,
                        c.AttachmentUrl,
                        c.CommentByName,
                        c.RequestIpAddress
                    }
                });
            }
            else
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
        }

        [NTechApi]
        [Route("SetIsWaitingForAdditionalInformation")]
        [HttpPost]
        public ActionResult SetIsWaitingForAdditionalInformation(string applicationNr, bool? isWaitingForAdditionalInformation)
        {
            if (string.IsNullOrWhiteSpace(applicationNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing applicationNr");

            if (isWaitingForAdditionalInformation == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing isWaitingForAdditionalInformation");

            var result = Service.Resolve<IApplicationWaitingForAdditionalInformationService>().SetIsWaitingForAdditionalInformation(applicationNr, isWaitingForAdditionalInformation.Value);
            object newComment = null;
            if (result.AddedCommentId.HasValue)
            {
                var c = Service.Resolve<IApplicationCommentService>().FetchSingle(result.AddedCommentId.Value);
                newComment = new
                {
                    c.Id,
                    c.CommentDate,
                    c.CommentText,
                    c.CommentByName
                };
            }
            return Json2(new
            {
                newComment = newComment
            });
        }

        [NTechApi]
        [HttpPost]
        [Route("FetchCustomerItems")]
        public ActionResult FetchCustomerItems(int customerId, IList<string> propertyNames)
        {
            var c = new PreCreditCustomerClient();
            var result = c.BulkFetchPropertiesByCustomerIdsD(new HashSet<int> { customerId }, propertyNames?.ToArray())?.Opt(customerId);
            var items = result.Select(x => new { name = x.Key, value = x.Value }).ToList();
            return Json2(new { customerId = customerId, items = items });
        }
    }
}