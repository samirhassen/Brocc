using Moq;
using nCredit;
using nCredit.Code;
using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents.NewCredit;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using NTech.Banking.BankAccounts;
using NTech.Banking.BankAccounts.Se;
using NTech.Core.Credit.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.CreditStandard;

namespace NTech.Core.Host.IntegrationTests.UlStandard.Utilities
{
    internal static class CreditsUlStandard
    {
        public static CreditHeader GetCreateCredit(UlStandardTestRunner.TestSupport support, int creditNumber)
        {
            var contextKey = $"TestCredit{creditNumber}_Header";

            if (!support.Context.ContainsKey(contextKey))
            {
                throw new Exception($"Credit {creditNumber} has not been created");
            }

            return (CreditHeader)support.Context[contextKey];
        }

        public class CreateCreditOptions
        {
            public decimal WithheldInitialFeeAmount { get; set; } = 500m;
            public decimal SettledAmount { get; set; } = 3000m;
            public decimal PaidToCustomerAmount { get; set; } = 2000m;
            public decimal AnnuityAmount { get; set; } = 300m;
            public decimal MarginInterestRatePercent { get; set; } = 9.55m;
            public decimal ReferenceInterestRatePercent { get; set; } = 0.01m;
            public int? MainApplicantCustomerId { get; set; } = null;
            public decimal NotificationFeeAmount { get; set; } = 20m;
        }

        public static CreditHeader CreateCreditComposable(UlStandardTestRunner.TestSupport support, CreditContextExtended creditContext, int creditNumber,
            bool skipOverrideOutgoingAccount = false, bool activateDirectDebit = false, CreateCreditOptions? options = null)
        {
            options = options ?? new CreateCreditOptions();
            var contextKey = $"TestCredit{creditNumber}_Header";
            if (support.Context.ContainsKey(contextKey))
            {
                throw new Exception($"Credit {creditNumber} has already been created");
            }

            var fromBankAccountNr = BankAccountNumberSe.Parse("33000803279819");
            if (!skipOverrideOutgoingAccount)
                Credits.OverrideOutgoingPaymentAccount(fromBankAccountNr, support);

            var settleLoanBgNr = BankGiroNumberSe.Parse("902-0033").NormalizedValue;
            var payToCustomerBankAccountNr = BankAccountNumberSe.Parse("3300190109109819").NormalizedValue;

            int mainApplicantCustomerId;
            if (!options.MainApplicantCustomerId.HasValue)
            {
                int GetPersonSeed(bool isCoApplicant)
                {
                    //Main applicants is odd number so customer 1, 3, 5 and so on
                    //Co applicants use even numbers 2, 4, 6 and so on
                    //This so we can generate as many credits as needed without known the number ahead of time
                    if (isCoApplicant)
                        return creditNumber * 2;
                    else
                        return (creditNumber * 2) - 1;
                }

                mainApplicantCustomerId = TestPersons.EnsureTestPerson(support, GetPersonSeed(false));
            }
            else
                mainApplicantCustomerId = options.MainApplicantCustomerId.Value;

            var creditNr = $"C987{creditNumber}"; //The 987 prefix is just to make the length a bit more realistic. It has no significance.

            var creditRequest = new NewCreditRequest
            {
                CapitalizedInitialFeeAmount = 0m,
                CreditAmountParts = Enumerables.SkipNulls(
                    options.SettledAmount == 0m ? null : new NewCreditRequest.CreditAmountPartModel
                    {
                        Amount = options.SettledAmount,
                        PaymentBankAccountNr = settleLoanBgNr,
                        PaymentBankAccountNrType = BankAccountNumberTypeCode.BankGiroSe.ToString(),
                        IsSettlingOtherLoan = true,
                        ShouldBePaidOut = true,
                        PaymentReference = "abc123",
                        SubAccountCode = CreditStandardSubAccountCode.SettledLoanPartCode
                    },
                    options.PaidToCustomerAmount == 0m ? null : new NewCreditRequest.CreditAmountPartModel
                    {
                        Amount = options.PaidToCustomerAmount,
                        PaymentBankAccountNr = payToCustomerBankAccountNr,
                        PaymentBankAccountNrType = BankAccountNumberTypeCode.BankAccountSe.ToString(),
                        IsDirectToCustomerPayment = true,
                        ShouldBePaidOut = true,
                        PaymentMessage = "text1",
                        SubAccountCode = CreditStandardSubAccountCode.PaidToCustomerPartCode
                    },
                    options.WithheldInitialFeeAmount == 0m ? null : new NewCreditRequest.CreditAmountPartModel
                    {
                        Amount = options.WithheldInitialFeeAmount,
                        IsCoveringInitialFeeDrawnFromLoan = true,
                        SubAccountCode = CreditStandardSubAccountCode.WithheldInitialFeeCode
                    }).ToList()
                ,
                AnnuityAmount = options.AnnuityAmount,
                CreditNr = creditNr,
                ProviderName = "self",
                NrOfApplicants = 1,
                NotificationFee = options.NotificationFeeAmount,
                MarginInterestRatePercent = options.MarginInterestRatePercent,
                ProviderApplicationId = null,
                Applicants = new List<NewCreditRequestExceptCapital.Applicant>
                {
                    new NewCreditRequestExceptCapital.Applicant
                    {
                        ApplicantNr = 1,
                        CustomerId = mainApplicantCustomerId,
                        AgreementPdfArchiveKey = "0d464fe1-e09c-403e-ba2e-2ccaa7564c4d.pdf"
                    }
                },
                DirectDebitDetails = activateDirectDebit ? new NewCreditRequest.DirectDebitDetailsModel
                {
                    AccountOwner = 1,
                    AccountNr = payToCustomerBankAccountNr,
                    IsActive = true,
                    IsExternalStatusActive = true
                } : null
            };

            var legalInterestCeilingService = new LegalInterestCeilingService(support.CreditEnvSettings);
            var creditCustomerListServiceComposable = new Mock<ICreditCustomerListServiceComposable>(MockBehavior.Strict);

            var customerClient = TestPersons.CreateRealisticCustomerClient(support);

            var ocrPaymentReferenceGenerator = new OcrPaymentReferenceGenerator(support.ClientConfiguration.Country.BaseCountry, () => new CreditContextExtended(support.CurrentUser, support.Clock));

            var newCreditManager = new NewCreditBusinessEventManager(
                support.CurrentUser,
                legalInterestCeilingService,
                creditCustomerListServiceComposable.Object, support.EncryptionService,
                support.Clock, support.ClientConfiguration, customerClient.Object,
                ocrPaymentReferenceGenerator, support.CreditEnvSettings, support.CreatePaymentAccountService(support.CreditEnvSettings),
                support.GetRequiredService<CustomCostTypeService>());

            var createdCredit = newCreditManager.CreateNewCredit(creditContext, creditRequest, new Lazy<decimal>(() => options.ReferenceInterestRatePercent));

            support.Context[contextKey] = createdCredit;

            return createdCredit;
        }

