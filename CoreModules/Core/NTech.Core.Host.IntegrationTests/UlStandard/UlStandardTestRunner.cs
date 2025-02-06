using Moq;
using nCredit;
using nCredit.DomainModel;
using nPreCredit;
using nPreCredit.Code.Services;
using NTech.Banking.BankAccounts;
using NTech.Banking.BankAccounts.Se;
using NTech.Core.Credit.Database;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Customer.Shared;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Module;
using NTech.Core.PreCredit.Database;

namespace NTech.Core.Host.IntegrationTests.UlStandard
{
    internal static class UlStandardTestRunner
    {
        public static void RunTestStartingFromEmptyDatabases(Action<TestSupport> runTest)
        {
            TestDatabases.RunTestUsingDatabases(() =>
            {
                var customerEnvSettings = new Mock<ICustomerEnvSettings>(MockBehavior.Strict);
                customerEnvSettings.Setup(x => x.RelativeKycLogFolder).Returns((string)null!);

                var support = SupportShared.CreateSupport<TestSupport>("ulStandard", "SE", customerEnvSettings.Object, x =>
                {
                    x.OutgoingPaymentAccount = BankAccountNumberSe.Parse("33009410222393");
                    x.IncomingPaymentAccount = BankGiroNumberSe.Parse("9020033");
                    x.AutogiroSettings = new AutogiroSettingsModel
                    {
                        BankGiroNr = x.IncomingPaymentAccount as BankGiroNumberSe,
                        CustomerNr = "424242"
                    };
                    //We start from "today" rather than the prod date way back in time since we know the test will never import old migrated loans
                    //so we dont need to write 50k dates on every test for no reason
                    x.CalendarDateService = new CalendarDateService(x.Clock.Today, x.CreateCreditContextFactory());

                    var creditEnvSettings = new Mock<ICreditEnvSettings>(MockBehavior.Strict);
                    creditEnvSettings.Setup(x => x.IsProduction).Returns(false);
                    creditEnvSettings.Setup(x => x.OutgoingPaymentFileCustomerMessagePattern).Returns("PaymentMessage");
                    creditEnvSettings.Setup(x => x.OutgoingPaymentBankAccountNr).Returns(() => x.OutgoingPaymentAccount);
                    creditEnvSettings.Setup(x => x.IsUnsecuredLoansEnabled).Returns(true);
                    creditEnvSettings.Setup(x => x.IsStandardUnsecuredLoansEnabled).Returns(true);
                    creditEnvSettings.Setup(x => x.IsCompanyLoansEnabled).Returns(false);
                    creditEnvSettings.Setup(x => x.IsMortgageLoansEnabled).Returns(false);
                    creditEnvSettings.Setup(x => x.ClientInterestModel).Returns(nCredit.DbModel.DomainModel.InterestModelCode.Actual_365_25);
                    creditEnvSettings.Setup(x => x.CreditsUse360DayInterestYear).Returns(false);
                    creditEnvSettings.Setup(x => x.HasPerLoanDueDay).Returns(false);
                    creditEnvSettings.Setup(x => x.IsDirectDebitPaymentsEnabled).Returns(true);
                    creditEnvSettings.Setup(x => x.IncomingPaymentBankAccountNr).Returns(() => x.IncomingPaymentAccount);
                    creditEnvSettings.Setup(x => x.AutogiroSettings).Returns(() => x.AutogiroSettings);
                    creditEnvSettings.Setup(x => x.CreditSettlementBalanceLimit).Returns(200m);
                    creditEnvSettings.Setup(x => x.ClientCreditType).Returns(CreditType.UnsecuredLoan);
                    creditEnvSettings.Setup(x => x.DebtCollectionPartnerName).Returns(""); //TODO: What do they actually use?
                    creditEnvSettings.Setup(x => x.LegalInterestCeilingPercent).Returns((decimal?)null);
                    creditEnvSettings.Setup(x => x.IsStandardMortgageLoansEnabled).Returns(false);
                    creditEnvSettings.Setup(x => x.ShouldRecalculateAnnuityOnInterestChange).Returns(false);
                    creditEnvSettings.Setup(x => x.OutgoingCreditNotificationDeliveryFolder).Returns(default(DirectoryInfo)!);
                    creditEnvSettings.Setup(x => x.CreditSettlementOfferGraceDays).Returns(0);

                    x.CreditEnvSettings = creditEnvSettings.Object;

                    var preCreditEnvSettings = new Mock<IPreCreditEnvSettings>(MockBehavior.Strict);
                    preCreditEnvSettings.Setup(x => x.IsStandardMortgageLoansEnabled).Returns(false);
                    preCreditEnvSettings.Setup(x => x.IsStandardUnsecuredLoansEnabled).Returns(true);
                    preCreditEnvSettings.Setup(x => x.CreditReportProviderName).Returns("UcSe");
                    preCreditEnvSettings.Setup(x => x.ListCreditReportProviders).Returns(new[] { "UcSe" });

                    var affiliates = new List<AffiliateModel>()
                    {
                        new AffiliateModel
                        {
                            ProviderName = "self",
                            DisplayToEnduserName = "Ul Standard AB",
                            StreetAddress = "Gatan 1, Staden",
                            EnduserContactPhone = "010 524 8410",
                            EnduserContactEmail = "self@example.org",
                            WebsiteAddress = "self.example.org",
                            IsSelf = true,
                            IsSendingRejectionEmails = true,
                            IsUsingDirectLinkFlow = true,
                            IsSendingAdditionalQuestionsEmail = true,
                            FallbackCampaignCode = "H00000"
                        }
                    };

#pragma warning disable CS8603 // Possible null reference return.
                    preCreditEnvSettings
                        .Setup(x => x.GetAffiliateModel(It.IsAny<string>(), It.IsAny<bool>()))
                        .Returns<string, bool>((n, m) => affiliates.FirstOrDefault(x => x.ProviderName == n));
#pragma warning restore CS8603 // Possible null reference return.

                    x.PreCreditEnvSettings = preCreditEnvSettings.Object;

                    var preCreditContextService = new Mock<IPreCreditContextFactoryService>(MockBehavior.Strict);
                    preCreditContextService.Setup(x => x.CreateExtended()).Returns(() => new PreCreditContextExtended(x.CurrentUser, x.Clock));
                    x.PreCreditContextService = preCreditContextService.Object;
                }, setupClientConfig: cfg =>
                {
                    cfg.Setup(x => x.OptionalSetting("ntech.credit.termination.duedays")).Returns("28");
                    cfg.Setup(x => x.GetSingleCustomInt(false, "NotificationProcessSettings", "TerminationLetterGraceDays")).Returns(new int?(0));
                    cfg.Setup(x => x.GetSingleCustomInt(false, "NotificationProcessSettings", "DebtCollectionGraceDays")).Returns(new int?(0));
                });

                runTest(support);
            });
        }
        public class TestSupport : CreditSupportShared, ISupportSharedCredit, ISupportSharedPreCredit
        {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            public override ICreditEnvSettings CreditEnvSettings { get; set; }
            public IPreCreditEnvSettings PreCreditEnvSettings { get; set; }
            public IBankAccountNumber OutgoingPaymentAccount { get; set; }
            public IBankAccountNumber IncomingPaymentAccount { get; set; }
            public AutogiroSettingsModel AutogiroSettings { get; set; }
            public CalendarDateService CalendarDateService { get; set; }
            public IPreCreditContextFactoryService PreCreditContextService { get; set; }
            public override nCredit.DbModel.DomainModel.NotificationProcessSettings NotificationProcessSettings { get; set; } = new nCredit.DbModel.DomainModel.NotificationProcessSettings
            {
                NotificationNotificationDay = 14,
                NotificationDueDay = 28,
                PaymentFreeMonthMinNrOfPaidNotifications = 3,
                PaymentFreeMonthExcludeNotificationFee = false,
                PaymentFreeMonthMaxNrPerYear = 0,
                ReminderFeeAmount = 60,
                ReminderMinDaysBetween = 7,
                SkipReminderLimitAmount = 0,
                NotificationOverDueGraceDays = 5,
                MaxNrOfReminders = 2,
                NrOfFreeInitialReminders = 0,
                MaxNrOfRemindersWithFees = 1,
                TerminationLetterDueDay = null,
                FirstReminderDaysBefore = 7,
                AreBackToBackPaymentFreeMonthsAllowed = false,
                AllowMissingCustomerAddress = false
            };
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            public override CreditType CreditType => CreditType.UnsecuredLoan;

            public override CreditContextFactory CreateCreditContextFactory()
            {
                return new CreditContextFactory(() => new CreditContextExtended(CurrentUser, Clock));
            }
        }
    }
}
