using Moq;
using nSavings.Code;
using nSavings.DbModel.BusinessEvents;
using NTech;
using NTech.Core.Savings.Shared.Database;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFixed;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;
using NTech.Services.Infrastructure;

namespace Brocc.Savings.Tests;

[TestClass]
public class SavingsInterestCapitalizationTests
{
    [TestMethod]
    public void TestFilterByFixedInterestAccount_ShouldReturn0()
    {
        const int userId = 123;
        const string meta = "abc123";
        var clock = MockClock();
        var cli = new Mock<IDocumentClient>();
        var context = new Mock<ISavingsContext>();

        List<SavingsAccountHeader> savingsAccounts =
        [
            new()
            {
                Status = nameof(SavingsAccountStatusCode.Active),
                AccountTypeCode = nameof(SavingsAccountTypeCode.StandardAccount)
            },
            new()
            {
                Status = nameof(SavingsAccountStatusCode.Active),
                AccountTypeCode = nameof(SavingsAccountTypeCode.StandardAccount)
            }
        ];

        context.Setup(c => c.SavingsAccountHeadersQueryable).Returns(savingsAccounts.AsQueryable());

        var mgr = new InterestCapitalizationBusinessEventManager(userId, meta, clock.Object, cli.Object,
            context.Object);

        var changed = mgr.RunInterestCapitalizationAllAccounts(true, SavingsAccountTypeCode.FixedInterestAccount);

        Assert.AreEqual(0, changed);

        // TODO: Validate business no event setup
        // TODO: Validate document service not being called
        // TODO: Validate no ledger transactions
    }

    [TestMethod]
    public void TestFilterByStandardAccount_ShouldReturn0()
    {
        const int userId = 123;
        const string meta = "abc123";
        var clock = MockClock();
        var cli = new Mock<IDocumentClient>();
        var context = new Mock<ISavingsContext>();

        List<SavingsAccountHeader> savingsAccounts =
        [
            new()
            {
                Status = nameof(SavingsAccountStatusCode.Active),
                AccountTypeCode = nameof(SavingsAccountTypeCode.FixedInterestAccount)
            },
            new()
            {
                Status = nameof(SavingsAccountStatusCode.Active),
                AccountTypeCode = nameof(SavingsAccountTypeCode.FixedInterestAccount)
            }
        ];

        context.Setup(c => c.SavingsAccountHeadersQueryable).Returns(savingsAccounts.AsQueryable());

        var mgr = new InterestCapitalizationBusinessEventManager(userId, meta, clock.Object, cli.Object,
            context.Object);

        var changed = mgr.RunInterestCapitalizationAllAccounts(true, SavingsAccountTypeCode.StandardAccount);

        Assert.AreEqual(0, changed);

        // TODO: Validate business no event setup
        // TODO: Validate document service not being called
        // TODO: Validate no ledger transactions
    }

    [TestMethod]
    public void TestStandardAccountInterestMissing_ShouldThrow()
    {
        const int userId = 123;
        const string meta = "abc123";
        var clock = MockClock();
        var cli = new Mock<IDocumentClient>();
        var context = new Mock<ISavingsContext>();

        SetupFixedInterestRates(context);

        List<SavingsAccountHeader> savingsAccounts =
        [
            MockAccount(1, SavingsAccountTypeCode.FixedInterestAccount, "fixed-rate-1"),
            MockAccount(2, SavingsAccountTypeCode.FixedInterestAccount, "fixed-rate-1"),
            MockAccount(3, SavingsAccountTypeCode.StandardAccount),
            MockAccount(4, SavingsAccountTypeCode.StandardAccount)
        ];
        context.Setup(c => c.SavingsAccountHeadersQueryable).Returns(savingsAccounts.AsQueryable());

        var mgr = new InterestCapitalizationBusinessEventManager(userId, meta, clock.Object, cli.Object,
            context.Object);

        var ex = Assert.ThrowsException<Exception>(() => mgr.RunInterestCapitalizationAllAccounts(true, null));

        Assert.AreEqual("Some accounts do not have an active interest rate: [3, 4]", ex.Message);

        // TODO: Validate business event setup
        // TODO: Validate document service not being called
        // TODO: Validate no ledger transactions
    }

    [TestMethod]
    public void TestFixedInterestAccountInterestMissing_ShouldThrow()
    {
        const int userId = 123;
        const string meta = "abc123";
        var clock = MockClock();
        var cli = new Mock<IDocumentClient>();
        var context = new Mock<ISavingsContext>();

        SetupFlexInterestRates(context);

        List<SavingsAccountHeader> savingsAccounts =
        [
            MockAccount(1, SavingsAccountTypeCode.FixedInterestAccount),
            MockAccount(2, SavingsAccountTypeCode.FixedInterestAccount),
            MockAccount(3, SavingsAccountTypeCode.StandardAccount),
            MockAccount(4, SavingsAccountTypeCode.StandardAccount)
        ];
        context.Setup(c => c.SavingsAccountHeadersQueryable).Returns(savingsAccounts.AsQueryable());

        var mgr = new InterestCapitalizationBusinessEventManager(userId, meta, clock.Object, cli.Object,
            context.Object);

        var ex = Assert.ThrowsException<Exception>(() => mgr.RunInterestCapitalizationAllAccounts(true, null));

        Assert.AreEqual("Some accounts do not have an active interest rate: [1, 2]", ex.Message);

        // TODO: Validate business event setup
        // TODO: Validate document service not being called
        // TODO: Validate no ledger transactions
    }