        public static CreditHeader CreateCredit(UlStandardTestRunner.TestSupport support, int creditNumber,
            bool skipOverrideOutgoingAccount = false, bool activateDirectDebit = false,
            CreateCreditOptions? options = null)
        {
            using (var context = new CreditContextExtended(support.CurrentUser, support.Clock))
            {
                return context.UsingTransaction(() =>
                {
                    var credit = CreateCreditComposable(support, context, creditNumber, skipOverrideOutgoingAccount: skipOverrideOutgoingAccount,
                        activateDirectDebit: activateDirectDebit, options: options);

                    context.SaveChanges();
                    return credit;
                });
            }
        }

        public static AmortizationPlan GetCreditAmortizationPlan(UlStandardTestRunner.TestSupport support, string creditNr, NotificationProcessSettings notificationProcessSettings)
        {
            using (var context = support.CreateCreditContextFactory().CreateContext())
            {
                var model = AmortizationPlan.GetHistoricalCreditModel(creditNr, context, false);
                var dividerOverride = CreditDomainModel.GetInterestDividerOverrideByCode(InterestModelCode.Actual_365_25);
                var isOk = FixedDueDayAmortizationPlanCalculator.TryGetAmortizationPlan(model, notificationProcessSettings, out var amortizationPlan, out var failedMessage, support.Clock, dividerOverride);
                Assert.That(isOk, Is.True, failedMessage);
                return amortizationPlan;
            }
        }
    }
}
