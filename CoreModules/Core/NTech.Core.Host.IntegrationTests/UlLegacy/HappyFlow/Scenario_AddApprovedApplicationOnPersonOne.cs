using nPreCredit;
using NTech.Core.Host.IntegrationTests.UlLegacy;
using NTech.Core.Host.IntegrationTests.UlLegacy.Utilities;
using NTech.Core.PreCredit.Database;
using NTech.Core.PreCredit.Shared.Code.PetrusOnlyScoringService;
using NTech.Core.PreCredit.Shared.Services.Utilities;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlLegacyScenarioTests
    {
        private void AddApprovedApplicationOnPersonOne(UlLegacyTestRunner.TestSupport support)
        {
            const int RequestedLoanAmount = 8000;
            const int RequestedRepaymentTimeInYears = 7;

            var applicationNr = ApplicationsLegacy.CreateApplication(support, 1, RequestedRepaymentTimeInYears, RequestedLoanAmount);
            var testPerson1CustomerId = TestPersons.GetTestPersonCustomerIdBySeed(support, 1);

            support.Context["TestPerson1_ApplicationNr"] = applicationNr;

            using var context = new PreCreditContext();
            var application = context.CreditApplicationHeaders.Single(x => x.ApplicationNr == applicationNr);
            var applicationItems = context.CreditApplicationItems.Where(x => x.ApplicationNr == applicationNr).ToList();
            Assert.That(applicationItems.Single(x => x.Name == "customerId").Value, Is.EqualTo(testPerson1CustomerId.ToString()));
            Assert.That(applicationItems.Single(x => x.Name == "amount").Value, Is.EqualTo(RequestedLoanAmount.ToString()));
            Assert.That(applicationItems.Single(x => x.Name == "repaymentTimeInYears").Value, Is.EqualTo(RequestedRepaymentTimeInYears.ToString()));

            var partialCreditApplicationModelRepository = new PartialCreditApplicationModelRepository(support.EncryptionService, support.PreCreditContextService, new LinqQueryExpanderDoNothing());
            var app = partialCreditApplicationModelRepository.Get(
                applicationNr,
                new PartialCreditApplicationModelRequest { ApplicantFields = new List<string>() { "customerId" } });
            Assert.That(app.Applicant(1).Get("customerId").IntValue.Optional, Is.EqualTo(testPerson1CustomerId));

            ///////////////////////////////////////////////
            //// Add credit decision /////////////////////
            /////////////////////////////////////////////
            ApplicationsLegacy.DoAutomaticCreditCheckOnApplication_Accept(support, applicationNr, new PetrusOnlyCreditCheckResponse.OfferModel
            {
                Amount = RequestedLoanAmount,
                RepaymentTimeInMonths = 12 * RequestedRepaymentTimeInYears,
                InitialFeeAmount = 0m,
                NotificationFeeAmount = 5m,
                MarginInterestRatePercent = 12m
            });


            using (var context2 = support.PreCreditContextService.CreateExtended())
            {
                var application2 = context2.CreditApplicationHeadersQueryable.Single(x => x.ApplicationNr == applicationNr);
                Assert.That(application2.CurrentCreditDecisionId, Is.Not.Null, "Missing credit decision");
            }
        }

        private PartialCreditReportModel CreateCreditReport(UlLegacyTestRunner.TestSupport support, int seedNr)
        {
            var testPerson = TestPersons.GetTestPersonDataBySeed(support, seedNr);
            var creditReportItems = testPerson.Keys.Select(propertyName =>
            {
                if (propertyName.StartsWith("creditreport_"))
                {
                    return new PartialCreditReportModel.Item
                    {
                        Name = propertyName.Substring("creditreport_".Length),
                        Value = testPerson[propertyName]
                    };
                }
                else if (propertyName.StartsWith("satfi_"))
                {
                    return new PartialCreditReportModel.Item
                    {
                        Name = propertyName.Substring("satfi_".Length),
                        Value = testPerson[propertyName]
                    };
                }
                else if (propertyName.IsOneOf("firstName", "lastName", "addressStreet", "addressZipcode", "addressCity", "addressCountry"))
                {
                    return new PartialCreditReportModel.Item
                    {
                        Name = propertyName,
                        Value = testPerson[propertyName]
                    };
                }
                else
                {
                    return null;
                }
            }).Where(x => x != null).ToList();

            return new PartialCreditReportModel(new List<PartialCreditReportModel.Item>(creditReportItems!));
        }
    }
}