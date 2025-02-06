using nCredit.Code;
using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using nCredit.DbModel.BusinessEvents.NewCredit;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.Email;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class CreateCreditController : NController
    {
        private Lazy<decimal> GetCurrentReferenceInterestRate(ICreditContextExtended context)
        {
            var model = new DomainModel.SharedDatedValueDomainModel(context);
            return new Lazy<decimal>(() => model.GetReferenceInterestRatePercent(Clock.Today));
        }

        private Code.OcrPaymentReferenceGenerator CreateOcrPaymentReferenceGenerator() => new OcrPaymentReferenceGenerator(NEnv.ClientCfg.Country.BaseCountry, () => new CreditContextExtended(GetCurrentUserMetadata(), Clock));


        [HttpPost]
        [Route("Api/CreateCredit")]
        public ActionResult CreateCredit(NewCreditRequest request)
        {
            if (request?.IsCompanyCredit ?? false)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Company loans cannot be created this way");

            var user = this.GetCurrentUserMetadata();
            var customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceHttpContextUser.SharedInstance, NEnv.ServiceRegistry);
            var mgr = new NewCreditBusinessEventManager(user, this.Service.LegalInterestCeiling, Service.CreditCustomerListService,
                Service.GetEncryptionService(user), new CoreClock(), NEnv.ClientCfgCore, customerClient, CreateOcrPaymentReferenceGenerator(), NEnv.EnvSettings,
                Service.PaymentAccount, Service.CustomCostType);

            CreditHeader credit;
            using (var context = new CreditContextExtended(user, Clock))
            {
                credit = context.UsingTransaction(() =>
                {
                    var createdCredit = mgr.CreateNewCredit(context, request, GetCurrentReferenceInterestRate(context));
                    context.SaveChanges();
                    return createdCredit;
                });
            }

            if (credit != null)
            {
                OnLoansCreated(new HashSet<string> { credit.CreditNr });
            }

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        [HttpPost]
        [Route("Api/CreateCredits")]
        public ActionResult CreateCredits(NewCreditRequest[] newCreditRequests, NewAdditionalLoanRequest[] additionalLoanRequests)
        {
            if (newCreditRequests?.Any(x => x.IsCompanyCredit ?? false) ?? false)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Company loans cannot be created this way");

            var currentUser = GetCurrentUserMetadata();
            var customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceHttpContextUser.SharedInstance, NEnv.ServiceRegistry);
            var mgr = new NewCreditBusinessEventManager(currentUser, this.Service.LegalInterestCeiling, Service.CreditCustomerListService, Service.GetEncryptionService(currentUser),
                new CoreClock(), NEnv.ClientCfgCore, customerClient, CreateOcrPaymentReferenceGenerator(), NEnv.EnvSettings, Service.PaymentAccount, Service.CustomCostType);
            var amgr = new NewAdditionalLoanBusinessEventManager(GetCurrentUserMetadata(), this.Service.LegalInterestCeiling, NEnv.EnvSettings,
                customerClient, CoreClock.SharedInstance, NEnv.ClientCfgCore, Service.GetEncryptionService(GetCurrentUserMetadata()), Service.PaymentAccount);

            if (newCreditRequests == null)
                newCreditRequests = new NewCreditRequest[] { };
            if (additionalLoanRequests == null)
            {
                additionalLoanRequests = new NewAdditionalLoanRequest[] { };
            }
            var newCreditNrs = new HashSet<string>();
            using (var context = new CreditContextExtended(currentUser, Clock))
            {
                context.DoUsingTransaction(() =>
                {
                    foreach (var request in newCreditRequests)
                    {
                        var c = mgr.CreateNewCredit(context, request, GetCurrentReferenceInterestRate(context));
                        if (c != null)
                            newCreditNrs.Add(c.CreditNr);
                    }
                    foreach (var request in additionalLoanRequests)
                    {
                        amgr.CreateNew(context, request);
                    }
                    context.SaveChanges();
                });
            }

            if (newCreditNrs.Count > 0)
                OnLoansCreated(newCreditNrs);

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        private void OnLoansCreated(HashSet<string> creditNrs)
        {
            if (creditNrs.Count == 0)
                return;

            try
            {
                Service.CustomerRelationsMerge.MergeLoansToCustomerRelations(onlyTheseCreditNrs: creditNrs);

                SendLoanCreatedSecureMessagesIfEnabled(creditNrs);
            }
            catch (Exception ex)
            {
                //Swallowing this to ensure that external callers dont interpret the service error as the loan not having been
                //created and try to create it again.
                NLog.Error(ex, $"OnLoansCreated failed after creating new loans: {string.Join(",", creditNrs.Take(10))}");
            }
        }

        private void SendLoanCreatedSecureMessagesIfEnabled(HashSet<string> creditNrs)
        {
            var isStandard = NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans.standard");
            if (!isStandard)
                return;

            if (!NTechEmailServiceFactory.HasEmailProvider)
                return;

            var customerClient = new CreditCustomerClient();

            var creditCreatedSecureMessageTemplates = customerClient
                .LoadSettings("creditCreatedSecureMessageTemplates");

            var isEnabled = creditCreatedSecureMessageTemplates["isEnabled"] == "true";
            if (!isEnabled)
                return;

            var service = Service;
            var paymentReceiptService = new LoanPayoutReceiptService(service.DocumentClientHttpContext, service.PdfTemplateReader,
                NEnv.ClientCfgCore, NEnv.EnvSettings, service.GetEncryptionService(GetCurrentUserMetadata()));
            var documentClient = Service.DocumentClientHttpContext;
            paymentReceiptService.WithDocumentArchiveRenderer(renderPayoutReceipt =>
            {
                foreach (var creditNrsGroup in creditNrs.ToArray().SplitIntoGroupsOfN(200))
                {
                    using (var context = new CreditContextExtended(GetCurrentUserMetadata(), Clock))
                    {
                        Dictionary<string, Dictionary<string, object>> paymentReceiptPrintContextsByCreditNr = null;
                        if (paymentReceiptService.IsFeatureActive())
                        {
                            paymentReceiptPrintContextsByCreditNr = paymentReceiptService.CreatePrintContexts(creditNrsGroup.ToHashSet(), context);
                        }

                        var creditDataByCreditNr = context
                            .CreditHeaders
                            .Where(x => creditNrsGroup.Contains(x.CreditNr))
                            .Select(x => new
                            {
                                x.CreditNr,
                                MainApplicantCustomerId = x
                                    .CreditCustomers
                                    .Where(y => y.ApplicantNr == 1)
                                    .Select(y => y.CustomerId)
                                    .FirstOrDefault(),
                                x.CreditType

                            })
                            .ToDictionary(x => x.CreditNr, x => x);

                        foreach (var creditNr in creditNrsGroup)
                        {
                            var creditData = creditDataByCreditNr[creditNr];

                            var templateText = creditCreatedSecureMessageTemplates["templateText"];

                            string attachmentArchiveKey = null;
                            if (paymentReceiptService.IsFeatureActive())
                            {
                                attachmentArchiveKey = renderPayoutReceipt((
                                    Context: paymentReceiptPrintContextsByCreditNr[creditData.CreditNr],
                                    RenderedPdfFilename: $"Payout-Receipt-{creditData.CreditNr}.pdf"));
                            }

                            var messageId = customerClient.SendSecureMessage(
                                creditData.MainApplicantCustomerId,
                                creditData.CreditNr,
                                $"Credit_{creditData.CreditType}",
                                templateText,
                                true,
                                "markdown");

                            if (paymentReceiptService.IsFeatureActive())
                            {
                                customerClient.AttachArchiveDocumentToSecureMessage(messageId.Value, attachmentArchiveKey);
                            }
                        }
                    }
                }
            });
        }
    }
}