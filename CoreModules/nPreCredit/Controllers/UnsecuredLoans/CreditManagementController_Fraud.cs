using nPreCredit.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    public partial class CreditManagementController
    {

        [Route("FraudCheckNew")]
        public ActionResult FraudCheckNew(string applicationNr, int applicantNr, bool continueExisting)
        {
            Func<string, ActionResult> goBackWithMessage = msg =>
            {
                return RedirectToAction("CreditApplication", "CreditManagement", new { applicationNr, onLoadMessage = $"Fraud check not possible: {msg}" });
            };
            //Check if preconditions for a fraud check are met
            var repo = DependancyInjection.Services.Resolve<IPartialCreditApplicationModelRepository>();
            var appModel = repo.Get(
                applicationNr,
                applicantFields: new List<string> { "customerId" });
            string missingFieldsMessage;
            if (!repo.ExistsAll(applicationNr, out missingFieldsMessage, applicationFields: new List<string> { NEnv.ClientCfg.Country.BaseCountry == "FI" ? "iban" : "bankAccountNr" }))
            {
                return goBackWithMessage("Missing from application: " + missingFieldsMessage);
            }
            using (var context = new PreCreditContext())
            {
                var h = context.CreditApplicationHeaders.Where(x => x.ApplicationNr == applicationNr).Select(x => new
                {
                    x.IsActive,
                    x.IsFinalDecisionMade,
                    x.IsPartiallyApproved
                }).Single();

                if (!h.IsActive)
                {
                    return goBackWithMessage("Application is not active");
                }
                if (h.IsFinalDecisionMade || h.IsPartiallyApproved)
                {
                    return goBackWithMessage("Application has already been approved");
                }
            }

            var cc = new PreCreditCustomerClient();
            var customerId = appModel.Applicant(applicantNr).Get("customerId").IntValue.Required;
            var r = cc.CheckPropertyStatus(customerId, new HashSet<string>() { "addressZipcode", "civicRegNr" });
            if (r.HasMissingPropertyNamesIssueOnRequestedProperties())
            {
                return goBackWithMessage(r.GetMissingPropertyNamesIssueDescription());
            }
            return RedirectToAction("New", "FraudCheck", new { applicationNr, applicantNr, continueExisting });
        }

        [Route("FraudCheckView")]
        public ActionResult FraudCheckView(string applicationNr, int applicantNr)
        {
            return RedirectToAction("View", "FraudCheck", new { applicationNr, applicantNr });
        }
    }
}