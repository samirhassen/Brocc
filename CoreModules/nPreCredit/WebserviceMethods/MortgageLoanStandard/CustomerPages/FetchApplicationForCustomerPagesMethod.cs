using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.CreditStandard;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.MortgageLoansStandard
{
    public class FetchApplicationForCustomerPagesMethod : TypedWebserviceMethod<FetchApplicationForCustomerPagesMethod.Request, FetchApplicationForCustomerPagesMethod.Response>
    {
        public override string Path => "MortgageLoanStandard/CustomerPages/Fetch-Application";

        public override bool IsEnabled => NEnv.IsStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var customerClient = new PreCreditCustomerClient();
            var settings = customerClient.LoadSettings("enableDisableSecureMessages");

            using (var context = new PreCreditContext())
            {
                var customerId = request.CustomerId.Value;

                var complexListsToGet = new string[] { "Application" };
                var signedAgreementDocumentType = CreditApplicationDocumentTypeCode.SignedAgreement.ToString();

                var application = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == request.ApplicationNr && x.CustomerListMemberships.Any(y => y.ListName == "Applicant" && y.CustomerId == customerId) && !x.ArchivedDate.HasValue)
                    .OrderBy(x => x.ApplicationDate)
                    .ThenBy(x => x.ApplicationNr)
                    .Select(x => new
                    {
                        CreditNr = x
                            .ComplexApplicationListItems
                            .Where(y => y.ListName == "Application" && y.ItemName == "creditNr" && !y.IsRepeatable && y.Nr == 1 && x.IsFinalDecisionMade)
                            .Select(y => y.ItemValue)
                            .FirstOrDefault(),
                        CurrentCreditDecisionItems = x.CurrentCreditDecision.DecisionItems,
                        ComplexApplicationListItems = x.ComplexApplicationListItems.Where(y => complexListsToGet.Contains(y.ListName)),
                        SignedAgreementArchiveKey = x.Documents
                            .Where(y => !y.RemovedByUserId.HasValue && y.DocumentType == signedAgreementDocumentType)
                            .OrderByDescending(y => y.Id)
                            .Select(y => y.DocumentArchiveKey)
                            .FirstOrDefault()
                    })
                    .SingleOrDefault();

                if (application == null)
                    return Error("Not found", errorCode: "noSuchApplicationExists");

                var applicationInfoService = requestContext.Resolver().Resolve<ApplicationInfoService>();

                var ai = applicationInfoService.GetApplicationInfo(request.ApplicationNr);
                Lazy<ApplicationApplicantsModel> applicants = new Lazy<ApplicationApplicantsModel>(() => applicationInfoService.GetApplicationApplicants(request.ApplicationNr));

                var workflowService = requestContext.Resolver().Resolve<IMortgageLoanStandardWorkflowService>();

                var applicationRow = ComplexApplicationList.CreateListFromFlattenedItems("Application", application.ComplexApplicationListItems.ToList()).GetRow(1, true);

                var response = new Response.ApplicationModel
                {
                    ApplicationNr = ai.ApplicationNr,
                    ApplicationDate = ai.ApplicationDate,
                    IsActive = ai.IsActive,
                    CreditNr = application.CreditNr,
                    IsCancelled = ai.IsCancelled,
                    IsFinalDecisionMade = ai.IsFinalDecisionMade,
                    IsRejected = ai.IsRejected,
                    IsKycTaskActive = workflowService.AreAllStepsBeforeComplete(MortgageLoanStandardWorkflowService.WaitingForAdditionalInfoStep.Name, ai.ListNames),
                    IsKycTaskApproved = applicationRow.GetUniqueItemBoolean("hasAnsweredKycQuestions") ?? false
                };

                var decision = CreditDecisionItemsToRow(application.CurrentCreditDecisionItems);
                if (decision.GetUniqueItemBoolean("isOffer") == true)
                {
                    response.LatestAcceptedDecision = new Response.LatestAcceptedDecisionModel
                    {
                        IsFinal = decision.GetUniqueItem("decisionType", require: true) == "Final",
                        LoanAmount = decision.GetUniqueItemInteger("loanAmount", require: true).Value
                    };
                }

                response.ObjectSummary = new Response.ObjectSummaryModel
                {
                    IsApartment = applicationRow.GetUniqueItem("objectTypeCode") == "seBrf",
                    AddressApartmentNr = applicationRow.GetUniqueItem("seBrfApartmentNr"),
                    AddressSeTaxOfficeApartmentNr = applicationRow.GetUniqueItem("seTaxOfficeApartmentNr"),
                    AddressCity = applicationRow.GetUniqueItem("objectAddressCity"),
                    AddressStreet = applicationRow.GetUniqueItem("objectAddressStreet"),
                    AddressZipCode = applicationRow.GetUniqueItem("objectAddressZipcode"),
                    AddressMunicipality = applicationRow.GetUniqueItem("objectAddressMunicipality")
                };

                response.Enums = CreditStandardEnumService.Instance.GetApiEnums(language: NEnv.ClientCfg.Country.GetBaseLanguage());

                response.IsInactiveMessagingAllowed = settings["isInactiveMessagingAllowed"] == "true"; ;

                return new Response
                {
                    Application = response
                };
            }
        }

        private static ComplexApplicationList.Row CreditDecisionItemsToRow(IEnumerable<CreditDecisionItem> creditDecisionItems) =>
            ComplexApplicationList.CreateListFromFlattenedItems("FakeDecisionList", creditDecisionItems.Select(x => new ComplexApplicationListItemBase
            {
                ListName = "FakeDecisionList",
                Nr = 1,
                IsRepeatable = x.IsRepeatable,
                ItemName = x.ItemName,
                ItemValue = x.Value
            }).ToList()).GetRow(1, true);


        public class Request
        {
            [Required]
            public int? CustomerId { get; set; }

            [Required]
            public string ApplicationNr { get; set; }
        }

        public class Response
        {
            public ApplicationModel Application { get; set; }

            public class ApplicationModel
            {
                public string ApplicationNr { get; set; }
                public EnumsApiModel Enums { get; set; }
                public bool IsActive { get; set; }
                public DateTimeOffset ApplicationDate { get; set; }
                public bool IsCancelled { get; set; }
                public bool IsRejected { get; set; }
                public bool IsFinalDecisionMade { get; set; }
                public bool IsKycTaskActive { get; set; }
                public bool IsKycTaskApproved { get; set; }
                public string CreditNr { get; set; }
                public LatestAcceptedDecisionModel LatestAcceptedDecision { get; set; }
                public ObjectSummaryModel ObjectSummary { get; set; }
                public bool IsInactiveMessagingAllowed { get; set; }
            }

            public class LatestAcceptedDecisionModel
            {
                public bool IsFinal { get; set; }
                public int LoanAmount { get; set; }
            }

            public class ObjectSummaryModel
            {
                public bool IsApartment { get; set; }
                public string AddressStreet { get; set; }
                public string AddressZipCode { get; set; }
                public string AddressCity { get; set; }
                public string AddressMunicipality { get; set; }
                public string AddressApartmentNr { get; set; }
                public string AddressSeTaxOfficeApartmentNr { get; set; }
            }
        }
    }
}