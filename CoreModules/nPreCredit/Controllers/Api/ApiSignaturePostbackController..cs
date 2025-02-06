using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.Clients;
using nPreCredit.Code.ElectronicSignatures;
using nPreCredit.Code.Services;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [RoutePrefix("api")]
    public class ApiSignaturePostbackController : NController
    {
        private readonly IApplicationDocumentService applicationDocumentService;
        private readonly IApplicationCommentService applicationCommentService;

        private readonly Lazy<nPreCreditClient> preCreditClient = new Lazy<nPreCreditClient>(() =>
        {
            var unp = NEnv.ApplicationAutomationUsernameAndPassword;
            var token = NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry, unp.Item1, unp.Item2);
            return new nPreCreditClient(token.GetToken);
        });

        public ApiSignaturePostbackController(IApplicationDocumentService applicationDocumentService, IApplicationCommentService commentService)
        {
            this.applicationDocumentService = applicationDocumentService;
            applicationCommentService = commentService;
        }

        [Route("Signatures/Signal-Session-Event")]
        [AllowAnonymous]
        [HttpPost]
        public ActionResult SignalSessionEvent(string sessionId, string providerName)
        {
            //External callbacks end up here. We pass it on to an internal method with a user so it can act
            //The idea of why this is safe is that the only thing it does is get the session and use it's state to act
            //Where the session is gotten from cannot be manipulated by changing the id or providerName so this should be safe.

            if (sessionId == null || providerName == null)
                return new HttpStatusCodeResult(HttpStatusCode.OK);

            preCreditClient.Value.SignalSessionEventAuthorized(sessionId, providerName);

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        [Route("Signatures/Signal-Session-Event-Authorized")]
        [NTechAuthorize]
        [HttpPost]
        public ActionResult SignalSessionEventAuthorized(string sessionId, string providerName)
        {
            var provider = new ElectronicSignatureProvider(Clock);

            if (provider.TryHandle(sessionId, Resolver))
            {
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }

            if (providerName == SignatureProviderCode.signicat.ToString() && !string.IsNullOrWhiteSpace(sessionId))
            {
                var c = SignicatSigningClientFactory.CreateClient();
                var s = c.GetSignatureSession(sessionId);

                var sessionType = s.CustomData?.Opt("SignatureSessionType");
                if (sessionType == SignatureSessionTypeCode.CompanyLoanInitialAgreementSignatureV1.ToString())
                {
                    var applicationNr = s.CustomData?.Opt("ApplicationNr");
                    if (applicationNr != null)
                    {
                        var ai = Resolver.Resolve<ApplicationInfoService>()
                            .GetApplicationInfo(applicationNr);

                        Resolver.Resolve<Code.Services.CompanyLoans.ICompanyLoanAgreementSignatureService>()
                            .OnSignatureEvent(ai);
                    }
                }
                else if (sessionType == SignatureSessionTypeCode.DualMortgageLoanAgreementSignatureV1.ToString())
                {
                    var applicationNr = s.CustomData?.Opt("ApplicationNr");
                    var customerIdRaw = s.CustomData.Opt("CustomerId");
                    if (!string.IsNullOrWhiteSpace(s.SignedDocumentKey) && !string.IsNullOrWhiteSpace(applicationNr) && !string.IsNullOrWhiteSpace(customerIdRaw))
                    {
                        var signedDocument = c.GetDocument(s.SignedDocumentKey);
                        var dc = Resolver.Resolve<IDocumentClient>();
                        var filename = signedDocument.DocumentDownloadName ?? "Agreement.pdf";
                        var documentArchiveKey = dc.ArchiveStore(
                            Convert.FromBase64String(signedDocument.DocumentDataBase64),
                            signedDocument.DocumentMimeType,
                            signedDocument.DocumentDownloadName ?? "Agreement.pdf");

                        using (var context = new PreCreditContextExtended(this.NTechUser, this.Clock))
                        {
                            var customerIdInt = int.Parse(customerIdRaw);

                            var existingSignedAgreementsForCustomer = applicationDocumentService.FetchForApplication(applicationNr,
                                new List<string> { CreditApplicationDocumentTypeCode.SignedAgreement.ToString() })
                                .Where(agr => agr.CustomerId == customerIdInt).ToList();

                            if (existingSignedAgreementsForCustomer.Any())
                            {
                                // Customer managed to sign the agreement again, remove the earlier one so only the new one is active. 
                                foreach (var existing in existingSignedAgreementsForCustomer)
                                {
                                    applicationDocumentService.RemoveDocument(applicationNr, existing.DocumentId);

                                    var attachment = CommentAttachment.CreateFileFromArchiveKey(documentArchiveKey,
                                        "application/pdf", "OldAgreement.pdf");
                                    applicationCommentService.TryAddComment(applicationNr,
                                        "Old signed agreement removed ", "OldSignedAgreementRemoved",
                                        attachment, out _);
                                }
                            }

                            context.CreateAndAddApplicationDocument(documentArchiveKey,
                                filename,
                                CreditApplicationDocumentTypeCode.SignedAgreement,
                                applicationNr: applicationNr,
                                customerId: customerIdInt);

                            context.CreateAndAddComment("Agreement has been signed by customer ", "AgreementHasBeenSigned", applicationNr);
                            context.SaveChanges();
                        }
                    }
                }
                else if (sessionType == SignatureSessionTypeCode.DualMortgageLoanApplicationSignatureV1.ToString())
                {
                    var applicationNr = s.CustomData?.Opt("ApplicationNr");
                    var applicantNrRaw = s.CustomData.Opt("ApplicantNr");
                    if (!string.IsNullOrWhiteSpace(s.SignedDocumentKey) && !string.IsNullOrWhiteSpace(applicationNr) && !string.IsNullOrWhiteSpace(applicantNrRaw))
                    {
                        var signedDocument = c.GetDocument(s.SignedDocumentKey);
                        var dc = Resolver.Resolve<IDocumentClient>();
                        var filename = signedDocument.DocumentDownloadName ?? "Agreement.pdf";
                        var documentArchiveKey = dc.ArchiveStore(
                            Convert.FromBase64String(signedDocument.DocumentDataBase64),
                            signedDocument.DocumentMimeType,
                            filename);

                        using (var context = new PreCreditContextExtended(this.NTechUser, this.Clock))
                        {
                            context.CreateAndAddApplicationDocument(documentArchiveKey,
                                filename,
                                CreditApplicationDocumentTypeCode.SignedApplicationAndPOA,
                                applicationNr: applicationNr,
                                applicantNr: int.Parse(applicantNrRaw));
                            context.SaveChanges();
                        }
                    }
                }
                else if (sessionType == SignatureSessionTypeCode.DualMortgageLoanApplicationSignatureV2.ToString())
                {
                    var applicationNr = s.CustomData?.Opt("ApplicationNr");
                    if (applicationNr == null)
                        return new HttpStatusCodeResult(HttpStatusCode.OK);

                    var sessionBankNameByDocumentId = new Lazy<Dictionary<string, string>>(() => JsonConvert.DeserializeObject<Dictionary<string, string>>(s.CustomData["BankNameByDocumentId"]));
                    var applicantNr = int.Parse(s.CustomData["ApplicantNr"]);
                    var bankNamesByApplicantNr = Resolver.Resolve<Code.Services.MortgageLoans.IMortgageLoanDualApplicationAndPoaService>().GetPoaBankNames(applicationNr);
                    var dc = LegacyServiceClientFactory.CreateDocumentClient(LegacyHttpServiceHttpContextUser.SharedInstance, NEnv.ServiceRegistry);
                    using (var context = new PreCreditContextExtended(this.NTechUser, this.Clock))
                    {
                        var applicationDocumentId = s.CustomData["ApplicationDocumentId"];
                        c.DownloadAndHandleSignedDocument(applicationDocumentId, s, dc, (archiveKey, filename) => context.CreateAndAddApplicationDocument(archiveKey,
                            filename,
                            CreditApplicationDocumentTypeCode.SignedApplication,
                            applicationNr: applicationNr,
                            applicantNr: applicantNr));

                        foreach (var actualBankName in bankNamesByApplicantNr.Opt(applicantNr) ?? new List<string>())
                        {
                            var documentId = sessionBankNameByDocumentId?.Value?.Where(x => x.Value == actualBankName)?.SingleOrDefault().Key;
                            if (documentId != null)
                            {
                                c.DownloadAndHandleSignedDocument(documentId, s, dc, (archiveKey, filename) => context.CreateAndAddApplicationDocument(archiveKey,
                                    filename,
                                    CreditApplicationDocumentTypeCode.SignedPowerOfAttorney,
                                    applicationNr: applicationNr,
                                    applicantNr: applicantNr,
                                    documentSubType: actualBankName));
                            }
                        }
                        context.SaveChanges();
                    }
                }
            }
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        public enum SignatureSessionTypeCode
        {
            CompanyLoanInitialAgreementSignatureV1,
            DualMortgageLoanAgreementSignatureV1,
            DualMortgageLoanApplicationSignatureV1,
            DualMortgageLoanApplicationSignatureV2,
            UnsecuredLoanAgreementSignatureV1,
            ManualSignature,
            UnsecuredLoanStandardAgreementSignatureV1,
            DirectDebitConsentSignatureV1
        }

        public static Uri GetCallbackUrl()
        {
            return NEnv.ServiceRegistry.Internal.ServiceUrl(NEnv.CurrentServiceName, "api/Signatures/Signal-Session-Event");
        }
    }
}