    [TestMethod]
    public void TestInterestRateIsAccumulatedCorrectly_ShouldUpdateAll()
    {
        const int userId = 123;
        const string meta = "abc123";
        var clock = MockClock();
        var cli = new Mock<IDocumentClient>();
        var context = new Mock<ISavingsContext>();

        SetupClientConfigMock();

        SetupFlexInterestRates(context);
        SetupFixedInterestRates(context);
        context.Setup(s => s.SaveChanges()).Returns(1);

        List<SavingsAccountHeader> savingsAccounts =
        [
            MockAccount(1, SavingsAccountTypeCode.FixedInterestAccount, "fixed-rate-1"),
            MockAccount(2, SavingsAccountTypeCode.FixedInterestAccount, "fixed-rate-1"),
            MockAccount(3, SavingsAccountTypeCode.StandardAccount),
            MockAccount(4, SavingsAccountTypeCode.StandardAccount)
        ];

        context.Setup(c => c.SavingsAccountHeadersQueryable).Returns(savingsAccounts.AsQueryable());

        var mgr = new InterestCapitalizationBusinessEventManager(userId, meta, clock.Object, cli.Object,
            context.Object);

        var changed = mgr.RunInterestCapitalizationAllAccounts(false, null);

        Assert.AreEqual(4, changed);

        // TODO: Validate document service not being called
        // TODO: Validate ledger transactions
        // TODO: Validate business event setup
        // TODO: Validate accumulated interest amounts
    }

    [TestMethod]
    public void TestInterestRateIsAccumulatedCorrectly_ShouldSubmitToDocumentService()
    {
        const int userId = 123;
        const string meta = "abc123";
        var clock = MockClock();
        var cli = new Mock<IDocumentClient>();
        var context = new Mock<ISavingsContext>();

        SetupClientConfigMock();

        SetupFlexInterestRates(context);
        SetupFixedInterestRates(context);
        context.Setup(s => s.SaveChanges()).Returns(1);

        List<SavingsAccountHeader> savingsAccounts =
        [
            MockAccount(1, SavingsAccountTypeCode.FixedInterestAccount, "fixed-rate-1"),
            MockAccount(2, SavingsAccountTypeCode.FixedInterestAccount, "fixed-rate-1"),
            MockAccount(3, SavingsAccountTypeCode.StandardAccount),
            MockAccount(4, SavingsAccountTypeCode.StandardAccount)
        ];
        context.Setup(c => c.SavingsAccountHeadersQueryable).Returns(savingsAccounts.AsQueryable());

        List<DocumentClientExcelRequest> documentRequests = [];
        List<string> documentNames = [];
        cli.Setup(c => c.CreateXlsxToArchive(Capture.In(documentRequests), Capture.In(documentNames)))
            .Returns("test123");

        var mgr = new InterestCapitalizationBusinessEventManager(userId, meta, clock.Object, cli.Object,
            context.Object);

        var changed = mgr.RunInterestCapitalizationAllAccounts(true, null);

        Assert.AreEqual(4, changed);
        Assert.AreEqual(4, documentRequests.Count);
        Assert.AreEqual(4, documentNames.Count);

        for (var i = 0; i < 4; i++)
        {
            var name = documentNames[i];
            var request = documentRequests[i];
            Assert.AreEqual($"MonthlyInterestCapitalizationCalculationDetails_{i + 1}_2019-12.xlsx", name);
            
            // TODO: This needs something to validate, i.e. it also needs to be filled with info
        }
    }

    private static void SetupClientConfigMock()
    {
        var cfgMock = new Mock<IClientConfiguration>();

        var country = new ClientConfiguration.CountrySetup
        {
            BaseCurrency = "EUR",
            BaseCountry = "FI",
            BaseFormattingCulture = "fi-FI"
        };

        cfgMock.Setup(c => c.Country).Returns(country);

        NTechCache.Set("nSavings.ClientCfg", cfgMock.Object, TimeSpan.FromDays(5));
    }

    private static SavingsAccountHeader MockAccount(int accountNr, SavingsAccountTypeCode type,
        string? fixedRate = null)
    {
        return new SavingsAccountHeader
        {
            SavingsAccountNr = accountNr.ToString(),
            Status = nameof(SavingsAccountStatusCode.Active),
            AccountTypeCode = type.ToString(),
            SavingsAccountInterestCapitalizations = [],
            CreatedByEvent = new BusinessEvent
            {
                TransactionDate = new DateTime(2018, 1, 1)
            },
            FixedInterestProduct = fixedRate
        };
    }

    private static void SetupFlexInterestRates(Mock<ISavingsContext> context)
    {
        List<SharedSavingsInterestRate> interestRates =
        [
            new()
            {
                Id = 1,
                BusinessEventId = 2,
                AppliesToAccountsSinceBusinessEventId = 1,
                AccountTypeCode = nameof(SavingsAccountTypeCode.StandardAccount),
                InterestRatePercent = 3.2m,
                ValidFromDate = new DateTime(2017, 12, 11),
            }
        ];
        context.Setup(c => c.SharedSavingsInterestRatesQueryable).Returns(interestRates.AsQueryable());
    }

    private static void SetupFixedInterestRates(Mock<ISavingsContext> context)
    {
        List<FixedAccountProduct> products =
        [
            new()
            {
                Id = "fixed-rate-1",
                Name = "Fixed Rate 1",
                InterestRatePercent = 3.2m,
                TermInMonths = 12,
                ValidFrom = new DateTime(2017, 12, 11),
                Response = true,
                CreatedAtBusinessEvent = new BusinessEvent
                {
                    Id = 852
                }
            }
        ];
        context.Setup(c => c.FixedAccountProductQueryable).Returns(products.AsQueryable());
    }

    private static Mock<IClock> MockClock()
    {
        var clock = new Mock<IClock>();
        var now = new DateTime(2020, 1, 1);
        clock.Setup(c => c.Now).Returns(now);
        clock.Setup(c => c.Today).Returns(now);
        return clock;
    }
}