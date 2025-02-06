using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.Clients;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.MortgageLoans;
using NTech.Core.Module.Shared.Clients;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.WebserviceMethods.MortgageLoans
{
    public class FetchMortgageLoanDualApplicationSignatureStatusMethod : TypedWebserviceMethod<FetchMortgageLoanDualApplicationSignatureStatusMethod.Request, FetchMortgageLoanDualApplicationSignatureStatusMethod.Response>
    {
        public override string Path => "MortgageLoan/Fetch-Dual-Application-SignatureStatus";

        public override bool IsEnabled => NEnv.IsOnlyNonStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            if (string.IsNullOrWhiteSpace(request.ApplicationNr) && string.IsNullOrWhiteSpace(request.Token))
            {
                return Error("ApplicationNr or Token required", errorCode: "applicationNrOrTokenRequired");
            }

            string applicationNr;
            var listName = FetchMortgageLoanApplicationAdditionalQuestionsStatusMethod.ApplicationSignatureTokenListName;
            using (var context = new PreCreditContext())
            {
                if (!string.IsNullOrWhiteSpace(request.Token))
                {
                    var applicationNrs = context
                        .ComplexApplicationListItems
                        .Where(x => x.ListName == listName && x.Nr == 1 && x.IsRepeatable && x.ItemName == "tokens" && x.ItemValue == request.Token)
                        .Select(x => x.ApplicationNr)
                        .ToHashSet();
                    if (applicationNrs.Count == 0)
                        return Error("No such application", errorCode: "noSuchApplication");
                    else if (applicationNrs.Count > 1)
                        throw new Exception($"System integrity error. The same token '{request.Token}' occurrs on multiple applications.");
                    else
                        applicationNr = applicationNrs.Single();
                }
                else
                    applicationNr = request.ApplicationNr;
            }

            var r = requestContext.Resolver();

            var infoService = r.Resolve<ApplicationInfoService>();

            var ai = infoService.GetApplicationInfo(applicationNr);
            if (ai == null)
                return Error("No such application", errorCode: "noSuchApplication");

            var applicants = infoService.GetApplicationApplicants(applicationNr);

            var response = InitResponse(r.Resolve<IMortgageLoanWorkflowService>(), ai, applicants);

            var bankNamesByApplicantNr = r.Resolve<IMortgageLoanDualApplicationAndPoaService>().GetPoaBankNames(applicationNr);

            response.BankNamesByApplicantNr = Enumerable
                .Range(1, applicants.NrOfApplicants)
                .ToDictionary(x => x, x => bankNamesByApplicantNr.Opt(x) ?? new List<string>());

            if (!response.IsPendingSignatures)
            {
                return response;
            }

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                var customerIds = applicants
                    .AllConnectedCustomerIdsWithRoles
                    .Where(x => x.Value.Contains("Applicant"))
                    .Select(x => x.Key)
                    .ToHashSet();

                var model = FetchMortgageLoanDualAgreementSignatureStatusMethod.EnsureTokens(applicationNr, FetchMortgageLoanApplicationAdditionalQuestionsStatusMethod.ApplicationSignatureTokenListName, customerIds, context);

                response.SignatureTokenByCustomerId = model.SignatureTokenByCustomerId;

                if (request.ReturnTokenSigningUrl.GetValueOrDefault() && !string.IsNullOrWhiteSpace(request.Token))
                {
                    var customerId = model.SignatureTokenByCustomerId.Single(x => x.Value == request.Token).Key;
                    var applicantNr = applicants.CustomerIdByApplicantNr.Single(x => x.Value == customerId).Key;

                    if (response.HasSignedByCustomerId[customerId])
                        return response;

                    if (NEnv.SignatureProvider != SignatureProviderCode.signicat)
                        throw new NotImplementedException();

                    var ac = r.Resolve<IMortgageLoanDualApplicationAndPoaService>();

                    var sc = SignicatSigningClientFactory.CreateClient();
                    var customerData = r.Resolve<ICustomerClient>().BulkFetchPropertiesByCustomerIdsD(new HashSet<int> { customerId }, "firstName", "lastName", "civicRegNr")?.Opt(customerId);
                    var sr = NEnv.ServiceRegistry;

                    var req = new SignicatSigningClient.StartMultiDocumentSignatureSessionRequest
                    {
                        Pdfs = new List<SignicatSigningClient.StartMultiDocumentSignatureSessionRequest.PdfModel>(),
                        SigningCustomersByApplicantNr = new Dictionary<int, SignicatSigningClient.StartMultiDocumentSignatureSessionRequest.Customer>
                        {
                            { 1, new SignicatSigningClient.StartMultiDocumentSignatureSessionRequest.Customer
                                {
                                    ApplicantNr = 1,
                                    CivicRegNr = customerData["civicRegNr"],
                                    FirstName = customerData.Opt("firstName"),
                                    LastName = customerData.Opt("lastName"),
                                    SignicatLoginMethod = sc.GetElectronicIdLoginMethod()
                                }
                            }
                        },
                        CustomData = new Dictionary<string, string>
                        {
                            { "SignatureSessionType", Controllers.Api.ApiSignaturePostbackController.SignatureSessionTypeCode.DualMortgageLoanApplicationSignatureV2.ToString() },
                            { "ApplicationNr", applicationNr },
                            { "CustomerId", customerId.ToString() },
                            { "ApplicantNr", applicantNr.ToString() }
                        },
                        RedirectAfterSuccessUrl = sr.External.ServiceUrl("nCustomerPages", "/a/#/token-login", Tuple.Create("t", $"aa_{request.Token}")).ToString(),
                        RedirectAfterFailedUrl = sr.External.ServiceUrl("nCustomerPages", "/a/#/signature/failed").ToString(),
                        ServerToServerCallbackUrl = Controllers.Api.ApiSignaturePostbackController.GetCallbackUrl().ToString()
                    };

                    var bankNameByDocumentId = new Dictionary<string, string>();

                    if (!ac.TryGetPrintContext(applicationNr, applicantNr, true, null, out var applicationContext, out var applicationMsg))
                    {
                        return Error(applicationMsg);
                    }
                    req.Pdfs.Add(new SignicatSigningClient.StartMultiDocumentSignatureSessionRequest.PdfModel
                    {
                        PdfBytesBase64 = Convert.ToBase64String(ac.CreateApplicationAndPoaDocument(applicationContext).ToArray()),
                        PdfDisplayFileName = "Application1.pdf",
                        PdfId = ApplicationDocumentId
                    });

                    var applicantBankNames = bankNamesByApplicantNr.Opt(applicantNr);
                    if (applicantBankNames != null)
                    {
                        foreach (var bank in applicantBankNames.Select((x, i) => new { BankName = x, DocumentId = $"Poa{i + 1}", }))
                        {
                            if (!ac.TryGetPrintContext(applicationNr, applicantNr, false, bank.BankName, out var bankContext, out var bankMsg))
                            {
                                return Error(bankMsg);
                            }
                            req.Pdfs.Add(new SignicatSigningClient.StartMultiDocumentSignatureSessionRequest.PdfModel
                            {
                                PdfBytesBase64 = Convert.ToBase64String(ac.CreateApplicationAndPoaDocument(bankContext).ToArray()),
                                PdfDisplayFileName = $"{bank.DocumentId}.pdf",
                                PdfId = bank.DocumentId
                            });
                            bankNameByDocumentId[bank.DocumentId] = bank.BankName;
                        }
                    }

                    req.CustomData["BankNameByDocumentId"] = JsonConvert.SerializeObject(bankNameByDocumentId);
                    req.CustomData["ApplicationDocumentId"] = ApplicationDocumentId;
                    req.SignedCombinations = req.Pdfs.Select(x => new SignicatSigningClient.StartMultiDocumentSignatureSessionRequest.SignedCombination
                    {
                        CombinationFileName = x.PdfDisplayFileName,
                        CombinationId = x.PdfId,
                        PdfIds = new List<string> { x.PdfId }
                    }).ToList();

                    var session = sc.StartMultiDocumentSignatureSession(req);
                    response.TokenSigningUrl = session.GetActiveSignatureUrlForApplicant(1);
                }
            }

            return response;
        }

        private const string ApplicationDocumentId = "Application1";

        private Response InitResponse(IMortgageLoanWorkflowService wf, ApplicationInfoModel ai, ApplicationApplicantsModel applicants)
        {
            var currentListName = wf.GetCurrentListName(ai.ListNames);
            if (!wf.TryDecomposeListName(currentListName, out var names))
            {
                throw new Exception("Invalid application. Current listname is broken.");
            }
            var currentStepName = names.Item1;

            var signApplicationStep = wf.Model.GetSignApplicationStepIfAny();

            if (signApplicationStep == null)
                throw new Exception("There needs to be a step in the workflow with CustomData item IsSignApplication = \"yes\"");

            var isSignApplicationStep = currentStepName == signApplicationStep.Name;

            using (var context = new PreCreditContext())
            {
                //NOTE: We dont need to track if they signed POA here as they cannot partially sign.
                var signedByApplicantNrs = context.CreditApplicationDocumentHeaders
                    .Where(x => x.ApplicationNr == ai.ApplicationNr && x.ApplicantNr.HasValue && x.DocumentType == CreditApplicationDocumentTypeCode.SignedApplication.ToString() && !x.RemovedByUserId.HasValue)
                    .Select(x => x.ApplicantNr)
                    .ToList()
                    .Select(x => x.Value)
                    .ToHashSet();

                return new Response
                {
                    ApplicationNr = ai.ApplicationNr,
                    IsPendingSignatures = isSignApplicationStep && ai.IsActive && !ai.IsFinalDecisionMade,
                    HasSignedByCustomerId = applicants
                        .AllConnectedCustomerIdsWithRoles
                        .Where(x => x.Value.Contains("Applicant"))
                        .ToDictionary(x => x.Key, x =>
                        {
                            var customerId = x.Key;
                            var applicantNr = applicants.CustomerIdByApplicantNr.Single(y => y.Value == customerId).Key;
                            return signedByApplicantNrs.Contains(applicantNr);
                        })
                };
            }
        }

        public class Request
        {
            public string ApplicationNr { get; set; }
            public string Token { get; set; }
            public bool? ReturnTokenSigningUrl { get; set; }
        }

        public class Response
        {
            public string ApplicationNr { get; set; }
            public bool IsPendingSignatures { get; set; }
            public Dictionary<int, List<string>> BankNamesByApplicantNr { get; set; }
            public Dictionary<int, bool> HasSignedByCustomerId { get; set; }
            public Dictionary<int, string> SignatureTokenByCustomerId { get; set; }
            public string TokenSigningUrl { get; set; }
        }
    }
}