using Moq;
using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using nCredit.DbModel.BusinessEvents.NewCredit;
using nCredit.DomainModel;
using Newtonsoft.Json;
using NTech.Banking.BookKeeping;
using NTech.Banking.SieFiles;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Clients;
using System.Text;
using System.Xml.Linq;

namespace NTech.Core.Host.IntegrationTests.SinglePaymentLoans
{
    public class OtherCostsTests
    {
        [Test]
        public void TenDayLoanCustomerIsNotifiedRightAway()
        {
            new Test().RunTest();
        }

        private class Test : SinglePaymentLoansTestRunner
        {
            protected override void DoTest()
            {
                var customerId = TestPersons.EnsureTestPerson(Support, 1);
                var creditEventManager = Support.GetRequiredService<NewCreditBusinessEventManager>();
                const string CreditNr = "L10000";
                const int SinglePaymentLoanRepaymentTimeInDays = 10;
                const decimal LoanAmount = 1000m;

                var customCostService = Support.GetRequiredService<CustomCostTypeService>();
                customCostService.SetCosts(new List<CustomCost>
                {
                    new CustomCost
                    {
                        Code = "someRandomCost",
                        Text = "Some cost"
                    },
                    new CustomCost
                    {
                        Code = InitialFeeNotificationCode,
                        Text = "Initial fee"
                    }
                });
                using (var context = Support.CreateCreditContextFactory().CreateContext())
                {
                    context.BeginTransaction();
                    try
                    {
                        creditEventManager.CreateNewCredit(context, new NewCreditRequest
                        {
                            FirstNotificationCosts = new List<NewCreditRequest.FirstNotificationCostItem>
                            {
                                new NewCreditRequest.FirstNotificationCostItem
                                {
                                    CostAmount = 298m,
                                    CostCode = "someRandomCost"
                                },
                                new NewCreditRequest.FirstNotificationCostItem
                                {
                                    CostAmount = 149m,
                                    CostCode = InitialFeeNotificationCode
                                }
                            },
                            BankAccountNr = "33008912135673",
                            CreditAmount = LoanAmount,
                            SinglePaymentLoanRepaymentTimeInDays = SinglePaymentLoanRepaymentTimeInDays,
                            CreditNr = CreditNr,
                            ProviderName = "self",
                            NrOfApplicants = 1,
                            MarginInterestRatePercent = 39m,
                            Applicants = new List<NewCreditRequestExceptCapital.Applicant>
                        {
                            new NewCreditRequestExceptCapital.Applicant
                            {
                                ApplicantNr = 1,
                                CustomerId = customerId
                            }
                        }
                        }, new Lazy<decimal>(() => 0m));
                        context.SaveChanges();
                        context.CommitTransaction();
                    }
                    catch
                    {
                        context.RollbackTransaction();
                        throw;
                    }
                }

                var notificationService = Support.GetRequiredService<NotificationService>();
                var notificationResult = notificationService.CreateNotifications(true, false);
                var paymentOrderService = Support.GetRequiredService<PaymentOrderService>();
                using (var context = Support.CreateCreditContextFactory().CreateContext())
                {
                    CreditDomainModel credit = CreditDomainModel.PreFetchForSingleCredit(CreditNr, context, Support.CreditEnvSettings);
                    var notification = CreditNotificationDomainModel.CreateForCredit(CreditNr, context, paymentOrderService.GetPaymentOrderItems(), onlyFetchOpen: false).Values.SingleOrDefault();

                    {
                        var amortizationModel = credit.GetAmortizationModel(Support.Clock.Today);
                        var perLoanDueDay = (int?)credit.GetDatedCreditValueOpt(Support.Clock.Today, nCredit.DatedCreditValueCode.NotificationDueDay);

                        Assert.That(notification, Is.Not.Null, "Notification should exist");
                        Assert.That(notification.DueDate, Is.EqualTo(Support.Clock.Today.AddDays(SinglePaymentLoanRepaymentTimeInDays)));
                        Assert.That(notification.DueDate.Day, Is.EqualTo(perLoanDueDay));
                        Assert.That(notification.GetRemainingBalance(Support.Clock.Today, CreditDomainModel.AmountType.Capital), Is.EqualTo(LoanAmount), "Should be one payment of loan amount (1)");
                        Assert.That(amortizationModel.UsesAnnuities, Is.EqualTo(false), "Should use rak amortering / fixed monthly payment");
                        Assert.That(amortizationModel.GetActualFixedMonthlyPaymentOrException(), Is.EqualTo(LoanAmount), "Should be one payment of loan amount (2)");
                    }

                    Support.Now = notification!.DueDate;
                    var notificationBalanceAmount = notification.GetRemainingBalance(Support.Now.Date);
                    Assert.That(notificationBalanceAmount, Is.EqualTo(149m + 298m + 1000m + 11.75m));

                    Credits.CreateAndImportPaymentFile(Support, new Dictionary<string, decimal>
                    {
                        [credit.CreditNr] = notificationBalanceAmount
                    });
                }

                var paymentOrder = paymentOrderService.GetPaymentOrderItems();
                using (var context = Support.CreateCreditContextFactory().CreateContext())
                {
                    var notification = CreditNotificationDomainModel.CreateForCredit(CreditNr, context, paymentOrder, onlyFetchOpen: false).Values.SingleOrDefault();
                    Assert.That(notification!.GetRemainingBalance(Support.Now.Date), Is.EqualTo(0m));
                }

                var bookKeepingRules = NtechBookKeepingRuleFile.Parse(XDocument.Parse(
@"<BookkeepingRules>
    <CompanyName>Test AB</CompanyName>
    <BusinessEvent>
        <BusinessEventName>NewIncomingPaymentFile</BusinessEventName>
        <Booking>
            <LedgerAccount>InterestDebt</LedgerAccount>
            <Connections>Credit,Notification,IncomingPayment</Connections>
            <Accounts>__A_Ränteintäkter__,__A_Bankkonto__</Accounts>
        </Booking>
        <Booking>
            <LedgerAccount>InterestDebt</LedgerAccount>
            <Connections>Credit,IncomingPayment</Connections>
            <Accounts>__A_Ränteintäkter__,__A_Bankkonto__</Accounts>
        </Booking>
        <Booking>
            <LedgerAccount>NotificationFeeDebt</LedgerAccount>
            <Connections>Credit,Notification,IncomingPayment</Connections>
            <Accounts>__A_Avigavgifter__,__A_Bankkonto__</Accounts>
        </Booking>
        <Booking>
            <LedgerAccount>ReminderFeeDebt</LedgerAccount>
            <Connections>Credit,Notification,IncomingPayment</Connections>
            <Accounts>__A_Påminnelseavgifter__,__A_Bankkonto__</Accounts>
        </Booking>
        <Booking>
            <LedgerAccount>NotificationCost</LedgerAccount>
            <Connections>Credit,Notification,IncomingPayment</Connections>
            <Accounts>__A_Uppläggningsavgifter__,__A_Bankkonto__</Accounts>
            <OnlySubAccountCode>initialFeeNotification</OnlySubAccountCode>
        </Booking>
        <Booking>
            <LedgerAccount>NotificationCost</LedgerAccount>
            <Connections>Credit,Notification,IncomingPayment</Connections>
            <Accounts>__A_Någon annan kostnad__,__A_Bankkonto__</Accounts>
            <OnlySubAccountCode>someRandomCost</OnlySubAccountCode>
        </Booking>
        <Booking>
            <LedgerAccount>CapitalDebt</LedgerAccount>
            <Connections>Credit,Notification,IncomingPayment</Connections>
            <Accounts>__A_Långfristiga reversfordringar__,__A_Bankkonto__</Accounts>
        </Booking>
        <Booking>
            <LedgerAccount>CapitalDebt</LedgerAccount>
            <Connections>Credit,IncomingPayment</Connections>
            <Accounts>__A_Långfristiga reversfordringar__,__A_Bankkonto__</Accounts>
        </Booking>
        <Booking>
            <LedgerAccount>CapitalDebt</LedgerAccount>
            <Connections>Credit,Writeoff</Connections>
            <Accounts>__A_Långfristiga reversfordringar__,__A_Förlustreservering reversfordringar__</Accounts>
        </Booking>
        <Booking>
            <LedgerAccount>UnplacedPayment</LedgerAccount>
            <Connections>IncomingPayment</Connections>
            <Accounts>__A_Bankkonto__,__A_Oplacerade inbetalningar__</Accounts>
        </Booking>
    </BusinessEvent>
</BookkeepingRules>"));

                var accountPlan = NtechAccountPlanFile.Parse(XDocument.Parse(
@"<AccountPlan>
  <Accounts>
    <Account name=""Långfristiga reversfordringar"" initialAccountNr=""1381""></Account>
    <Account  name=""Förlustreservering reversfordringar"" initialAccountNr=""1389""></Account>
    <Account  name=""Bankkonto"" initialAccountNr=""1940""></Account>
    <Account  name=""Interimskonto"" initialAccountNr=""2490""></Account>
    <Account  name=""Oplacerade inbetalningar"" initialAccountNr=""2436""></Account>
    <Account  name=""Ränteintäkter"" initialAccountNr=""3024""></Account>
    <Account  name=""Avigavgifter"" initialAccountNr=""3661""></Account>
    <Account  name=""Påminnelseavgifter"" initialAccountNr=""3670""></Account>
    <Account  name=""Uppläggningsavgifter"" initialAccountNr=""3682""></Account>
    <Account  name=""Någon annan kostnad"" initialAccountNr=""3677""></Account>
  </Accounts>
</AccountPlan>"));


                var bookKeeping = new BookKeepingFileManager(Support.CurrentUser, Support.ClientConfiguration, Support.Clock,
                    Support.CreditEnvSettings, Support.CreateCreditContextFactory(), () => bookKeepingRules);
                using(var context = Support.CreateCreditContextFactory().CreateContext())
                {
                    context.BeginTransaction();
                    try
                    {
                        var documentClient = new Mock<IDocumentClient>();
                        SieBookKeepingFile? sieFile = null;

                        var isOk = bookKeeping.TryCreateBookKeepingFile(context, new List<DateTime> { Support.Clock.Today }, documentClient.Object, null, new HashSet<string> { "self" },
                            Support.GetRequiredService<IKeyValueStoreService>(), accountPlan, out var header, out var warnings, x => sieFile = x);
                        Assert.That(isOk, Is.True, "Should create bookkeeping file", warnings?.FirstOrDefault());

                        var ms = new MemoryStream();
                        sieFile!.Save(ms);
                        



                        Console.WriteLine(JsonConvert.SerializeObject(sieFile, Formatting.Indented));
                        Console.WriteLine("------------------------------");
                        Console.WriteLine(Encoding.GetEncoding(437).GetString(ms.ToArray()));

                        context.SaveChanges();
                        context.CommitTransaction();
                    }
                    catch
                    {
                        context.RollbackTransaction();
                        throw;
                    }
                }                
            }
        }
    }
}
