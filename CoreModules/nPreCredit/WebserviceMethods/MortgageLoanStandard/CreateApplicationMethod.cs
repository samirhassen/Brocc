using nPreCredit.Code.Plugins;
using nPreCredit.Code.Services;
using nPreCredit.WebserviceMethods.SharedStandard;
using NTech.Banking.CivicRegNumbers;
using NTech.Banking.PluginApis.CreateApplication;
using NTech.Core;
using NTech.Core.Module.Shared.Clients;
using NTech.Services.Infrastructure.MortgageLoanStandard;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace nPreCredit.WebserviceMethods.MortgageLoanStandard
{
    public class CreateApplicationMethod : TypedWebserviceMethod<MortgageLoanStandardApplicationCreateRequest, CreateApplicationMethod.Response>
    {
        public override string Path => "MortgageLoanStandard/Create-Application";

        public override bool IsEnabled => NEnv.IsStandardMortgageLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, MortgageLoanStandardApplicationCreateRequest request)
        {
            ValidateUsingAnnotations(request);

            if (!(request.Applicants.Count == 1 || request.Applicants.Count == 2))
                return Error("Applicants must have one or two members", errorCode: "invalidApplicantCount");

            if (!string.IsNullOrWhiteSpace(request?.Meta?.ProviderName) && NEnv.GetAffiliateModel(request.Meta.ProviderName, allowMissing: true) == null)
                return Error("Meta.ProviderName unknown provider used", errorCode: "unknownProviderName");

            if ((request.Purchase == null) == (request.ChangeExistingLoan == null))
                return Error("Exactly one of Purchase and ChangeExistingLoan must be included", errorCode: "purchaseOrChangeExistingRequired");
            if (request.HouseholdChildren != null && request.NrOfHouseholdChildren.HasValue && request.HouseholdChildren.Count != request.NrOfHouseholdChildren.Value)
                return Error("When both HouseholdChildren and NrOfHouseholdChildren are used the counts must match", errorCode: "householdChildrenMismatch");

            var clientConfig = NEnv.ClientCfg;

            CreateApplicationRequestModelExtended.CheckForDuplicateCivicRegNrs(request?.Applicants?.Select(x => x.CivicRegNr), NEnv.ClientCfgCore);

            var resolver = requestContext.Resolver();
            var workflowSevice = resolver.Resolve<IMortgageLoanStandardWorkflowService>();
            var createApplicationContext = new PluginApplicationRequestTranslatorBase.Context(
                resolver.Resolve<ICoreClock>(),
                resolver.Resolve<ICustomerClient>(),
                workflowSevice,
                resolver.Resolve<IKeyValueStoreService>(),
                resolver.Resolve<ApplicationDataSourceService>(),
                "ML",
                () => NEnv.GetAffiliateModels(),
                resolver.Resolve<IPreCreditContextFactoryService>());

            var createRequest = TranslateRequest(request, createApplicationContext, NEnv.BaseCivicRegNumberParser);

            if (!NEnv.IsProduction)
                createApplicationContext.SetKeyValueStoreValue(createRequest.ApplicationNr, "ApplicationRequestJson", requestContext.RequestJson);

            var application = resolver.Resolve<SharedCreateApplicationService>().CreateApplication(
                createRequest, CreditApplicationTypeCode.mortgageLoan, workflowSevice, CreditApplicationEventCode.CreditApplicationCreated);

            return new Response
            {
                ApplicationNr = application.ApplicationNr
            };
        }

        private CreateApplicationRequestModelExtended TranslateRequest(MortgageLoanStandardApplicationCreateRequest request, IApplicationCreationContext creationContext, CivicRegNumberParser civicRegNumberParser)
        {
            var createRequest = new CreateApplicationRequestModelExtended();

            var applicationItems = new Dictionary<string, string>();
            var applicantItems = new Dictionary<int, Dictionary<string, string>>();

            void AddApplicationItem(string name, string value) => applicationItems[name] = value;

            var objectTypeCode = request.ObjectTypeCode.NormalizeNullOrWhitespace();

            createRequest.ApplicationNr = creationContext.GenerateNewApplicationNr();
            createRequest.ProviderName = request.Meta.ProviderName;
            createRequest.NrOfApplicants = request.Applicants.Count;
            AddApplicationItem("objectTypeCode", objectTypeCode);
            AddApplicationItem("objectAddressStreet", request.ObjectAddressStreet.NormalizeNullOrWhitespace());
            AddApplicationItem("objectAddressZipcode", request.ObjectAddressZipcode.NormalizeNullOrWhitespace());
            AddApplicationItem("objectAddressCity", request.ObjectAddressCity.NormalizeNullOrWhitespace());
            AddApplicationItem("objectAddressMunicipality", request.ObjectAddressMunicipality.NormalizeNullOrWhitespace());
            AddApplicationItem("objectAddressCounty", request.ObjectAddressCounty.NormalizeNullOrWhitespace());
            AddApplicationItem("objectMonthlyFeeAmount", request.ObjectMonthlyFeeAmount?.ToString(CultureInfo.InvariantCulture));
            AddApplicationItem("objectLivingArea", request.ObjectLivingArea?.ToString(CultureInfo.InvariantCulture));
            AddApplicationItem("objectOtherMonthlyCostsAmount", request.ObjectOtherMonthlyCostsAmount?.ToString(CultureInfo.InvariantCulture));
            AddApplicationItem("outgoingChildSupportAmount", request.OutgoingChildSupportAmount?.ToString(CultureInfo.InvariantCulture));
            AddApplicationItem("incomingChildSupportAmount", request.IncomingChildSupportAmount?.ToString(CultureInfo.InvariantCulture));
            AddApplicationItem("childBenefitAmount", request.ChildBenefitAmount?.ToString(CultureInfo.InvariantCulture));

            if (objectTypeCode == MortgageLoanStandardApplicationCreateRequest.ObjectTypeCodeValue.seBrf.ToString())
                AddApplicationItem("seBrfApartmentNr", request.SeBrfApartmentNr.NormalizeNullOrWhitespace());

            if (request.Purchase != null)
            {
                var purchase = request.Purchase;
                AddApplicationItem("isPurchase", "true");
                AddApplicationItem("objectPriceAmount", purchase.ObjectPriceAmount?.ToString());
                //Price is the initial estimate when buying since we are 100% sure someone will buy it for this price ... they just did.
                AddApplicationItem("objectValueAmount", purchase.ObjectPriceAmount?.ToString());
                AddApplicationItem("ownSavingsAmount", purchase.OwnSavingsAmount?.ToString());
            }
            else //ChangeExistingLoan
            {
                var change = request.ChangeExistingLoan;
                AddApplicationItem("isPurchase", "false");
                AddApplicationItem("objectValueAmount", change.ObjectValueAmount?.ToString());
                AddApplicationItem("paidToCustomerAmount", change.PaidToCustomerAmount?.ToString());
                var rowNr = 1;
                foreach (var loan in change.MortgageLoansToSettle)
                {
                    createRequest.AddComplexApplicationItem("MortgageLoansToSettle", rowNr, new Dictionary<string, string>
                    {
                        { "exists", "true" },
                        { "currentDebtAmount", loan.CurrentDebtAmount?.ToString() },
                        { "shouldBeSettled", loan.ShouldBeSettled.HasValue ? (loan.ShouldBeSettled.Value ? "true" : "false") : null },
                        { "bankName", loan.BankName?.NormalizeNullOrWhitespace() },
                        { "currentMonthlyAmortizationAmount", loan.CurrentMonthlyAmortizationAmount?.ToString(CultureInfo.InvariantCulture) },
                        { "interestRatePercent", loan.InterestRatePercent?.ToString(CultureInfo.InvariantCulture) },
                        { "loanNumber", loan.LoanNumber.NormalizeNullOrWhitespace() }
                    }, null);
                    rowNr++;
                }
            }

            AddApplicationItem("providerApplicationId", request.ProviderApplicationId);

            var applicantNr = 1;
            void AddApplicantItem(string name, string value)
            {
                if (!applicantItems.ContainsKey(applicantNr))
                    applicantItems[applicantNr] = new Dictionary<string, string>();
                applicantItems[applicantNr][name] = value;
            }

            foreach (var applicant in request.Applicants)
            {
                var civicRegNr = civicRegNumberParser.Parse(applicant.CivicRegNr);
                var customerId = creationContext.CreateOrUpdatePerson(civicRegNr, new Dictionary<string, string>
                    {
                        { "firstName", applicant.FirstName },
                        { "lastName", applicant.LastName },
                        { "phone", applicant.Phone },
                        { "email", applicant.Email }
                    },
                    false, createRequest.ApplicationNr);

                createRequest.SetCustomerListMember("Applicant", customerId);

                AddApplicantItem("customerId", customerId.ToString());

                AddApplicantItem("hasConsentedToShareBankAccountData", applicant.HasConsentedToShareBankAccountData?.ToString()?.ToLower());
                AddApplicantItem("hasConsentedToCreditReport", applicant.HasConsentedToCreditReport?.ToString()?.ToLower());
                AddApplicantItem("employment", applicant.Employment);
                AddApplicantItem("employer", applicant.Employer);
                AddApplicantItem("employedSince", DateWithoutTimeAttribute.ParseDateWithoutTimeOrNull(applicant.EmployedSince, allowMonthOnly: true)?.ToString("yyyy-MM"));
                AddApplicantItem("employedTo", DateWithoutTimeAttribute.ParseDateWithoutTimeOrNull(applicant.EmployedTo, allowMonthOnly: true)?.ToString("yyyy-MM"));
                AddApplicantItem("employerPhone", applicant.EmployerPhone);
                AddApplicantItem("incomePerMonthAmount", applicant.IncomePerMonthAmount?.ToString(CultureInfo.InvariantCulture));
                //Main applicant is always part of the household. The co applicant can be explicity opted out but is otherwise assumed to be part of the household.
                AddApplicantItem("isPartOfTheHousehold", applicantNr == 1 ? "true" : (applicant.IsPartOfTheHousehold == false ? "false" : "true"));

                applicantNr++;
            }

            if (request.HouseholdChildren == null && request.NrOfHouseholdChildren.HasValue)
                request.HouseholdChildren = Enumerable.Range(1, request.NrOfHouseholdChildren.Value).Select(x => new MortgageLoanStandardApplicationCreateRequest.ChildModel
                {
                    Exists = true
                }).ToList();

            if (request.HouseholdChildren != null)
            {
                var rowNr = 1;
                foreach (var child in request.HouseholdChildren)
                {
                    createRequest.AddComplexApplicationItem("HouseholdChildren", rowNr, new Dictionary<string, string>
                    {
                        { "exists", "true" },
                        { "ageInYears", child.AgeInYears?.ToString(CultureInfo.InvariantCulture) },
                        { "sharedCustody", child.SharedCustody?.ToString()?.ToLowerInvariant() }
                    }, null);
                    rowNr++;
                }
            }

            if (request.LoansToSettle != null)
            {
                var rowNr = 1;
                foreach (var loan in request.LoansToSettle)
                {
                    createRequest.AddComplexApplicationItem("LoansToSettle", rowNr, new Dictionary<string, string>
                    {
                        { "exists", "true" },
                        { "loanType", loan.LoanType.NormalizeNullOrWhitespace() },
                        { "currentDebtAmount", loan.CurrentDebtAmount?.ToString(CultureInfo.InvariantCulture) },
                        { "monthlyCostAmount", loan.MonthlyCostAmount?.ToString(CultureInfo.InvariantCulture) }
                    }, null);
                    rowNr++;
                }
            }

            createRequest.AddComplexApplicationItem("Application", 1, applicationItems, null);
            foreach (var applicant in applicantItems)
            {
                createRequest.AddComplexApplicationItem("Applicant", applicant.Key, applicant.Value, null);
            }

            createRequest.SetComment("Application created", customerIpAddress: request.CustomerExternalIpAddress);

            return createRequest;
        }

        public class Response
        {
            public string ApplicationNr { get; set; }
        }
    }
}