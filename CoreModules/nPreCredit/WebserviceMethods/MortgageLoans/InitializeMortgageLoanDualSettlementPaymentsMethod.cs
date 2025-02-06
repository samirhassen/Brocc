using nPreCredit.Code.Datasources;
using nPreCredit.Code.Services;
using NTech;
using NTech.Banking.BankAccounts.Fi;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace nPreCredit.WebserviceMethods.MortgageLoans
{
    public class InitializeMortgageLoanDualSettlementPaymentsMethod : TypedWebserviceMethod<InitializeMortgageLoanDualSettlementPaymentsMethod.Request, InitializeMortgageLoanDualSettlementPaymentsMethod.Response>
    {
        public override string Path => "MortgageLoan/Initialize-Dual-SettlementPayments";

        public override bool IsEnabled => NEnv.IsOnlyNonStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var r = requestContext.Resolver();

            var infoService = r.Resolve<ApplicationInfoService>();

            var ai = infoService.GetApplicationInfo(request.ApplicationNr);
            if (ai == null)
                return Error("No such application", errorCode: "noSuchApplication");

            var wf = r.Resolve<IMortgageLoanWorkflowService>();

            var currentListName = wf.GetCurrentListName(ai.ListNames);
            if (!wf.TryDecomposeListName(currentListName, out var names))
            {
                throw new Exception("Invalid application. Current listname is broken.");
            }
            var currentStepName = names.Item1;

            var settlementStep = wf.Model.FindStepByCustomData(x => x?.IsSettlement == "yes", new { IsSettlement = "" });

            if (settlementStep == null)
                throw new Exception("There needs to be a step in the workflow with CustomData item IsSettlement = \"yes\"");

            var isSettlementStep = currentStepName == settlementStep.Name;
            if (!isSettlementStep || !ai.IsActive || ai.IsFinalDecisionMade || !ai.HasLockedAgreement)
                return Error("No on the settlement step", errorCode: "wrongStatus");

            var ds = r.Resolve<ApplicationDataSourceService>();

            var dataItems = ds.GetDataSimple(request.ApplicationNr, new Dictionary<string, HashSet<string>>
            {
                { CreditApplicationItemDataSource.DataSourceNameShared, new HashSet<string>
                {
                    "application.outgoingPaymentFileStatus",
                    "application.consumerBankAccountIban"
                } },
                { ComplexApplicationListDataSource.DataSourceNameShared, new HashSet<string> {
                    "CurrentMortgageLoans#*#u#exists",
                    "CurrentMortgageLoans#*#u#loanShouldBeSettled",
                    "CurrentMortgageLoans#*#u#loanTotalAmount",
                    "CurrentMortgageLoans#*#u#bankName",
                    "CurrentOtherLoans#*#u#exists",
                    "CurrentOtherLoans#*#u#loanShouldBeSettled",
                    "CurrentOtherLoans#*#u#loanTotalAmount",
                    "CurrentOtherLoans#*#u#bankName" }
                }
            });

            var outgoingPaymentFileStatus = dataItems.Item(CreditApplicationItemDataSource.DataSourceNameShared, "application.outgoingPaymentFileStatus").StringValue.Optional;

            if ((outgoingPaymentFileStatus ?? "initial") != "initial")
            {
                return new Response
                {
                    WasInitialized = false
                };
            }

            var loans = dataItems
                .ItemNames(ComplexApplicationListDataSource.DataSourceNameShared)
                .Select(compoundName =>
                    {
                        var n = ComplexApplicationListDataSource.ParseFullySpecifiedCompoundName(compoundName);
                        var value = dataItems.Item(ComplexApplicationListDataSource.DataSourceNameShared, compoundName).StringValue.Optional;
                        return new
                        {
                            n.ListName,
                            n.RowNr,
                            n.ItemName,
                            Value = value
                        };
                    })
                .Where(x => x.Value != null)
                .GroupBy(x => new { x.ListName, x.RowNr })
                .Select(x => new
                {
                    ListName = x.Key.ListName,
                    RowNr = x.Key.RowNr,
                    Exists = x.FirstOrDefault(y => y.ItemName == "exists")?.Value == "true",
                    LoanShouldBeSettled = x.FirstOrDefault(y => y.ItemName == "loanShouldBeSettled")?.Value == "true",
                    LoanTotalAmount = Numbers.ParseDecimalOrNull(x.FirstOrDefault(y => y.ItemName == "loanTotalAmount")?.Value),
                    BankName = x.FirstOrDefault(y => y.ItemName == "bankName")?.Value
                })
                .Where(x => x.Exists && x.LoanTotalAmount.HasValue && x.LoanTotalAmount.Value > 0m)
                .ToList();

            Dictionary<string, decimal?> decisionAmounts;
            using (var context = new PreCreditContext())
            {
                var decisionItemNames = new List<string> { "mainPurchaseAmount", "mainDirectToCustomerAmount", "childDirectToCustomerAmount" };
                decisionAmounts = context
                    .CreditDecisionItems
                    .Where(x =>
                        x.Decision.ApplicationNr == request.ApplicationNr
                        && x.Decision.CreditApplication.CurrentCreditDecisionId == x.CreditDecisionId
                        && decisionItemNames.Contains(x.ItemName))
                    .Select(x => new
                    {
                        x.ItemName,
                        x.Value
                    })
                    .ToList()
                    .ToDictionary(x => x.ItemName, x => Numbers.ParseDecimalOrNull(x.Value));
            }

            var initialPayments = new List<ComplexApplicationListOperation>();
            var mainNr = 1;
            var childNr = 1;
            Action<bool, decimal, string, string> addPayment = (isMain, amount, bankName, bankAccountNr) =>
            {
                var values = new Dictionary<string, string>
                    {
                        { "exists", "true" },
                        { "paymentAmount", amount.ToString(CultureInfo.InvariantCulture) },
                    };

                if (!string.IsNullOrWhiteSpace(bankName))
                    values["targetBankName"] = bankName;

                if (!string.IsNullOrWhiteSpace(bankAccountNr))
                    values["targetAccountIban"] = bankAccountNr;

                initialPayments.AddRange(ComplexApplicationListOperation.CreateNewRow(
                    request.ApplicationNr,
                    $"{(isMain ? "Main" : "Child")}SettlementPayments",
                    isMain ? mainNr++ : childNr++,
                    uniqueValues: values));
            };
            var mainPurchaseAmount = decisionAmounts.OptSDefaultValue("mainPurchaseAmount");
            var mainDirectToCustomerAmount = decisionAmounts.OptSDefaultValue("mainDirectToCustomerAmount");
            var childDirectToCustomerAmount = decisionAmounts.OptSDefaultValue("childDirectToCustomerAmount");

            if (mainPurchaseAmount.HasValue && mainPurchaseAmount.Value > 0m)
            {
                addPayment(true, mainPurchaseAmount.Value, null, null);
            }

            foreach (var settledMortgageLoan in loans.Where(x => x.ListName == "CurrentMortgageLoans" && x.LoanShouldBeSettled))
            {
                addPayment(true, settledMortgageLoan.LoanTotalAmount.Value, settledMortgageLoan.BankName, null);
            }

            Lazy<(string AccountNr, string BankName)> directToCustomerBankAccountNrAndBankName = new Lazy<(string AccountNr, string BankName)>(() =>
            {
                var consumerBankAccountIban = dataItems.Item(CreditApplicationItemDataSource.DataSourceNameShared, "application.consumerBankAccountIban").StringValue.Optional;
                if (string.IsNullOrWhiteSpace(consumerBankAccountIban))
                    return (null, null);
                if (r.Resolve<IClientConfiguration>().Country.BaseCountry == "FI" &&
                    IBANFi.TryParse(consumerBankAccountIban, out var ibanFi))
                    return (ibanFi.NormalizedValue, NEnv.IBANToBICTranslatorInstance.InferBankName(ibanFi));
                return (null, null);
            });

            if (childDirectToCustomerAmount.HasValue && childDirectToCustomerAmount.Value > 0m)
            {
                var (bankAccountNr, bankName) = directToCustomerBankAccountNrAndBankName.Value;
                addPayment(false, childDirectToCustomerAmount.Value, bankName, bankAccountNr);
            }

            if (mainDirectToCustomerAmount.HasValue && mainDirectToCustomerAmount.Value > 0m)
            {
                var (bankAccountNr, bankName) = directToCustomerBankAccountNrAndBankName.Value;
                addPayment(true, mainDirectToCustomerAmount.Value, bankName, bankAccountNr);
            }

            foreach (var settledOtherLoan in loans.Where(x => x.ListName == "CurrentOtherLoans" && x.LoanShouldBeSettled))
            {
                addPayment(false, settledOtherLoan.LoanTotalAmount.Value, settledOtherLoan.BankName, null);
            }

            ds.SetData(request.ApplicationNr, new ApplicationDataSourceEditModel
            {
                DataSourceName = CreditApplicationItemDataSource.DataSourceNameShared,
                CompoundItemName = "application.outgoingPaymentFileStatus",
                NewValue = "initialized"
            }, requestContext.CurrentUserMetadata());

            var s = r.Resolve<IComplexApplicationListService>();

            return new Response
            {
                WasInitialized = s.ChangeList(initialPayments)
            };
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
        }

        public class Response
        {
            public bool WasInitialized { get; set; }
        }
    }
}