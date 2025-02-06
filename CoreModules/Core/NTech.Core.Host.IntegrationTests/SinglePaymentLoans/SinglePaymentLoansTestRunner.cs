using Moq;
using nCredit;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using nPreCredit;
using nPreCredit.Code.Services;
using NTech.Banking.BankAccounts.Se;
using NTech.Core.Credit.Database;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Customer.Shared;
using NTech.Core.Customer.Shared.Models;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.PreCredit.Database;


namespace NTech.Core.Host.IntegrationTests.SinglePaymentLoans
{
    internal abstract class SinglePaymentLoansTestRunner
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public TestSupport Support { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        protected abstract void DoTest();
        public const string InitialFeeNotificationCode = "initialFeeNotification";

        public virtual void RunTest(Action<Mock<IClientConfigurationCore>>? overrideClientConfig = null)
        {
            var s = SetupTest(overrideClientConfig: overrideClientConfig);
            Support = s.Support;
            try
            {
                var customCostService = Support.GetRequiredService<CustomCostTypeService>();
                customCostService.SetCosts(new List<CustomCost>
                {
                    new CustomCost
                    {
                        Code = InitialFeeNotificationCode,
                        Text = "Initial fee"
                    }
                });

                var orderService = Support.GetRequiredService<PaymentOrderService>();
                orderService.SetOrder(Enumerables
                    .Singleton(PaymentOrderItem.FromCustomCostCode(InitialFeeNotificationCode))
                    .Concat(PaymentOrderService.GetDefaultBuiltinPaymentOrder())
                    .ToList());

                DoTest();
            }
            finally
            {
                s.Lock.ReleaseMutex();
            }
        }

        public static (TestSupport Support, Mutex Lock) SetupTest(Action<Mock<IClientConfigurationCore>>? overrideClientConfig = null)
        {
            var mutex = TestDatabases.AquireLockAndCreateTestDatabase();
            var support = SupportShared.CreateSupport<TestSupport>("singlePaymentLoans", "SE", CreateCustomerEnvSettings(), x =>
            {
                x.CreditEnvSettings = CreateCreditEnvSettings(x);
                x.PreCreditEnvSettings = CreatePreCreditEnvSettings();

                var preCreditContextService = new Mock<IPreCreditContextFactoryService>(MockBehavior.Strict);
                preCreditContextService.Setup(x => x.CreateExtended()).Returns(() => new PreCreditContextExtended(x.CurrentUser, x.Clock));
                x.PreCreditContextService = preCreditContextService.Object;
                x.NotificationProcessSettings = new NotificationProcessSettings
                {
                    NotificationNotificationDay = 14,
                    NotificationDueDay = 28,
                    ReminderFeeAmount = 60,
                    ReminderMinDaysBetween = 7,
                    SkipReminderLimitAmount = 0,
                    NotificationOverDueGraceDays = 5,
                    MaxNrOfReminders = 2,
                    MaxNrOfRemindersWithFees = 1,
                    FirstReminderDaysBefore = NotificationProcessSettings.DefaultFirstReminderDaysBefore
                };
            },
            setupClientConfig: cfg =>
            {
                cfg.Setup(x => x.OptionalSetting("ntech.credit.termination.duedays")).Returns("28");
                cfg.Setup(x => x.OptionalSetting("ntech.handlerlimits.levelamounts")).Returns("300000,500000");
                cfg.Setup(x => x.GetSingleCustomInt(false, "NotificationProcessSettings", "TerminationLetterGraceDays")).Returns(new int?(0));
                cfg.Setup(x => x.GetSingleCustomInt(false, "NotificationProcessSettings", "DebtCollectionGraceDays")).Returns(new int?(0));                

                overrideClientConfig?.Invoke(cfg);
            },
            activeFeatures: new HashSet<string>
            {
                "ntech.feature.handlerlimits.v1",
                "ntech.precredit.manualsignatures",
                "ntech.feature.credit.amortizationplan.v1",
                "ntech.feature.unsecuredloans",
                "ntech.feature.unsecuredloans.standard",
                "ntech.feature.manualCreditReports",
                "ntech.feature.directdebitpaymentsenabled",
                "ntech.customerpages.allowdirecteidlogin",
                "ntech.feature.kycbatchscreening",
                "ntech.feature.securemessages",
                "ntech.feature.customeroverview",
                "ntech.feature.precredit",
                "ntech.feature.paymentplan",
                "ntech.feature.useannuities"
            });

            return (Support: support, Lock: mutex);
        }

        public static void RunTestStartingFromEmptyDatabases(Action<TestSupport> runTest)
        {
            var s = SetupTest();
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
                DisplayToEnduserName = "Single loan AB",
                StreetAddress = "Gatan 1, Staden",
                EnduserContactPhone = "010 524 8410",
                EnduserContactEmail = "self@example.org",
                WebsiteAddress = "self.example.org",
                IsSelf = true,
                IsSendingRejectionEmails = false,
                IsUsingDirectLinkFlow = true,
                IsSendingAdditionalQuestionsEmail = false
            };
            envSettings
                .Setup(x => x.GetAffiliateModel("self", It.IsAny<bool>()))
                .Returns(affiliate);
            envSettings.Setup(x => x.IsUnsecuredLoansEnabled).Returns(true);
            envSettings.Setup(x => x.IsMortgageLoansEnabled).Returns(false);
            envSettings.Setup(x => x.IsStandardUnsecuredLoansEnabled).Returns(true);
            envSettings.Setup(x => x.IsCompanyLoansEnabled).Returns(false);
            envSettings.Setup(x => x.CreditsUse360DayInterestYear).Returns(false);
            envSettings.Setup(x => x.CreditReportProviderName).Returns("someCreditReportProvider");
            envSettings.Setup(x => x.AdditionalQuestionsUrlPattern).Returns("http://localhost/questions");
            envSettings.Setup(x => x.ApplicationWrapperUrlPattern).Returns("http://localhost/wrapper");
            envSettings.Setup(x => x.ShowDemoMessages).Returns(false);
            envSettings.Setup(x => x.IsProduction).Returns(false);
            envSettings.Setup(x => x.IsStandardMortgageLoansEnabled).Returns(false);
            envSettings.Setup(x => x.IsAdditionalLoanScoringRuleDisabled).Returns(false);
            envSettings.Setup(x => x.IsCoApplicantScoringRuleDisabled).Returns(false);

            return envSettings.Object;
        }

