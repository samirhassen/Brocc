using Newtonsoft.Json;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.CreditStandard;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace nPreCredit.WebserviceMethods.SharedStandard
{
    public class EditLoanStandardApplicantMethod : TypedWebserviceMethod<EditLoanStandardApplicantMethod.Request, EditLoanStandardApplicantMethod.Response>
    {
        public override string Path => "LoanStandard/Edit-Applicant";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled || NEnv.IsStandardMortgageLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var resolver = requestContext.Resolver();
            var s = resolver.Resolve<IComplexApplicationListService>();

            ComplexApplicationListOperation CreateEdit(string itemName, string newValue) =>
                new ComplexApplicationListOperation
                {
                    ApplicationNr = request.ApplicationNr,
                    Nr = request.ApplicantNr.Value,
                    ListName = "Applicant",
                    IsDelete = string.IsNullOrWhiteSpace(newValue),
                    ItemName = itemName,
                    UniqueValue = string.IsNullOrWhiteSpace(newValue) ? null : newValue
                };

            string NullableBool(bool? b) => b.HasValue ? (b.Value ? "true" : "false") : "";

            s.ChangeList(Enumerables.SkipNulls(
                CreateEdit("isPartOfTheHousehold", NullableBool(request.IsPartOfTheHousehold)),
                CreateEdit("employment", request.Employment),
                CreateEdit("employer", request.Employer),
                CreateEdit("employerPhone", request.EmployerPhone),
                CreateEdit("employedSince", request.EmployedSince),
                CreateEdit("employedTo", request.EmployedTo),
                CreateEdit("claimsToBePep", NullableBool(request.ClaimsToBePep)),
                CreateEdit("marriage", request.Marriage),
                CreateEdit("incomePerMonthAmount", request.IncomePerMonthAmount?.ToString(CultureInfo.InvariantCulture)),
                CreateEdit("hasConsentedToShareBankAccountData", NullableBool(request.HasConsentedToShareBankAccountData)),
                CreateEdit("hasConsentedToCreditReport", NullableBool(request.HasConsentedToCreditReport)),
                CreateEdit("claimsToHaveKfmDebt", NullableBool(request.ClaimsToHaveKfmDebt)),
                CreateEdit("claimsToBeGuarantor", NullableBool(request.ClaimsToBeGuarantor)),
                CreateEdit("hasLegalOrFinancialGuardian", NullableBool(request.HasLegalOrFinancialGuardian))
            ).ToList());

            return new Response
            {

            };
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            [Required]
            public int? ApplicantNr { get; set; }

            public bool? IsPartOfTheHousehold { get; set; }
            public bool? ClaimsToBePep { get; set; }
            [EnumCode(EnumType = typeof(CreditStandardCivilStatus.Code))]
            public string Marriage { get; set; }
            public int? IncomePerMonthAmount { get; set; }
            public bool? HasConsentedToShareBankAccountData { get; set; }
            public bool? HasConsentedToCreditReport { get; set; }
            public bool? ClaimsToHaveKfmDebt { get; set; }
            [EnumCode(EnumType = typeof(CreditStandardEmployment.Code))]
            public string Employment { get; set; }
            public string Employer { get; set; }
            public string EmployerPhone { get; set; }
            [DateWithoutTime]
            public string EmployedSince { get; set; }
            [DateWithoutTime]
            public string EmployedTo { get; set; }
            public bool? HasLegalOrFinancialGuardian { get; set; }
            public bool? ClaimsToBeGuarantor { get; set; }
        }

        public class Response
        {

        }
    }
}