using Newtonsoft.Json;
using nPreCredit.Code.Services;
using NTech.Banking.BankAccounts;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design.Serialization;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class SetDirectDebitLoanTermsMethod : TypedWebserviceMethod<SetDirectDebitLoanTermsMethod.Request, SetDirectDebitLoanTermsMethod.Response>
    {
        public override string Path => "UnsecuredLoanStandard/DirectDebit/Set-LoanTerms";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var service = requestContext.Resolver().Resolve<IComplexApplicationListService>();

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                List<ComplexApplicationListOperation> changes;
                var directDebitLoanTermsListName = "DirectDebitLoanTerms";
                var directDebitLoanTermsRowNr = 1;
                var application = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == request.ApplicationNr)
                    .Select(x => new
                    {
                        DirectDebitLoanTermsListItems = x.ComplexApplicationListItems.Where(y => y.ListName == directDebitLoanTermsListName && y.Nr == directDebitLoanTermsRowNr),
                        ApplicationListItems = x.ComplexApplicationListItems.Where(y => y.ListName == "Application" && y.Nr == 1)
                    })
                    .FirstOrDefault();

                if (application == null)
                    return Error("No such application exists");

                if (request.IsCancel.Value)
                {
                    changes = ComplexApplicationListService.CreateDeleteOperations(application.DirectDebitLoanTermsListItems.ToList());
                }
                else
                {
                    var t = request.NewTerms;
                    if (t == null)
                        return Error("IsCancel = true requires NewTerms to be included");

                    if (t.IsActive.Value && (string.IsNullOrWhiteSpace(t.BankAccountNr) || !t.BankAccountNrOwnerApplicantNr.HasValue))
                        return Error("IsActive = true requires BankAccountNr, BankAccountNrType and BankAccountNrOwnerApplicantNr");

                    if (t.EnsureCreditNrExists ?? false)
                    {
                        var ai = requestContext.Resolver().Resolve<ApplicationInfoService>().GetApplicationInfo(request.ApplicationNr);
                        //NOTE: This is here since the ui want to display payer number which requires creditNr to make computation possible
                        var applicationRow = ComplexApplicationList
                            .CreateListFromFlattenedItems("Application", application.ApplicationListItems.ToList())
                            .GetRow(1, true);
                        var creditNrResult = UnsecuredLoanStandardCreateLoanMethod.EnsureCreditNr(ai, applicationRow, context);
                        if(creditNrResult.WasCreated)
                        {
                            context.SaveChanges();
                        }
                    }

                    Dictionary<string, string> directDebitLoanTerms;
                    if (t.IsActive.Value)
                    {
                        var parser = new BankAccountNumberParser(NEnv.ClientCfg.Country.BaseCountry);
                        if (!parser.TryParseFromStringWithDefaults(t.BankAccountNr, t.BankAccountNrType, out var parsedBankAccountNr))
                            return Error("Invalid BankAccountNr+BankAccountNrType combination");
                        directDebitLoanTerms = new Dictionary<string, string>
                        {
                            { "bankAccountNr", parsedBankAccountNr.FormatFor(null) },
                            { "bankAccountNrType", parsedBankAccountNr.AccountType.ToString() },
                            { "accountOwnerApplicantNr", t.BankAccountNrOwnerApplicantNr.Value.ToString() },
                            { "isActive", "true" },
                            { "isPending", t.IsPending.Value ? "true" : "false" }
                        };
                        if (!string.IsNullOrWhiteSpace(t.DirectDebitConsentArchiveKey))
                            directDebitLoanTerms["signedConsentPdfArchiveKey"] = t.DirectDebitConsentArchiveKey;
                    }
                    else
                    {
                        directDebitLoanTerms = new Dictionary<string, string>
                        {
                            { "isActive", "false" },
                            { "isPending", t.IsPending.Value ? "true" : "false" }
                        };
                    }
                    changes = ComplexApplicationListService.CreateReplaceRowOperations(application.DirectDebitLoanTermsListItems.ToList(), request.ApplicationNr, directDebitLoanTermsListName, directDebitLoanTermsRowNr, directDebitLoanTerms);
                }

                ComplexApplicationListService.ChangeListComposable(changes, context);
                context.SaveChanges();
            }

            return new Response
            {

            };
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            [Required]
            public bool? IsCancel { get; set; }

            public TermsModel NewTerms { get; set; }

            public class TermsModel
            {
                public bool? IsActive { get; set; }
                public bool? IsPending { get; set; }
                public bool? EnsureCreditNrExists { get; set; }
                public string BankAccountNr { get; set; }
                public string BankAccountNrType { get; set; }
                public int? BankAccountNrOwnerApplicantNr { get; set; }
                public string DirectDebitConsentArchiveKey { get; set; }
            }
        }

        public class Response
        {

        }
    }
}