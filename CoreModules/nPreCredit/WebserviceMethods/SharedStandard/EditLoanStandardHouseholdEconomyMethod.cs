using Newtonsoft.Json;
using nPreCredit.Code.Services;
using NTech.Banking.BankAccounts;
using NTech.Services.Infrastructure.CreditStandard;
using NTech.Services.Infrastructure.NTechWs;
using NTech.Services.Infrastructure.NTechWs.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace nPreCredit.WebserviceMethods.SharedStandard
{
    public class EditLoanStandardHouseholdEconomyMethod : TypedWebserviceMethod<EditLoanStandardHouseholdEconomyMethod.Request, EditLoanStandardHouseholdEconomyMethod.Response>
    {
        public override string Path => "LoanStandard/Edit-HouseholdEconomy";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled || NEnv.IsStandardMortgageLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var resolver = requestContext.Resolver();
            var s = resolver.Resolve<IComplexApplicationListService>();

            var isMl = NEnv.IsStandardMortgageLoansEnabled;

            var complexListChanges = new List<ComplexApplicationListOperation>();

            void AddApplicationEdit(string itemName, string newValue) => complexListChanges.Add(
                new ComplexApplicationListOperation
                {
                    ApplicationNr = request.ApplicationNr,
                    Nr = 1,
                    ListName = "Application",
                    IsDelete = string.IsNullOrWhiteSpace(newValue),
                    ItemName = itemName,
                    UniqueValue = string.IsNullOrWhiteSpace(newValue) ? null : newValue
                });

            using (var context = new PreCreditContext())
            {
                complexListChanges.AddRange(GetChildrenChanges(context, request));
                complexListChanges.AddRange(GetOtherLoansChanges(context, request, isMl));
            }

            AddApplicationEdit("housing", request.Housing);
            AddApplicationEdit("housingCostPerMonthAmount", request.HousingCostPerMonthAmount?.ToString(CultureInfo.InvariantCulture));
            AddApplicationEdit("otherHouseholdFixedCostsAmount", request.OtherHouseholdFixedCostsAmount?.ToString(CultureInfo.InvariantCulture));
            AddApplicationEdit("otherHouseholdFinancialAssetsAmount", request.OtherHouseholdFinancialAssetsAmount?.ToString(CultureInfo.InvariantCulture));
            AddApplicationEdit("outgoingChildSupportAmount", request?.OutgoingChildSupportAmount?.ToString(CultureInfo.InvariantCulture));
            AddApplicationEdit("incomingChildSupportAmount", request?.IncomingChildSupportAmount?.ToString(CultureInfo.InvariantCulture));
            AddApplicationEdit("childBenefitAmount", request?.ChildBenefitAmount?.ToString(CultureInfo.InvariantCulture));

            s.ChangeList(complexListChanges);

            return new Response
            {

            };
        }

        private List<ComplexApplicationListOperation> GetChildrenChanges(PreCreditContext context, Request request)
        {
            const string ChildrenListName = "HouseholdChildren";
            var currentChildrenItems = context
                .ComplexApplicationListItems
                .Where(x => x.ApplicationNr == request.ApplicationNr && x.ListName == ChildrenListName)
                .ToList();
            var newChildrenItems = (request.Children ?? new List<Request.ChildModel>()).Select(x =>
            {
                var d = new Dictionary<string, string>();
                if (x.AgeInYears.HasValue)
                    d["ageInYears"] = x.AgeInYears.Value.ToString(CultureInfo.InvariantCulture);
                if (x.SharedCustody.HasValue)
                    d["sharedCustody"] = x.SharedCustody.Value ? "true" : "false";
                return d;
            }).ToList();
            return ComplexApplicationListService.SynchListTreatedAsArray(request.ApplicationNr, ChildrenListName, currentChildrenItems, newChildrenItems);
        }

        private List<ComplexApplicationListOperation> GetOtherLoansChanges(PreCreditContext context, Request request, bool isMl)
        {
            const string LoansListName = "LoansToSettle";
            var currentLoansItems = context
                .ComplexApplicationListItems
                .Where(x => x.ApplicationNr == request.ApplicationNr && x.ListName == LoansListName)
                .ToList();
            var newLoansItems = (request.OtherLoans ?? new List<Request.OtherLoanModel>()).Select(x =>
            {
                var d = new Dictionary<string, string>();
                d["loanType"] = string.IsNullOrWhiteSpace(x.LoanType) ? CreditStandardOtherLoanType.Code.unknown.ToString() : x.LoanType;
                if (x.CurrentDebtAmount.HasValue)
                    d["currentDebtAmount"] = x.CurrentDebtAmount.Value.ToString(CultureInfo.InvariantCulture);
                if (x.MonthlyCostAmount.HasValue)
                    d["monthlyCostAmount"] = x.MonthlyCostAmount.Value.ToString(CultureInfo.InvariantCulture);
                if (x.CurrentInterestRatePercent.HasValue)
                    d["currentInterestRatePercent"] = x.CurrentInterestRatePercent.Value.ToString(CultureInfo.InvariantCulture);
                if (x.ShouldBeSettled.HasValue)
                    d["shouldBeSettled"] = isMl ? null : x.ShouldBeSettled.Value ? "true" : "false";
                if (Request.bankAccountNrParser.Value.TryParseFromStringWithDefaults(x.BankAccountNr, x.BankAccountNrType, out var bankAccountNr))
                {
                    d["bankAccountNr"] = bankAccountNr.FormatFor(null);
                    d["bankAccountNrType"] = bankAccountNr.AccountType.ToString();
                }
                if (!string.IsNullOrWhiteSpace(x.SettlementPaymentReference))
                    d["settlementPaymentReference"] = x.SettlementPaymentReference?.Trim();
                if (!string.IsNullOrWhiteSpace(x.SettlementPaymentMessage))
                    d["settlementPaymentMessage"] = x.SettlementPaymentMessage?.Trim();

                return d;
            }).ToList();
            return ComplexApplicationListService.SynchListTreatedAsArray(request.ApplicationNr, LoansListName, currentLoansItems, newLoansItems);
        }

        public class Request : IValidatableObject
        {
            [Required]
            public string ApplicationNr { get; set; }

            [EnumCode(EnumType = typeof(CreditStandardHousingType.Code))]
            public string Housing { get; set; }
            public int? HousingCostPerMonthAmount { get; set; }
            public int? OtherHouseholdFixedCostsAmount { get; set; }
            public int? OtherHouseholdFinancialAssetsAmount { get; set; }
            public List<ChildModel> Children { get; set; }
            public List<OtherLoanModel> OtherLoans { get; set; }
            public int? OutgoingChildSupportAmount { get; set; }
            public int? IncomingChildSupportAmount { get; set; }
            public int? ChildBenefitAmount { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                var request = (Request)validationContext.ObjectInstance;
                if (request.OtherLoans != null)
                {
                    foreach (var otherLoan in request.OtherLoans)
                    {
                        if (!(string.IsNullOrWhiteSpace(otherLoan.BankAccountNr) && string.IsNullOrWhiteSpace(otherLoan.BankAccountNrType)))
                        {
                            if (!bankAccountNrParser.Value.TryParseFromStringWithDefaults(otherLoan.BankAccountNr, otherLoan.BankAccountNrType, out var parsedAccount))
                            {
                                yield return new ValidationResult("Invalid BankAccountNr+BankAccountNrType", new[] { "BankAccountNr" });
                            }
                            else
                            {
                                if (!string.IsNullOrWhiteSpace(otherLoan.SettlementPaymentReference) && !PaymentReferenceNumberValidator.IsValidPaymentReferenceNr(parsedAccount.AccountType.ToString(), otherLoan.SettlementPaymentReference))
                                {
                                    yield return new ValidationResult($"Invalid BankAccountNrType+SettlementPaymentReference", new[] { "SettlementPaymentReference" });
                                }
                            }
                        }
                    }
                }
            }

            public class ChildModel
            {
                public int? AgeInYears { get; set; }
                public bool? SharedCustody { get; set; }
            }

            public class OtherLoanModel
            {
                [EnumCode(EnumType = typeof(CreditStandardOtherLoanType.Code))]
                public string LoanType { get; set; }
                public int? CurrentDebtAmount { get; set; }
                public int? MonthlyCostAmount { get; set; }
                public decimal? CurrentInterestRatePercent { get; set; }
                public bool? ShouldBeSettled { get; set; }
                public string BankAccountNrType { get; set; }
                public string BankAccountNr { get; set; }
                public string SettlementPaymentReference { get; set; }
                public string SettlementPaymentMessage { get; set; }
            }

            public static Lazy<BankAccountNumberParser> bankAccountNrParser = new Lazy<BankAccountNumberParser>(() => new BankAccountNumberParser(NEnv.ClientCfg.Country.BaseCountry));
        }

        public class Response
        {

        }
    }
}