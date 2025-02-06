using Newtonsoft.Json;
using nPreCredit.Code.Services;
using NTech.Banking.BankAccounts;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class EditUnsecuredLoanApplicationMethod : TypedWebserviceMethod<EditUnsecuredLoanApplicationMethod.Request, EditUnsecuredLoanApplicationMethod.Response>
    {
        public override string Path => "UnsecuredLoanStandard/Edit-Application";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled;
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
                    Nr = 1,
                    ListName = "Application",
                    IsDelete = string.IsNullOrWhiteSpace(newValue),
                    ItemName = itemName,
                    UniqueValue = string.IsNullOrWhiteSpace(newValue) ? null : newValue
                };

            IBankAccountNumber bankAccountNumber = null;
            if (!string.IsNullOrWhiteSpace(request.PaidToCustomerBankAccountNr))
            {
                bankAccountNumber = Request.bankAccountNrParser.Value.ParseFromStringWithDefaults(request.PaidToCustomerBankAccountNr, request.PaidToCustomerBankAccountNrType);
            }

            IBankAccountNumber directDebitBankAccountNr = null;
            if (!string.IsNullOrWhiteSpace(request.DirectDebitBankAccountNr))
            {
                directDebitBankAccountNr = Request.bankAccountNrParser.Value.ParseFromStringWithDefaults(request.DirectDebitBankAccountNr, null);
            }

            string requestedRepaymentTime = null;
            if (request.RequestedRepaymentTimeInDays.HasValue)
                requestedRepaymentTime = request.RequestedRepaymentTimeInDays.Value + "d";
            else if(request.RequestedRepaymentTimeInMonths.HasValue)
                requestedRepaymentTime = request.RequestedRepaymentTimeInMonths.Value + "m";

            s.ChangeList(new List<ComplexApplicationListOperation>
            {
                CreateEdit("requestedLoanAmount", request.RequestedLoanAmount?.ToString(CultureInfo.InvariantCulture)),                
                CreateEdit("requestedRepaymentTime", requestedRepaymentTime),
                CreateEdit("paidToCustomerBankAccountNr", bankAccountNumber?.FormatFor(null)),
                CreateEdit("paidToCustomerBankAccountNrType", bankAccountNumber?.AccountType.ToString()),
                CreateEdit("directDebitBankAccountNr", directDebitBankAccountNr?.FormatFor(null)),
                CreateEdit("directDebitAccountOwnerApplicantNr", request.DirectDebitAccountOwnerApplicantNr?.ToString()),
                CreateEdit("loanObjective", request.LoanObjective),
            });

            return new Response
            {

            };
        }

        public class Request : IValidatableObject
        {
            [Required]
            public string ApplicationNr { get; set; }

            public int? RequestedLoanAmount { get; set; }
            public int? RequestedRepaymentTimeInMonths { get; set; }
            public int? RequestedRepaymentTimeInDays { get; set; }
            public string PaidToCustomerBankAccountNr { get; set; }
            public string PaidToCustomerBankAccountNrType { get; set; }
            public string DirectDebitBankAccountNr { get; set; }
            public int? DirectDebitAccountOwnerApplicantNr { get; set; }
            public string LoanObjective { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                if (!string.IsNullOrWhiteSpace(PaidToCustomerBankAccountNr) && !bankAccountNrParser.Value.TryParseFromStringWithDefaults(PaidToCustomerBankAccountNr, PaidToCustomerBankAccountNrType, out _))
                {
                    yield return new ValidationResult("Invalid PaidToCustomerBankAccountNr+PaidToCustomerBankAccountNrType");
                }

                if(RequestedRepaymentTimeInDays.HasValue && RequestedRepaymentTimeInMonths.HasValue)
                {
                    yield return new ValidationResult("At most one of RequestedRepaymentTimeInDays and RequestedRepaymentTimeInMonths can be included");
                }
            }

            public static Lazy<BankAccountNumberParser> bankAccountNrParser = new Lazy<BankAccountNumberParser>(() => new BankAccountNumberParser(NEnv.ClientCfg.Country.BaseCountry));
        }

        public class Response
        {

        }
    }
}