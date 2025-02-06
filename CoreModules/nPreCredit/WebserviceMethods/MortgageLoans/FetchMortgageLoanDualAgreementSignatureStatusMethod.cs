using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.Clients;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.MortgageLoans;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.WebserviceMethods.MortgageLoans
{
    public class FetchMortgageLoanDualAgreementSignatureStatusMethod : TypedWebserviceMethod<FetchMortgageLoanDualAgreementSignatureStatusMethod.Request, FetchMortgageLoanDualAgreementSignatureStatusMethod.Response>
    {
        public override string Path => "MortgageLoan/Fetch-Dual-Agreement-SignatureStatus";

        public override bool IsEnabled => NEnv.IsOnlyNonStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            if (string.IsNullOrWhiteSpace(request.ApplicationNr) && string.IsNullOrWhiteSpace(request.Token))
            {
                return Error("ApplicationNr or Token required", errorCode: "applicationNrOrTokenRequired");
            }

            const string ListName = "DualAgreementSignatureTokens";
            string applicationNr;
            using (var context = new PreCreditContext())
            {
                if (!string.IsNullOrWhiteSpace(request.Token))
                {
                    var applicationNrs = context
                        .ComplexApplicationListItems
                        .Where(x => x.ListName == ListName && x.Nr == 1 && x.IsRepeatable && x.ItemName == "tokens" && x.ItemValue == request.Token)
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

            var response = InitResponse(r.Resolve<IMortgageLoanWorkflowService>(), ai);

            if (!response.IsPendingSignatures)
            {
                return response;
            }

            var applicants = infoService.GetApplicationApplicants(applicationNr);
            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                var customerIds = applicants
                    .AllConnectedCustomerIdsWithRoles
                    .Where(x => x.Value.Contains("Applicant") || x.Value.Contains("ApplicationObject"))
                    .Select(x => x.Key)
                    .ToHashSet();

                var model = EnsureTokens(applicationNr, ListName, customerIds, context);

                response.SignatureTokenByCustomerId = model.SignatureTokenByCustomerId;

                if (request.ReturnTokenSigningUrl.GetValueOrDefault() && !string.IsNullOrWhiteSpace(request.Token))
                {
                    var customerId = model.SignatureTokenByCustomerId.Single(x => x.Value == request.Token).Key;
                    if (NEnv.SignatureProvider != SignatureProviderCode.signicat)
                        throw new NotImplementedException();

                    var appDocumentService = r.Resolve<IApplicationDocumentService>();
                    var existingSignedAgreementsForCustomer = appDocumentService.FetchForApplication(applicationNr,
                            new List<string> { CreditApplicationDocumentTypeCode.SignedAgreement.ToString() })
                        .Where(agr => agr.CustomerId == customerId).ToList();
                    if (existingSignedAgreementsForCustomer.Any())
                    {
                        response.CustomerHasAlreadySigned = true;
                        return response;
                    }

                    var ac = r.Resolve<IMortgageLoanDualAgreementService>();
                    MortgageLoanDualAgreementService.CustomerModel customer = null;
                    var printContext = ac.GetPrintContext(ai, customerId, observeData: d =>
                        {
                            customer = d.Customers?.Opt(customerId);
                        });
                    var agreementData = ac.CreateAgreementPdf(printContext);

                    var sc = SignicatSigningClientFactory.CreateClient();
                    var sr = NEnv.ServiceRegistry;
                    var session = sc.StartSingleDocumentSignatureSession(new SignicatSigningClient.StartSingleDocumentSignatureSessionRequest
                    {
                        PdfBytesBase64 = Convert.ToBase64String(agreementData.ToArray()),
                        SigningCustomersByApplicantNr = new Dictionary<int, SignicatSigningClient.StartSingleDocumentSignatureSessionRequest.Customer>
                        {
                            { 1, new SignicatSigningClient.StartSingleDocumentSignatureSessionRequest.Customer
                                {
                                    ApplicantNr = 1,
                                    CivicRegNr = customer.CivicRegNr,
                                    FirstName = customer.FirstName,
                                    LastName = customer.LastName,
                                    SignicatLoginMethod = sc.GetElectronicIdLoginMethod()
                                }
                            }
                        },
                        CustomData = new Dictionary<string, string>
                        {
                            { "SignatureSessionType", Controllers.Api.ApiSignaturePostbackController.SignatureSessionTypeCode.DualMortgageLoanAgreementSignatureV1.ToString() },
                            { "ApplicationNr", applicationNr },
                            { "CustomerId", customerId.ToString() }
                        },
                        PdfDisplayFileName = "Agreement.pdf",
                        RedirectAfterSuccessUrl = sr.External.ServiceUrl("nCustomerPages", "/a/#/signature/success").ToString(),
                        RedirectAfterFailedUrl = sr.External.ServiceUrl("nCustomerPages", "/a/#/signature/failed").ToString(),
                        ServerToServerCallbackUrl = Controllers.Api.ApiSignaturePostbackController.GetCallbackUrl().ToString()
                    });
                    response.TokenSigningUrl = session.GetActiveSignatureUrlForApplicant(1);
                }
            }

            return response;
        }

        private Response InitResponse(IMortgageLoanWorkflowService wf, ApplicationInfoModel ai)
        {
            var currentListName = wf.GetCurrentListName(ai.ListNames);
            if (!wf.TryDecomposeListName(currentListName, out var names))
            {
                throw new Exception("Invalid application. Current listname is broken.");
            }
            var currentStepName = names.Item1;

            var signAgreementStep = wf.Model.FindStepByCustomData(x => x?.IsSignAgreement == "yes", new { IsSignAgreement = "" });

            if (signAgreementStep == null)
                throw new Exception("There needs to be a step in the workflow with CustomData item IsSignAgreement = \"yes\"");

            var isSignAgreementStep = currentStepName == signAgreementStep.Name;

            return new Response
            {
                IsPendingSignatures = isSignAgreementStep && ai.IsActive && ai.HasLockedAgreement && !ai.IsFinalDecisionMade,
                IsSignatureStepAccepted = wf.IsStepStatusAccepted(signAgreementStep.Name, ai.ListNames)
            };
        }

        public static DualAgreementSignatureTokensModel EnsureTokens(string applicationNr, string listName, HashSet<int> customerIds, PreCreditContextExtended context)
        {
            var currentModelRaw = context
                .ComplexApplicationListItems
                .Where(x => x.ApplicationNr == applicationNr && x.Nr == 1 && x.ItemName == "model" && x.ListName == listName && !x.IsRepeatable)
                .Select(x => x.ItemValue)
                .FirstOrDefault();
            DualAgreementSignatureTokensModel model = null;

            var hasNewTokens = false;
            if (currentModelRaw != null)
            {
                model = JsonConvert.DeserializeObject<DualAgreementSignatureTokensModel>(currentModelRaw);
            }
            if (model == null)
            {
                model = new DualAgreementSignatureTokensModel
                {
                    SignatureTokenByCustomerId = new Dictionary<int, string>()
                };
            }

            foreach (var customerId in customerIds)
            {
                if (!model.SignatureTokenByCustomerId.ContainsKey(customerId))
                {
                    hasNewTokens = true;
                    model.SignatureTokenByCustomerId[customerId] = OneTimeTokenGenerator.SharedInstance.GenerateUniqueToken();
                }
            }

            if (hasNewTokens)
            {
                var changes = new List<ComplexApplicationListOperation>();
                changes.Add(new ComplexApplicationListOperation
                {
                    ApplicationNr = applicationNr,
                    ListName = listName,
                    Nr = 1,
                    ItemName = "tokens",
                    RepeatedValue = model.SignatureTokenByCustomerId.Values.ToList()
                });
                changes.Add(new ComplexApplicationListOperation
                {
                    ApplicationNr = applicationNr,
                    ListName = listName,
                    Nr = 1,
                    ItemName = "model",
                    UniqueValue = JsonConvert.SerializeObject(model)
                });

                ComplexApplicationListService.ChangeListComposable(changes, context);

                context.SaveChanges();
            }

            return model;
        }

        public class Request
        {
            public string ApplicationNr { get; set; }
            public string Token { get; set; }
            public bool? ReturnTokenSigningUrl { get; set; }
        }

        public class Response
        {
            public bool IsSignatureStepAccepted { get; set; }
            public bool IsPendingSignatures { get; set; }
            public bool CustomerHasAlreadySigned { get; set; }
            public Dictionary<int, string> SignatureTokenByCustomerId { get; set; }
            public string TokenSigningUrl { get; set; }
        }

        public class DualAgreementSignatureTokensModel
        {
            public Dictionary<int, string> SignatureTokenByCustomerId { get; set; }
        }
    }
}