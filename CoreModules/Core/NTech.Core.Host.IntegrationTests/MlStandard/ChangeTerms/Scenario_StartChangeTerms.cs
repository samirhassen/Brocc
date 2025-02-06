using Moq;
using nCredit;
using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using NTech.Core.Host.IntegrationTests.UlLegacy;

namespace NTech.Core.Host.IntegrationTests.MlStandard
{
    public partial class ChangeTermsTests
    {
        private static void StartChangeTerms(MlStandardTestRunner.TestSupport support, CreditHeader credit, MlNewChangeTerms newChangeTerms)
        {
            var assertSystemCommentContains = "Change terms initiated.";

            var documentRenderer = new Mock<IDocumentRenderer>(MockBehavior.Strict);
            IDictionary<string, object>? termChangePrintContext = null;
            documentRenderer.Setup(x => x.Dispose());
            documentRenderer
                .Setup(x => x.RenderDocumentToArchive("mortgageloan-change-terms", It.IsAny<IDictionary<string, object>>(), It.IsAny<string>()))
                .Returns<string, IDictionary<string, object>, string>((_, context, __) =>
                {
                    termChangePrintContext = context;
                    return "terms.pdf";
                });
            var customerClient = TestPersons.CreateRealisticCustomerClient(support);

            var mgr = support.GetRequiredService<MortgageLoansCreditTermsChangeBusinessEventManager>();

            var (isSuccess, warningMessage, newTerms) = mgr.MlStartCreditTermsChange(credit.CreditNr, newChangeTerms, () => documentRenderer.Object, customerClient.Object);

            if (termChangePrintContext != null)
            {
                Assert.That(termChangePrintContext.Opt("interestRate"), Is.EqualTo($"{(newChangeTerms.NewMarginInterestRatePercent + newChangeTerms.NewReferenceInterestRatePercent)?.ToString("N2", support.FormattingCulture)}"));
            }

            using (var context = support.CreateCreditContextFactory().CreateContext())
            {
                if (!isSuccess)
                    TestContext.WriteLine(warningMessage);

                if (isSuccess && assertSystemCommentContains != null)
                {
                    var commentText = context.CreditCommentsQueryable.SingleOrDefault(x => x.CreatedByEventId == newTerms.CreatedByEventId)?.CommentText ?? "";
                    var containsText = commentText.Contains(assertSystemCommentContains);
                    if (!containsText)
                        TestContext.WriteLine(commentText);

                    Assert.That(containsText, Is.True);
                }

                Assert.That(isSuccess, Is.True);
            }
        }
    }
}