using Moq;
using nCredit;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using nPreCredit;
using nPreCredit.Code;
using nPreCredit.Code.Services;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.Shared.BankAccounts.Fi;
using NTech.Core.Credit.Database;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Customer.Shared;
using NTech.Core.Customer.Shared.Models;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.PreCredit.Database;
using NTech.Core.Savings.Database;
using NTech.Core.Savings.Shared;
using NTech.Core.Savings.Shared.Database;

namespace NTech.Core.Host.IntegrationTests.UlLegacy
{
    internal abstract class UlLegacyTestRunner
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public TestSupport Support { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        protected abstract void DoTest();

        public void RunTest()
        {
            var s = SetupTest();
            Support = s.Support;
            try
            {
                DoTest();
            }
            finally
            {
                s.Lock.ReleaseMutex();
            }
        }

        public static (TestSupport Support, Mutex Lock) SetupTest(Action<Mock<IClientConfigurationCore>>? setupClientConfig = null)
        {
            var mutex = TestDatabases.AquireLockAndCreateTestDatabase();
            var support = SupportShared.CreateSupport<TestSupport>("ulLegacy", "FI", CreateCustomerEnvSettings(), x =>
            {
                x.CreditEnvSettings = CreateCreditEnvSettings(x);
                x.PreCreditEnvSettings = CreatePreCreditEnvSettings();
                x.SavingsEnvSettings = CreateSavingsEnvSettings(x);

                var preCreditContextService = new Mock<IPreCreditContextFactoryService>(MockBehavior.Strict);
                preCreditContextService.Setup(x => x.CreateExtended()).Returns(() => new PreCreditContextExtended(x.CurrentUser, x.Clock));
                x.PreCreditContextService = preCreditContextService.Object;

                x.NotificationProcessSettings = new NotificationProcessSettings
                {
                    NotificationNotificationDay = 14,
                    NotificationDueDay = 28,
                    PaymentFreeMonthMinNrOfPaidNotifications = 3,
                    PaymentFreeMonthExcludeNotificationFee = false,
                    PaymentFreeMonthMaxNrPerYear = 3,
                    ReminderFeeAmount = 5,
                    ReminderMinDaysBetween = 7,
                    SkipReminderLimitAmount = 10,
                    NotificationOverDueGraceDays = 5,
                    MaxNrOfReminders = 2,
                    FirstReminderDaysBefore = 7,
                    MinMonthsBetweenPaymentFreeMonths = 3
                };
            },
            setupClientConfig: cfg =>
            {
                cfg.Setup(x => x.OptionalSetting("ntech.credit.annuity.maxrepaymenttimeinmonths")).Returns("180");
                cfg.Setup(x => x.OptionalSetting("ntech.credit.termination.duedays")).Returns((string)null!);
                cfg.Setup(x => x.GetSingleCustomInt(false, "NotificationProcessSettings", "TerminationLetterGraceDays")).Returns(new int?(0));
                cfg.Setup(x => x.GetSingleCustomInt(false, "NotificationProcessSettings", "DebtCollectionGraceDays")).Returns(new int?(0));
                setupClientConfig?.Invoke(cfg);
            },
            activeFeatures: new HashSet<string>
            {
                    "ntech.feature.unsecuredloans",
                    "ntech.feature.savings",
                    "ntech.feature.paymentplan",
                    "ntech.feature.paymentplan.capitalize"
            });

            return (Support: support, Lock: mutex);
        }

        public static void RunTestStartingFromEmptyDatabases(Action<TestSupport> runTest, Action<Mock<IClientConfigurationCore>>? setupClientConfig = null)
        {
            var s = SetupTest(setupClientConfig: setupClientConfig);
            try
            {
                runTest(s.Support);
            }
            finally
            {
                s.Lock.ReleaseMutex();
            }
        }

        private static IPreCreditEnvSettings CreatePreCreditEnvSettings()
        {
            var envSettings = new Mock<IPreCreditEnvSettings>(MockBehavior.Strict);
            var affiliate = new Module.AffiliateModel
            {
                ProviderName = "self",
                DisplayToEnduserName = "Ul Legacy Oy",
                StreetAddress = "Gatan 1, Staden",
                EnduserContactPhone = "010 524 8410",
                EnduserContactEmail = "self@example.org",
                WebsiteAddress = "self.example.org",
                IsSelf = true,
                IsSendingRejectionEmails = true,
                IsUsingDirectLinkFlow = true,
                IsSendingAdditionalQuestionsEmail = true,
                FallbackCampaignCode = "H00000"
            };
            envSettings
                .Setup(x => x.GetAffiliateModel("self", false))
                .Returns(affiliate);
            envSettings.Setup(x => x.IsUnsecuredLoansEnabled).Returns(true);
            envSettings.Setup(x => x.IsMortgageLoansEnabled).Returns(false);
            envSettings.Setup(x => x.IsStandardUnsecuredLoansEnabled).Returns(false);
            envSettings.Setup(x => x.IsCompanyLoansEnabled).Returns(false);
            envSettings.Setup(x => x.DefaultScoringVersion).Returns("2018");
            envSettings.Setup(x => x.CreditApplicationWorkListIsNewMinutes).Returns(0);
            envSettings.Setup(x => x.CreditsUse360DayInterestYear).Returns(false);
            envSettings.Setup(x => x.CreditReportProviderName).Returns("someCreditReportProvider");
            envSettings.Setup(x => x.AdditionalQuestionsUrlPattern).Returns("http://localhost/questions");
            envSettings.Setup(x => x.ApplicationWrapperUrlPattern).Returns("http://localhost/wrapper");
            envSettings.Setup(x => x.ShowDemoMessages).Returns(false);
            envSettings.Setup(x => x.IsProduction).Returns(false);
            envSettings.Setup(x => x.IsStandardMortgageLoansEnabled).Returns(false);

            envSettings.Setup(x => x.IsAdditionalLoanScoringRuleDisabled).Returns(false);
            envSettings.Setup(x => x.IsCoApplicantScoringRuleDisabled).Returns(false);
            envSettings.Setup(x => x.DisabledScoringRuleNames).Returns(new List<string>());

            envSettings.Setup(x => x.ScoringSetup).Returns(ScoringSetupModel.CreateDirect(0, new List<nPreCredit.Code.ScoringSetupModel.RejectionReason>
                {
                    new nPreCredit.Code.ScoringSetupModel.RejectionReason
                    {
                        Name = "score",
                        DisplayName = "Score",
                        PauseDays = 90,
                        ScoringRules = new List<ScoringSetupModel.ScoringRule>
                        {
                            new ScoringSetupModel.ScoringRule
                            {
                                ForceManualCheck = false,
                                Name = "InterestRateCutoff"
                            }
                        }
                    }
                }, new List<ScoringSetupModel.RejectionEmail>(), new List<ScoringSetupModel.ManualControlOnAcceptRule>()));

            return envSettings.Object;
        }

        private static ICreditEnvSettings CreateCreditEnvSettings(TestSupport support)
        {
            var creditEnvSettings = new Mock<ICreditEnvSettings>(MockBehavior.Strict);
            creditEnvSettings.Setup(x => x.OutgoingPaymentFileCustomerMessagePattern).Returns("PaymentMessage");
            creditEnvSettings.Setup(x => x.OutgoingPaymentBankAccountNr).Returns(IBANFi.Parse("FI2112345600000785"));
            creditEnvSettings.Setup(x => x.IncomingPaymentBankAccountNr).Returns(IBANFi.Parse("FI5840503542417639"));
            creditEnvSettings.Setup(x => x.IsStandardUnsecuredLoansEnabled).Returns(false);
            creditEnvSettings.Setup(x => x.IsProduction).Returns(false);
            creditEnvSettings.Setup(x => x.IsCompanyLoansEnabled).Returns(false);
            creditEnvSettings.Setup(x => x.IsMortgageLoansEnabled).Returns(false);
            creditEnvSettings.Setup(x => x.ClientInterestModel).Returns(nCredit.DbModel.DomainModel.InterestModelCode.Actual_365_25);
            creditEnvSettings.Setup(x => x.CreditsUse360DayInterestYear).Returns(false);
            creditEnvSettings.Setup(x => x.NrOfDaysUntilCreditTermsChangeOfferExpires).Returns(10);
            creditEnvSettings.Setup(x => x.CreditSettlementOfferGraceDays).Returns(10);
            creditEnvSettings.Setup(x => x.IsDirectDebitPaymentsEnabled).Returns(false);
            creditEnvSettings.Setup(x => x.HasPerLoanDueDay).Returns(false);
            creditEnvSettings.Setup(x => x.ClientCreditType).Returns(CreditType.UnsecuredLoan);
            creditEnvSettings.Setup(x => x.DebtCollectionPartnerName).Returns(""); //TODO: What do they actually use?
            creditEnvSettings.Setup(x => x.CreditSettlementBalanceLimit).Returns(30m);
            creditEnvSettings.Setup(x => x.ShouldRecalculateAnnuityOnInterestChange).Returns(() => support.ShouldRecalculateAnnuityOnInterestChange);
            creditEnvSettings.Setup(x => x.LegalInterestCeilingPercent).Returns(20m);
            creditEnvSettings.Setup(x => x.MinAndMaxAllowedMarginInterestRate).Returns(Tuple.Create((decimal?)0m, (decimal?)99m));
            creditEnvSettings.Setup(x => x.IsStandardMortgageLoansEnabled).Returns(false);
            creditEnvSettings.Setup(x => x.PositiveCreditRegisterSettings).Returns(new PositiveCreditRegisterSettingsModel { IsMock = true });
            creditEnvSettings.Setup(x => x.OutgoingCreditNotificationDeliveryFolder).Returns(default(DirectoryInfo)!);

            return creditEnvSettings.Object;
        }

        private static ICustomerEnvSettings CreateCustomerEnvSettings()
        {
            var s = new Mock<ICustomerEnvSettings>(MockBehavior.Strict);
            s.Setup(x => x.DefaultKycQuestionsSets).Returns(new Dictionary<string, KycQuestionsTemplate>());
            s.Setup(x => x.RelativeKycLogFolder).Returns((string)null!);
            return s.Object;
        }

        private static ISavingsEnvSettings CreateSavingsEnvSettings(TestSupport support)
        {
            var s = new Mock<ISavingsEnvSettings>(MockBehavior.Strict);
            s.Setup(x => x.IsProduction).Returns(false);
            s.Setup(x => x.MaxAllowedSavingsCustomerBalance).Returns(100000m);
            return s.Object;
        }

        public class TestSupport : CreditSupportShared, ISupportSharedCredit, ISupportSharedPreCredit
        {
            public bool ShouldRecalculateAnnuityOnInterestChange = true;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            public override ICreditEnvSettings CreditEnvSettings { get; set; }
            public IPreCreditEnvSettings PreCreditEnvSettings { get; set; }
            public ISavingsEnvSettings SavingsEnvSettings { get; set; }
            public IPreCreditContextFactoryService PreCreditContextService { get; set; }
            public override NotificationProcessSettings NotificationProcessSettings { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            public override CreditType CreditType => CreditType.UnsecuredLoan;

            public override CreditContextFactory CreateCreditContextFactory() => new CreditContextFactory(() => new CreditContextExtended(CurrentUser, Clock));
            public SavingsContextFactory CreateSavingsContextFactory() => new SavingsContextFactory(() => new SavingsContext(), SavingsContext.IsConcurrencyException);
        }
    }
}