        private static ICreditEnvSettings CreateCreditEnvSettings(TestSupport support)
        {
            var creditEnvSettings = new Mock<ICreditEnvSettings>(MockBehavior.Strict);
            creditEnvSettings.Setup(x => x.OutgoingPaymentFileCustomerMessagePattern).Returns("PaymentMessage");
            creditEnvSettings.Setup(x => x.OutgoingPaymentBankAccountNr).Returns(BankAccountNumberSe.Parse("33009410222393"));
            creditEnvSettings.Setup(x => x.IncomingPaymentBankAccountNr).Returns(BankGiroNumberSe.Parse("9020033"));
            creditEnvSettings.Setup(x => x.AutogiroSettings).Returns(new AutogiroSettingsModel
            {
                BankGiroNr = BankGiroNumberSe.Parse("9020033"),
                CustomerNr = "424242"
            });
            creditEnvSettings.Setup(x => x.IsStandardUnsecuredLoansEnabled).Returns(true);
            creditEnvSettings.Setup(x => x.IsCompanyLoansEnabled).Returns(false);
            creditEnvSettings.Setup(x => x.IsMortgageLoansEnabled).Returns(false);
            creditEnvSettings.Setup(x => x.ClientInterestModel).Returns(InterestModelCode.Actual_365_25);
            creditEnvSettings.Setup(x => x.CreditsUse360DayInterestYear).Returns(false);
            creditEnvSettings.Setup(x => x.NrOfDaysUntilCreditTermsChangeOfferExpires).Returns(10);
            creditEnvSettings.Setup(x => x.CreditSettlementOfferGraceDays).Returns(10);
            creditEnvSettings.Setup(x => x.IsDirectDebitPaymentsEnabled).Returns(true);
            creditEnvSettings.Setup(x => x.HasPerLoanDueDay).Returns(false);
            creditEnvSettings.Setup(x => x.ClientCreditType).Returns(CreditType.UnsecuredLoan);
            creditEnvSettings.Setup(x => x.DebtCollectionPartnerName).Returns(""); //TODO: What do they actually use?
            creditEnvSettings.Setup(x => x.CreditSettlementBalanceLimit).Returns(200m);
            creditEnvSettings.Setup(x => x.ShouldRecalculateAnnuityOnInterestChange).Returns(() => support.ShouldRecalculateAnnuityOnInterestChange);
            creditEnvSettings.Setup(x => x.LegalInterestCeilingPercent).Returns(new decimal?());
            creditEnvSettings.Setup(x => x.MinAndMaxAllowedMarginInterestRate).Returns(Tuple.Create((decimal?)null, (decimal?)null));
            creditEnvSettings.Setup(x => x.IsStandardMortgageLoansEnabled).Returns(false);
            creditEnvSettings.Setup(x => x.IsUnsecuredLoansEnabled).Returns(true);
            creditEnvSettings.Setup(x => x.SieFileEnding).Returns(".si");
            creditEnvSettings.Setup(x => x.IsProduction).Returns(false);
            creditEnvSettings.Setup(x => x.OutgoingPaymentFilesBankName).Returns("excelse");

            return creditEnvSettings.Object;
        }

        private static ICustomerEnvSettings CreateCustomerEnvSettings()
        {
            var s = new Mock<ICustomerEnvSettings>(MockBehavior.Strict);
            s.Setup(x => x.DefaultKycQuestionsSets).Returns(new Dictionary<string, KycQuestionsTemplate>());
            s.Setup(x => x.RelativeKycLogFolder).Returns((string)null!);
            return s.Object;
        }

        public class TestSupport : CreditSupportShared, ISupportSharedPreCredit
        {
            public bool ShouldRecalculateAnnuityOnInterestChange = true;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            public override ICreditEnvSettings CreditEnvSettings { get; set; }
            public IPreCreditEnvSettings PreCreditEnvSettings { get; set; }
            public IPreCreditContextFactoryService PreCreditContextService { get; set; }
            public override NotificationProcessSettings NotificationProcessSettings { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            public override CreditType CreditType => CreditType.UnsecuredLoan;

            public override CreditContextFactory CreateCreditContextFactory() => new CreditContextFactory(() => new CreditContextExtended(CurrentUser, Clock));
        }
    }
}
