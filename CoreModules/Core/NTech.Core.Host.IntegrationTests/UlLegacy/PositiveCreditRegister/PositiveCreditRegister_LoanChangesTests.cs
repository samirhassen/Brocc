using NTech.Core.Host.IntegrationTests.UlLegacy.Utilities;
using NTech.Core.Credit.Shared.Services.PositiveCreditRegister;
using NTech.Core.Module.Infrastrucutre.HttpClient;
using static NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models.NewOrChangedLoansRequestModel;
using Newtonsoft.Json;
using static NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models.BaseLoanExportRequestModel;
using NTech.Core.Host.IntegrationTests.UlLegacy.Termination;
using nCredit.Code.Services;
using NTech.Core.Credit.Shared.Services;

namespace NTech.Core.Host.IntegrationTests.UlLegacy.PositiveCreditRegister
{
    internal class PositiveCreditRegister_LoanChangesTests
    {
        [Test]
        public void PositiveCreditRegister_LoanChanges_ChangedTerms()
        {
            UlLegacyTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                var referenceInterestRatePercent = 1.30m;
                var marginInterestRatePercent = 3.20m;
                var credit = CreditsUlLegacy.CreateCredit(support, 1, creditAmount: 1000m, referenceInterestRatePercent: referenceInterestRatePercent, marginInterestRatePercent: marginInterestRatePercent, capitalizedInitialFeeAmount: 100m);
                var personData = TestPersons.GetTestPersonDataBySeed(support, 1);
                var customerClient = TestPersons.CreateRealisticCustomerClient(support);
                var syncConverter = new ServiceClientSyncConverterCore();
                var settings = support.CreditEnvSettings.PositiveCreditRegisterSettings;

                var service = support.GetRequiredService<PositiveCreditRegisterExportService>();

                var m = new UlLegacyRunMonthTester(support);
                m.RunOneMonth(doBeforeDay: dayNr => { });

                var newMarginInterestRate = 12m;
                var creditTermsChangeHeader = CreditsUlLegacy.StartChangeTerms(support, credit.CreditNr, 20, newMarginInterestRate, isEmailProviderDown: true);
                #pragma warning disable CS8602 // Dereference of a possibly null reference.
                var tryAccept = CreditsUlLegacy.TryAcceptCreditTermsChange(support, creditTermsChangeHeader.Id);
                #pragma warning restore CS8602

                support.MoveForwardNDays(1);

                var batch = SetupSendBatchObserver();
                try
                {
                    var (SuccessCount, Warnings) = service.ExportAllBatches();

                    var batchRequest = batch.FirstOrDefault();
                    var batchRequestContent = syncConverter.ToSync(() => batchRequest?.Content?.ReadAsStringAsync());

                    Assert.That(batchRequestContent, Is.Not.Null);
                    var data = JsonConvert.DeserializeObject<NewLoanReport>(batchRequestContent);

                    string expectedBatchReference = $"Batch_LoanChanges_{support.Clock.Now:yyyyMMddHHmmss}_.{{4}}";
                    StringAssert.IsMatch(expectedBatchReference, batchRequest?.Headers.GetValues("BatchReference").FirstOrDefault());

                    Assert.That(data?.Loans, Is.Not.Null);

                    foreach (var loan in data.Loans)
                    {
                        Assert.Multiple(() =>
                        {
                            Assert.That(loan, Is.Not.Null);
                            Assert.That(loan.ReportReference, Is.EqualTo(credit.CreditNr));
                            Assert.That(loan.LoanNumber.Type, Is.EqualTo(LoanNumberType.Other));
                            Assert.That(loan.LoanNumber.Number, Is.EqualTo(credit.CreditNr));
                            Assert.That(loan.LenderMarketingName, Is.EqualTo(settings.LenderMarketingName));
                            Assert.That(loan.ContractDate, Is.EqualTo(credit.CreatedByEvent.EventDate.DateTime.ToString("yyyy-MM-dd")));
                            Assert.That(loan.IsPeerToPeerLoanBroker, Is.EqualTo(false));
                            Assert.That(loan.LoanType, Is.EqualTo(LoanType.LumpSumLoan));
                            Assert.That(loan.BorrowersCount, Is.EqualTo(credit.NrOfApplicants));
                            Assert.That(loan.OneTimeServiceFees, Is.EqualTo(100m));
                            Assert.That(loan.IsLoanWithCollateral, Is.EqualTo(false));
                        });

                        //Borrowers
                        Assert.That(loan.Borrowers, Is.Not.Null);
                        foreach (var borrower in loan.Borrowers)
                        {
                            Assert.Multiple(() =>
                            {
                                Assert.That(borrower, Is.Not.Null);
                                Assert.That(borrower.IdCodeType, Is.EqualTo(IdCodeType.PersonalIdentityCode));
                                Assert.That(borrower.IdCode, Is.EqualTo(personData["civicRegNr"]));
                            });
                        }

                        //LumpSumLoan
                        Assert.Multiple(() =>
                        {
                            Assert.That(loan.LumpSumLoan, Is.Not.Null);
                            Assert.That(loan.LumpSumLoan.AmountIssued, Is.EqualTo(1000m));
                            Assert.That(loan.LumpSumLoan.AmountPaid, Is.EqualTo(0m));
                            Assert.That(loan.LumpSumLoan.Balance, Is.EqualTo(1100m));
                            Assert.That(loan.LumpSumLoan.AmountIssued, Is.GreaterThanOrEqualTo(loan.LumpSumLoan.AmountPaid));
                            Assert.That(loan.LumpSumLoan.AmortizationFrequency, Is.EqualTo(1));
                            Assert.That(loan.LumpSumLoan.PurposeOfUse, Is.EqualTo(LoanPurposeOfUse.OtherConsumerCredit));
                            Assert.That(loan.LumpSumLoan.RepaymentMethod, Is.EqualTo(RepaymentMethod.Annuities));
                        });
                        
                        //Interest
                        Assert.Multiple(() =>
                        {
                            Assert.That(loan.Interest, Is.Not.Null);
                            Assert.That(loan.Interest.TotalInterestRatePct, Is.EqualTo(referenceInterestRatePercent + newMarginInterestRate));
                            Assert.That(loan.Interest.MarginPct, Is.EqualTo(newMarginInterestRate));
                            Assert.That(loan.Interest.InterestType, Is.EqualTo(InterestType.Euribor));
                            Assert.That(loan.Interest.InterestDeterminationPeriod, Is.EqualTo(3));
                        });

                        //ConsumerCredit
                        Assert.Multiple(() =>
                        {
                            Assert.That(loan.ConsumerCredit, Is.Not.Null);
                            Assert.That(loan.ConsumerCredit.LoanConsumerProtectionAct, Is.EqualTo(LoanConsumerProtectionAct.ConsumerCredit));
                            Assert.That(loan.ConsumerCredit.IsGoodsOrServicesRelatedCredit, Is.EqualTo(false));
                        });
                    }
                }

                finally
                {
                    TeardownSendBatchObserver();
                }
            });
        }
        
        [Test]
        public void PositiveCreditRegister_LoanChanges_ChangedReferenceInterest()
        {
            UlLegacyTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                var referenceInterestRatePercent = 1.30m;
                var marginInterestRatePercent = 3.20m; 
                var credit = CreditsUlLegacy.CreateCredit(support, 1, creditAmount: 1000m, referenceInterestRatePercent: referenceInterestRatePercent, marginInterestRatePercent: marginInterestRatePercent, capitalizedInitialFeeAmount: 100m);
                var personData = TestPersons.GetTestPersonDataBySeed(support, 1);
                var syncConverter = new ServiceClientSyncConverterCore();
                var settings = support.CreditEnvSettings.PositiveCreditRegisterSettings;

                var service = support.GetRequiredService<PositiveCreditRegisterExportService>();

                var m = new UlLegacyRunMonthTester(support);
                m.RunOneMonth(doBeforeDay: dayNr => { });

                //Changed reference interest rate
                var changedReferenceRate = 5.0m;

                var referenceInterestChangeService = new ReferenceInterestChangeService(x => x, new KeyValueStoreService(() => support.CreateCreditContextFactory().CreateContext()),
                           support.Clock, new LegalInterestCeilingService(support.CreditEnvSettings), support.CreditEnvSettings,
                           support.ClientConfiguration, support.CreateCreditContextFactory());
                var changeModel = referenceInterestChangeService.BeginChangeReferenceInterest(changedReferenceRate, support.CurrentUser);
                referenceInterestChangeService.TryChangeReferenceInterest(changeModel, support.CurrentUser, out var failedMessage);

                support.MoveForwardNDays(1);

                var batch = SetupSendBatchObserver();
                try
                {
                    var (SuccessCount, Warnings) = service.ExportAllBatches();

                    var batchRequest = batch.FirstOrDefault();
                    var batchRequestContent = syncConverter.ToSync(() => batchRequest?.Content?.ReadAsStringAsync());

                    Assert.That(batchRequestContent, Is.Not.Null);
                    var data = JsonConvert.DeserializeObject<NewLoanReport>(batchRequestContent);

                    string expectedBatchReference = $"Batch_LoanChanges_{support.Clock.Now:yyyyMMddHHmmss}_.{{4}}";
                    StringAssert.IsMatch(expectedBatchReference, batchRequest?.Headers.GetValues("BatchReference").FirstOrDefault());

                    Assert.That(data?.Loans, Is.Not.Null);

                    foreach (var loan in data.Loans)
                    {
                        Assert.Multiple(() =>
                        {
                            Assert.That(loan, Is.Not.Null);
                            Assert.That(loan.ReportReference, Is.EqualTo(credit.CreditNr));
                            Assert.That(loan.LoanNumber.Type, Is.EqualTo(LoanNumberType.Other));
                            Assert.That(loan.LoanNumber.Number, Is.EqualTo(credit.CreditNr));
                            Assert.That(loan.LenderMarketingName, Is.EqualTo(settings.LenderMarketingName));
                            Assert.That(loan.ContractDate, Is.EqualTo(credit.CreatedByEvent.EventDate.DateTime.ToString("yyyy-MM-dd")));
                            Assert.That(loan.IsPeerToPeerLoanBroker, Is.EqualTo(false));
                            Assert.That(loan.LoanType, Is.EqualTo(LoanType.LumpSumLoan));
                            Assert.That(loan.BorrowersCount, Is.EqualTo(credit.NrOfApplicants));
                            Assert.That(loan.OneTimeServiceFees, Is.EqualTo(100m));
                            Assert.That(loan.IsLoanWithCollateral, Is.EqualTo(false));
                        });

                        //Borrowers
                        Assert.That(loan.Borrowers, Is.Not.Null);
                        foreach (var borrower in loan.Borrowers)
                        {
                            Assert.Multiple(() =>
                            {
                                Assert.That(borrower, Is.Not.Null);
                                Assert.That(borrower.IdCodeType, Is.EqualTo(IdCodeType.PersonalIdentityCode));
                                Assert.That(borrower.IdCode, Is.EqualTo(personData["civicRegNr"]));
                            });
                        }

                        //LumpSumLoan
                        Assert.Multiple(() =>
                        {
                            Assert.That(loan.LumpSumLoan, Is.Not.Null);
                            Assert.That(loan.LumpSumLoan.AmountIssued, Is.EqualTo(1000m));
                            Assert.That(loan.LumpSumLoan.AmountPaid, Is.EqualTo(0m));
                            Assert.That(loan.LumpSumLoan.Balance, Is.EqualTo(1100m));
                            Assert.That(loan.LumpSumLoan.AmountIssued, Is.GreaterThanOrEqualTo(loan.LumpSumLoan.AmountPaid));
                            Assert.That(loan.LumpSumLoan.AmortizationFrequency, Is.EqualTo(1));
                            Assert.That(loan.LumpSumLoan.PurposeOfUse, Is.EqualTo(LoanPurposeOfUse.OtherConsumerCredit));
                            Assert.That(loan.LumpSumLoan.RepaymentMethod, Is.EqualTo(RepaymentMethod.Annuities));
                        });

                        //Interest
                        Assert.Multiple(() =>
                        {
                            Assert.That(loan.Interest, Is.Not.Null);
                            Assert.That(loan.Interest.TotalInterestRatePct, Is.EqualTo(changedReferenceRate + marginInterestRatePercent));
                            Assert.That(loan.Interest.MarginPct, Is.EqualTo(marginInterestRatePercent));
                            Assert.That(loan.Interest.InterestType, Is.EqualTo(InterestType.Euribor));
                            Assert.That(loan.Interest.InterestDeterminationPeriod, Is.EqualTo(3));
                        });

                        //ConsumerCredit
                        Assert.Multiple(() =>
                        {
                            Assert.That(loan.ConsumerCredit, Is.Not.Null);
                            Assert.That(loan.ConsumerCredit.LoanConsumerProtectionAct, Is.EqualTo(LoanConsumerProtectionAct.ConsumerCredit));
                            Assert.That(loan.ConsumerCredit.IsGoodsOrServicesRelatedCredit, Is.EqualTo(false));
                        });
                    }
                }

                finally
                {
                    TeardownSendBatchObserver();
                }
            });
        }

        private static List<HttpRequestMessage> SetupSendBatchObserver()
        {
            var batches = new List<HttpRequestMessage>();
            void observeSendBatch(HttpRequestMessage request) => batches.Add(request);
            PositiveCreditRegisterExportService.ObserveSendBatch = observeSendBatch;
            return batches;
        }

        private static void TeardownSendBatchObserver() => PositiveCreditRegisterExportService.ObserveSendBatch = null;
    }
}
