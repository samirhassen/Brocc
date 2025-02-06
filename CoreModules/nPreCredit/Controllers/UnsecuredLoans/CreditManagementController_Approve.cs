using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.Email;
using NTech.Core.Module.Shared.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    public partial class CreditManagementController
    {
        [HttpPost]
        [Route("ApproveApplication")]
        public ActionResult ApproveApplication(string applicationNr, bool? skipDwLiveUpdate)
        {
            if (!NEnv.IsUnsecuredLoansEnabled || NEnv.IsStandardUnsecuredLoansEnabled)
            {
                //Only used by unsecured loan legacy
                return HttpNotFound();
            }

            using (var context = new PreCreditContextExtended(Resolver.Resolve<INTechCurrentUserMetadata>(), Clock))
            {
                var aa = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == applicationNr)
                    .Select(x => new
                    {
                        x.CurrentCreditDecision,
                        App = x,
                        IsMortgageLoanApplication = x.MortgageLoanExtension != null
                    })
                    .Single();
                var a = aa.App;
                var currentCreditDecision = aa.CurrentCreditDecision;

                if (aa.IsMortgageLoanApplication)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "This method cannot approve mortgage loan applications");
                }

                if (!a.IsActive)
                {
                    NLog.Warning("ApproveApplication({applicationNr}) failed. Reason: Not active", applicationNr);
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Application is not active");
                }

                if (a.IsFinalDecisionMade)
                {
                    NLog.Warning("ApproveApplication({applicationNr}) failed. Reason: Already created a credit", applicationNr);
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "The credit has already been created");
                }

                if (a.CreditCheckStatus != "Accepted")
                {
                    NLog.Warning("ApproveApplication({applicationNr}) failed. Reason: CreditCheckStatus != Accepted", applicationNr);
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "CreditCheckStatus is not Accepted");
                }

                if (a.FraudCheckStatus != "Accepted")
                {
                    NLog.Warning("ApproveApplication({applicationNr}) failed. Reason: FraudCheckStatus != Accepted", applicationNr);
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "FraudCheckStatus is not Accepted");
                }

                if (a.CustomerCheckStatus != "Accepted")
                {
                    NLog.Warning("ApproveApplication({applicationNr}) failed. Reason: CustomerCheckStatus != Accepted", applicationNr);
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "CustomerCheckStatus is not Accepted");
                }
                if (a.AgreementStatus != "Accepted")
                {
                    NLog.Warning("ApproveApplication({applicationNr}) failed. Reason: AgreementStatus != Accepted", applicationNr);
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "AgreementStatus is not Accepted");
                }

                string additionalLoanCreditNr = null;
                var creditDecision = currentCreditDecision as AcceptedCreditDecision;
                if (creditDecision == null)
                {
                    NLog.Warning("ApproveApplication({applicationNr}) failed. Reason: CurrentCreditDecision is missing", applicationNr);
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No current credit decision exists");
                }
                else
                {
                    var newLoanOffer = CreditDecisionModelParser.ParseAcceptedNewCreditOffer(creditDecision.AcceptedDecisionModel);
                    var additionalLoanOffer = CreditDecisionModelParser.ParseAcceptedAdditionalLoanOffer(creditDecision.AcceptedDecisionModel);

                    if (newLoanOffer == null && additionalLoanOffer == null)
                    {
                        NLog.Warning("ApproveApplication({applicationNr}) failed. Reason: CurrentCreditDecision has no offer", applicationNr);
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Current credit decision has no offer");
                    }
                    if (newLoanOffer != null && additionalLoanOffer != null)
                    {
                        NLog.Warning("ApproveApplication({applicationNr}) failed. Reason: CurrentCreditDecision has both new loan and additional loan offers", applicationNr);
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Current credit decision has both new loan and additional loan offers");
                    }
                    if (additionalLoanOffer != null && string.IsNullOrWhiteSpace(additionalLoanOffer.creditNr))
                    {
                        NLog.Warning("ApproveApplication({applicationNr}) failed. Reason: CurrentCreditDecision has an additional loan offer but no creditNr", applicationNr);
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "CurrentCreditDecision has an additional loan offer but no creditNr");
                    }
                    additionalLoanCreditNr = additionalLoanOffer?.creditNr;
                }
                var isAdditionalLoanOffer = additionalLoanCreditNr != null;

                var now = Clock.Now;

                string commentEmailPart = "";

                var repo = DependancyInjection.Services.Resolve<IPartialCreditApplicationModelRepository>();
                var appModel = repo.Get(applicationNr, applicantFields: new List<string> { "customerId" });
                var customerClient = new PreCreditCustomerClient();

                var emailValidator = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
                var emailsPerApplicant = Enumerable
                    .Range(1, appModel.NrOfApplicants)
                    .Select(x =>
                    {
                        var customerId = appModel.Applicant(x).Get("customerId").IntValue.Required;
                        var kv = customerClient.GetCustomerCardItems(customerId, "email");

                        var value = kv.ContainsKey("email") ? kv["email"] : null;

                        var isMissing = string.IsNullOrWhiteSpace(value);
                        var isInvalid = !isMissing && !emailValidator.IsValid(value);

                        return new { applicantNr = x, email = value, isMissing = isMissing, isInvalid = isInvalid };
                    });

                if (emailsPerApplicant.Any(x => x.isMissing || x.isInvalid))
                {
                    var m = string.Join(", ", emailsPerApplicant.Select(x => x.isInvalid ? $"invalid email for applicant {x.applicantNr}" : $"missing email for applicant {x.applicantNr}"));
                    var invalidOrMissingEmailMessage = $"Could not approve application since approval emails could not be sent: {m}";

                    var newComment = context.CreateAndAddComment(invalidOrMissingEmailMessage, "ApplicationApprovedFailed", applicationNr: applicationNr);

                    context.CreditApplicationComments.Add(newComment);

                    context.SaveChanges();

                    return Json2(new
                    {
                        userWarningMessage = invalidOrMissingEmailMessage,
                        newComment = new
                        {
                            newComment.Id,
                            newComment.CommentDate,
                            newComment.CommentText,
                            CommentByName = GetUserDisplayNameByUserId(newComment.CommentById.ToString())
                        }
                    });
                }

                a.IsPartiallyApproved = true;
                a.PartiallyApprovedById = CurrentUserId;
                a.PartiallyApprovedDate = now;

                var wasEmailSent = TrySendApprovalEmails(emailsPerApplicant.Select(x => x.email).ToList(), applicationNr, additionalLoanCreditNr);

                commentEmailPart = wasEmailSent ? " and approval email sent to applicants" : "";

                context.CreateAndAddComment(
                    "Application approved " + (isAdditionalLoanOffer ? $"(additional loan: {additionalLoanCreditNr}) " : "") + commentEmailPart,
                    "ApplicationApproved",
                    applicationNr: applicationNr);

                string userWarningMessage = null;
                if (!wasEmailSent)
                {
                    var emailFailedMessage = "Application approved but email could not be sent";
                    context.CreateAndAddComment(
                        emailFailedMessage,
                        "ApplicationApprovedEmailNotSent",
                        applicationNr: applicationNr);
                    userWarningMessage = emailFailedMessage;
                }

                context.SaveChanges();

                NTech.Services.Infrastructure.Eventing.NTechEventHandler.PublishEvent(PreCreditEventCode.CreditApplicationPartiallyApproved.ToString(), JsonConvert.SerializeObject(new { applicationNr = applicationNr, skipDwLiveUpdate = skipDwLiveUpdate, additionalLoanCreditNr = additionalLoanCreditNr }));

                return Json2(new
                {
                    redirectToUrl = userWarningMessage == null ? Url.Action("CreditApplications") : null,
                    userWarningMessage = userWarningMessage,
                    reloadPage = userWarningMessage != null
                });
            }
        }

        private static bool TrySendApprovalEmails(List<string> emails, string applicationNr, string additionalLoanCreditNr)
        {
            var s = EmailServiceFactory.CreateEmailService();
            string templateName = additionalLoanCreditNr != null ? "creditapproval-letter-additionalloan" : "creditapproval-letter-general";
            try
            {
                s.SendTemplateEmail(emails, templateName, null, $"Reason=CreditApproval, ApplicationNr={applicationNr}" + (additionalLoanCreditNr != null ? $", AdditionalLoanCreditNr={additionalLoanCreditNr}" : ""));
                return true;
            }
            catch (Exception ex)
            {
                NLog.Error(ex, $"Failed to send approval email but application was still approved: {applicationNr}");
                return false;
            }
        }
    }
}