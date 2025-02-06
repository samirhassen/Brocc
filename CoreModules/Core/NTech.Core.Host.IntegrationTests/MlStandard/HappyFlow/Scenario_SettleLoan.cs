using Moq;
using nCredit;
using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using nCredit.DomainModel;
using nCredit.Excel;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Host.IntegrationTests.MlStandard.Utilities;
using NTech.Core.Host.IntegrationTests.UlLegacy;
using NTech.Core.Module.Shared.Clients;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class MlStandardLikeScenarioTests
    {
        private void SettleLoan(MlStandardTestRunner.TestSupport support)
        {
            DoAfterDay(support, 25, () =>
            {

                var credit = CreditsMlStandard.GetCreatedCredit(support, 1);

                CreditDomainModel LoadCredit()
                {
                    using (var context = support.CreateCreditContextFactory().CreateContext())
                    {
                        return CreditDomainModel.PreFetchForSingleCredit(credit!.CreditNr, context, support.CreditEnvSettings);
                    }
                }

                CreditDomainModel creditModel = LoadCredit();

                var rseService = new SwedishMortgageLoanRseService(support.CreateCreditContextFactory(), support.GetNotificationProcessSettingsFactory(),
                    support.Clock, support.ClientConfiguration, support.CreditEnvSettings);
                var settlementMgr = support.GetRequiredService<CreditSettlementSuggestionBusinessEventManager>();
                var comparisonInterestRatePercent = creditModel.GetInterestRatePercent(support.Clock.Today) + 10m;
                var rseResult = rseService.CalculateRseForCredit(new RseForCreditRequest
                {
                    ComparisonInterestRatePercent = comparisonInterestRatePercent,
                    CreditNr = credit.CreditNr
                });

                settlementMgr.TryCreateAndSendSettlementSuggestion(credit.CreditNr,
                    support.Clock.Today, rseResult.Rse?.RseAmount, comparisonInterestRatePercent, out var _, out var offer, null);

                Assert.That(offer?.settlementAmount, Is.GreaterThan(0m));

                Credits.CreateAndPlaceUnplacedPayment(support, credit.CreditNr, offer.settlementAmount);
                Credits.AssertIsSettled(support, credit.CreditNr);

                creditModel = LoadCredit();
                Assert.That(creditModel.GetStatus(), Is.EqualTo(CreditStatus.Settled));

                support.Now = new DateTime(support.Now.Year + 1, 1, 1);

                var documentClient = new Mock<IDocumentClient>(MockBehavior.Strict);
                documentClient.Setup(x => x.BatchRenderBegin(It.IsAny<byte[]>())).Returns("b1");
                documentClient.Setup(x => x.BatchRenderDocumentToArchive("b1", It.IsAny<string>(), It.IsAny<IDictionary<string, object>>())).Returns(() => "someKey");
                documentClient.Setup(x => x.BatchRenderEnd("b1"));
                documentClient.Setup(x => x.TryFetchRaw(It.IsAny<string>()))
                    .Returns((IsSuccess: true, ContentType: "application/pdf", FileName: "somepdf.pdf", FileData: new byte[] { 1, 2, 3 }));
                documentClient.Setup(x => x.CreateXlsx(It.IsAny<DocumentClientExcelRequest>())).Returns(new MemoryStream(new byte[] { 1, 2, 3 }));
                documentClient.Setup(x => x.ArchiveStoreFile(It.IsAny<FileInfo>(), It.IsAny<string>(), It.IsAny<string>())).Returns("someKey");

                var annualStatementService = new LoanStandardAnnualSummaryService(support.CreateCreditContextFactory(), support.ClientConfiguration, documentClient.Object,
                    TestPersons.CreateRealisticCustomerClient(support).Object, _ => new byte[] { }, support.CreditEnvSettings);
                annualStatementService.CreateAndPossiblyExportAnnualStatementsForYear(support.Now.Year - 1, null);

                using (var context = support.CreateCreditContextFactory().CreateContext())
                {
                    var statementCount = context.CreditAnnualStatementHeadersQueryable.Count(x => x.CustomerId == credit.CreditCustomers.First().CustomerId);
                    Assert.That(statementCount, Is.EqualTo(1));
                }
            });
        }
    }
}