using Newtonsoft.Json;
using nPreCredit.Code.Services;
using NTech.Banking.BankAccounts;
using NTech.Services.Infrastructure.NTechWs;
using NTech.Services.Infrastructure.NTechWs.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class UnsecuredLoanApplicationEditBankAccountsForCustomerPagesMethod : TypedWebserviceMethod<UnsecuredLoanApplicationEditBankAccountsForCustomerPagesMethod.Request, UnsecuredLoanApplicationEditBankAccountsForCustomerPagesMethod.Response>
    {
        public override string Path => "UnsecuredLoanStandard/CustomerPages/Edit-BankAccounts";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                var customerId = request.CustomerId.Value;

                var infoService = requestContext.Resolver().Resolve<ApplicationInfoService>();
                var ai = infoService.GetApplicationInfo(request.ApplicationNr, true);
                if (ai == null)
                    return Error("No such application exists");
                var applicants = infoService.GetApplicationApplicants(request.ApplicationNr);
                if (!applicants.CustomerIdByApplicantNr.Values.Contains(customerId))
                    return Error("No such application exists");

                var application = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == request.ApplicationNr)
                    .Select(x => new
                    {
                        CurrentCreditDecisionItems = x.CurrentCreditDecision.DecisionItems,
                        x.ComplexApplicationListItems
                    })
                    .SingleOrDefault();

                var bankAccountResponse = FetchUnsecuredLoanApplicationForCustomerPagesMethod.CreateBankAccountsResponse(
                    ai,
                    application.ComplexApplicationListItems.ToList(),
                    application.CurrentCreditDecisionItems);

                if (request.PaidToCustomer != null && !bankAccountResponse.IsPossibleToEditPaidToCustomerBankAccount)
                    return Error("PaidToCustomer cannot be edited at this time");

                if (request.LoansToSettle != null && !bankAccountResponse.IsPossibleToEditLoansToSettleBankAccounts)
                {
                    return Error("LoansToSettle cannot be edited at this time");
                }

                var changes = new List<ComplexApplicationListOperation>();
                void AddChange(string listName, int nr, string name, string value)
                {
                    changes.Add(new ComplexApplicationListOperation
                    {
                        ApplicationNr = request.ApplicationNr,
                        ListName = listName,
                        Nr = nr,
                        ItemName = name,
                        UniqueValue = string.IsNullOrWhiteSpace(value) ? null : value?.Trim(),
                        IsDelete = string.IsNullOrWhiteSpace(value)
                    });
                }

                if (request.PaidToCustomer != null)
                {
                    var listName = "Application";
                    var account = Request.bankAccountNumberParser.Value.ParseFromStringWithDefaults(request.PaidToCustomer.BankAccountNr, request.PaidToCustomer.BankAccountNrType);
                    AddChange(listName, 1, "paidToCustomerBankAccountNr", account.FormatFor(null));
                    AddChange(listName, 1, "paidToCustomerBankAccountNrType", account.AccountType.ToString());
                }

                if (request.LoansToSettle != null)
                {
                    var listName = "LoansToSettle";
                    var loansToSettleList = ComplexApplicationList.CreateListFromFlattenedItems(listName, application.ComplexApplicationListItems);
                    var requestedEditRows = request.LoansToSettle.Accounts.ToDictionary(x => x.Nr, x => x);
                    foreach (var loanToSettleRow in loansToSettleList.GetRows())
                    {
                        if (!requestedEditRows.ContainsKey(loanToSettleRow.Nr))
                            continue;

                        var edit = requestedEditRows[loanToSettleRow.Nr];

                        var account = Request.bankAccountNumberParser.Value.ParseFromStringWithDefaults(edit.BankAccountNr, edit.BankAccountNrType);

                        AddChange(listName, loanToSettleRow.Nr, "bankAccountNr", account.FormatFor(null));
                        AddChange(listName, loanToSettleRow.Nr, "bankAccountNrType", account.AccountType.ToString());
                        AddChange(listName, loanToSettleRow.Nr, "settlementPaymentReference", edit.SettlementPaymentReference);
                        AddChange(listName, loanToSettleRow.Nr, "settlementPaymentMessage", edit.SettlementPaymentMessage);

                        requestedEditRows.Remove(loanToSettleRow.Nr);
                    }
                    if (requestedEditRows.Any())
                        return Error("Attempted to edit loans that dont exist");
                }

                if (changes.Any())
                {
                    ComplexApplicationListService.ChangeListComposable(changes, context);
                    context.SaveChanges();
                }

                return new Response
                {

                };
            }
        }

        public class Request : IValidatableObject
        {
            [Required]
            public int? CustomerId { get; set; }

            [Required]
            public string ApplicationNr { get; set; }

            public PaidToCustomerModel PaidToCustomer { get; set; }

            public class PaidToCustomerModel
            {
                [Required]
                public string BankAccountNr { get; set; }
                public string BankAccountNrType { get; set; }
            }

            public LoansToSettleModel LoansToSettle { get; set; }

            public class LoansToSettleModel
            {
                [Required]
                public List<LoanToSettleModel> Accounts { get; set; }
            }

            public class LoanToSettleModel
            {
                [Required]
                public int? Nr { get; set; }
                [Required]
                public string BankAccountNr { get; set; }
                public string BankAccountNrType { get; set; }
                public string SettlementPaymentReference { get; set; }
                public string SettlementPaymentMessage { get; set; }
            }

            public static Lazy<BankAccountNumberParser> bankAccountNumberParser =
                new Lazy<BankAccountNumberParser>(() => new BankAccountNumberParser(NEnv.ClientCfg.Country.BaseCountry));

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                var request = (Request)validationContext.ObjectInstance;
                if (request.PaidToCustomer != null)
                {
                    if (!bankAccountNumberParser.Value.TryParseFromStringWithDefaults(request.PaidToCustomer.BankAccountNr, request.PaidToCustomer.BankAccountNrType, out _))
                    {
                        yield return new ValidationResult("Invalid PaidToCustomerModel.BankAccountNr+BankAccountNrType combination");
                    }
                }

                if (request.LoansToSettle != null)
                {
                    foreach (var account in request.LoansToSettle.Accounts)
                    {
                        if (!account.Nr.HasValue)
                        {
                            yield return new ValidationResult($"Invalid LoansToSettle.Accounts. Missing Nr");
                        }
                        if (!bankAccountNumberParser.Value.TryParseFromStringWithDefaults(account.BankAccountNr, account.BankAccountNrType, out var parsedAccount))
                        {
                            yield return new ValidationResult($"Invalid LoansToSettle.Accounts[Nr={account.Nr}].BankAccountNr+BankAccountNrType combination", new[] { "BankAccountNr" });
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(account.SettlementPaymentReference) && !PaymentReferenceNumberValidator.IsValidPaymentReferenceNr(parsedAccount.AccountType.ToString(), account.SettlementPaymentReference))
                            {
                                yield return new ValidationResult($"Invalid LoansToSettle.Accounts[Nr={account.Nr}].BankAccountNrType+SettlementPaymentReference combination", new[] { "SettlementPaymentReference" });
                            }
                        }
                    }

                    if (request.LoansToSettle.Accounts.Select(x => x.Nr).Distinct().Count() != request.LoansToSettle.Accounts.Count)
                        yield return new ValidationResult("Duplicate LoansToSettleAccounts.Nr:s encountered");
                }
            }
        }

        public class Response
        {

        }
    }
}