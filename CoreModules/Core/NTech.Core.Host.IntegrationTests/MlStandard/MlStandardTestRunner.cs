using Moq;
using nCredit;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using NTech.Banking.BankAccounts;
using NTech.Banking.BankAccounts.Se;
using NTech.Core.Credit.Database;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Customer.Shared;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Module;

namespace NTech.Core.Host.IntegrationTests.UlLegacy
{
    internal static class MlStandardTestRunner
    {
        public static void RunTestStartingFromEmptyDatabases(Action<TestSupport> runTest, Action<Mock<ICreditEnvSettings>>? overrideCreditSettings = null)
        {
            TestDatabases.RunTestUsingDatabases(() =>
            {
                var customerEnvSettings = new Mock<ICustomerEnvSettings>(MockBehavior.Strict);
                var support = SupportShared.CreateSupport<TestSupport>("mlStandard", "SE", customerEnvSettings.Object, x =>
                {
                    x.OutgoingPaymentAccount = BankAccountNumberSe.Parse("33009410222393");
                    x.IncomingPaymentAccount = BankGiroNumberSe.Parse("9020033");
                    x.AutogiroSettings = new AutogiroSettingsModel
                    {
                        BankGiroNr = x.IncomingPaymentAccount as BankGiroNumberSe,
                        CustomerNr = "424242"
                    };

                    var creditEnvSettings = new Mock<ICreditEnvSettings>(MockBehavior.Strict);
                    creditEnvSettings.Setup(x => x.IsProduction).Returns(false);
                    creditEnvSettings.Setup(x => x.IsMortgageLoansEnabled).Returns(true);
                    creditEnvSettings.Setup(x => x.IsStandardMortgageLoansEnabled).Returns(true);
                    creditEnvSettings.Setup(x => x.MortgageLoanInterestBindingMonths).Returns(3);
                    creditEnvSettings.Setup(x => x.HasPerLoanDueDay).Returns(false);
                    creditEnvSettings.Setup(x => x.ClientInterestModel).Returns(InterestModelCode.Actual_365_25);
                    creditEnvSettings.Setup(x => x.CreditSettlementBalanceLimit).Returns(200);
                    creditEnvSettings.Setup(x => x.AutogiroSettings).Returns(() => x.AutogiroSettings);
                    creditEnvSettings.Setup(x => x.IsDirectDebitPaymentsEnabled).Returns(true);
                    creditEnvSettings.Setup(x => x.IncomingPaymentBankAccountNr).Returns(() => x.IncomingPaymentAccount);
                    creditEnvSettings.Setup(x => x.ClientCreditType).Returns(x.CreditType);
                    creditEnvSettings.Setup(x => x.IsCompanyLoansEnabled).Returns(false);
                    creditEnvSettings.Setup(x => x.DebtCollectionPartnerName).Returns("");
                    creditEnvSettings.Setup(x => x.NrOfDaysUntilCreditTermsChangeOfferExpires).Returns(120);
                    Tuple<decimal?, decimal?>? maxMinInterest = null;
                    creditEnvSettings.Setup(x => x.MinAndMaxAllowedMarginInterestRate).Returns(maxMinInterest!);
                    creditEnvSettings.Setup(x => x.LegalInterestCeilingPercent).Returns((decimal?)null);
                    creditEnvSettings.Setup(x => x.IsUnsecuredLoansEnabled).Returns(false);
                    creditEnvSettings.Setup(x => x.IsStandardUnsecuredLoansEnabled).Returns(false);
                    creditEnvSettings.Setup(x => x.SieFileEnding).Returns("si");
                    creditEnvSettings.Setup(x => x.GetAffiliateModels()).Returns(new List<AffiliateModel> { SelfAffiliate });
                    creditEnvSettings.Setup(x => x.OutgoingCreditNotificationDeliveryFolder).Returns(default(DirectoryInfo)!);
                    
                    overrideCreditSettings?.Invoke(creditEnvSettings);

                    x.CreditEnvSettings = creditEnvSettings.Object;
                }, setupClientConfig: clientConfig =>
                {
                    clientConfig.Setup(x => x.OptionalSetting("ntech.credit.termination.duedays")).Returns("28");
                    clientConfig.Setup(x => x.GetSingleCustomInt(false, "NotificationProcessSettings", "TerminationLetterGraceDays")).Returns(new int?(0));
                    clientConfig.Setup(x => x.GetSingleCustomInt(false, "NotificationProcessSettings", "DebtCollectionGraceDays")).Returns(new int?(0));                    
                }, activeFeatures: new HashSet<string> { CreditFeatureToggles.CoNotification, CreditFeatureToggles.AgreementNr });

                runTest(support);
            });
        }

        public class TestSupport : CreditSupportShared, ISupportSharedCredit
        {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            public override ICreditEnvSettings CreditEnvSettings { get; set; }
            public IBankAccountNumber OutgoingPaymentAccount { get; set; }
            public IBankAccountNumber IncomingPaymentAccount { get; set; }
            public AutogiroSettingsModel AutogiroSettings { get; set; }

            public override NotificationProcessSettings NotificationProcessSettings { get; set; } = new NotificationProcessSettings
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
                AreBackToBackPaymentFreeMonthsAllowed = false,
                MaxNrOfRemindersWithFees = 1,
                NrOfFreeInitialReminders = 0,
                FirstReminderDaysBefore = 7
            };
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            public override CreditType CreditType => CreditType.MortgageLoan;

            public override CreditContextFactory CreateCreditContextFactory() => new CreditContextFactory(() => new CreditContextExtended(CurrentUser, Clock));

            //These two track the balance on bookeeping accounts across the entire test run to allow tracking the end result on multiple RunOneMonth on the bookkeeping.
            public Dictionary<string, decimal> BookKeepingBalancePerAccount = new Dictionary<string, decimal>();
            public Dictionary<string, string> BookKeepingNamePerAccount = new Dictionary<string, string>();
        }

        private static AffiliateModel SelfAffiliate = new Module.AffiliateModel
            {
                ProviderName = "self",
                DisplayToEnduserName = "Ml Standard client",
                StreetAddress = "Gatan 1, Staden",
                EnduserContactPhone = "010 524 8410",
                EnduserContactEmail = "self@example.org",
                WebsiteAddress = "self.example.org",
                IsSelf = true
            };
}
}
