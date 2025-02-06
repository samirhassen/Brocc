using NTech.Core.Host.IntegrationTests.UlLegacy.Utilities;
using NTech.Core.Credit.Shared.Services.PositiveCreditRegister;
using NTech.Core.Module.Infrastrucutre.HttpClient;
using Newtonsoft.Json;
using NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models;
using static NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models.BaseLoanExportRequestModel;
using nCredit.DomainModel;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Credit.Shared.Services;

namespace NTech.Core.Host.IntegrationTests.UlLegacy.PositiveCreditRegister
{
    internal class PositiveCreditRegister_LoanRepaymentsTests
    {
        [Test]
        public void PositiveCreditRegister_LoanRepayment()
        {
            UlLegacyTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                var creditAmount = 4750m;
                var paidAmount = 250m;

                var credit = CreditsUlLegacy.CreateCredit(support, 1, creditAmount: creditAmount);
                var paymentDate = support.Now.UtcDateTime;
                Credits.CreateAndPlaceUnplacedPayment(support, creditNr: credit.CreditNr, amount: paidAmount);

                var personData = TestPersons.GetTestPersonDataBySeed(support, 1);
                var customerClient = TestPersons.CreateRealisticCustomerClient(support);
                var syncConverter = new ServiceClientSyncConverterCore();
                var settings = support.CreditEnvSettings.PositiveCreditRegisterSettings;

                var service = support.GetRequiredService<PositiveCreditRegisterExportService>();

                support.MoveForwardNDays(1);

                var batch = SetupSendBatchObserver();

                try
                {
                    var (SuccessCount, Warnings) = service.ExportAllBatches();

                    var batchRequest = batch.ElementAt(1);
                    var batchRequestContent = syncConverter.ToSync(() => batchRequest?.Content?.ReadAsStringAsync());

                    string expectedBatchReference = $"Batch_LoanRepayments_{support.Clock.Now:yyyyMMddHHmmss}_.{{4}}";
                    StringAssert.IsMatch(expectedBatchReference, batchRequest?.Headers.GetValues("BatchReference").FirstOrDefault());

                    Assert.That(batchRequestContent, Is.Not.Null);
                    var data = JsonConvert.DeserializeObject<LoanRepaymentsRequestModel>(batchRequestContent);

                    Assert.That(data?.Repayments, Is.Not.Null);

                    foreach (var repayment in data.Repayments)
                    {
                        Assert.Multiple(() =>
                        {
                            Assert.That(repayment.ReportCreationTimeUtc, Is.EqualTo(support.Now.UtcDateTime));
                            Assert.That(repayment.ReportType, Is.EqualTo(ReportType.NewReport));
                            Assert.That(repayment.ReportReference, Is.EqualTo("5"));
                            Assert.That(repayment.LoanNumber, Is.Not.Null);
                            Assert.That(repayment.LoanNumber.Number, Is.EqualTo(credit.CreditNr));
                            Assert.That(repayment.LoanNumber.Type, Is.EqualTo(LoanNumberType.Other));
                            Assert.That(repayment.LoanType, Is.EqualTo(LoanType.LumpSumLoan));
                            Assert.That(repayment.LumpSumLoanRepayment, Is.Not.Null);
                            Assert.That(repayment.LumpSumLoanRepayment.AmortizationPaid, Is.EqualTo(paidAmount));
                            Assert.That(repayment.LumpSumLoanRepayment.InterestPaid, Is.EqualTo(0m));
                            Assert.That(repayment.LumpSumLoanRepayment.OtherExpenses, Is.EqualTo(0m));
                            Assert.That(repayment.LumpSumLoanRepayment.PaymentDate, Is.EqualTo(paymentDate.ToString("yyyy-MM-dd")));
                            Assert.That(repayment.LumpSumLoanRepayment.Balance, Is.EqualTo(creditAmount - paidAmount));
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
        public void PositiveCreditRegister_LoanRepayment2()
        {
            UlLegacyTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                var creditAmount = 4750m;
                var credit = CreditsUlLegacy.CreateCredit(support, 1, creditAmount: creditAmount);

                decimal notificationCapitalAmount;
                decimal notificationInterestAmount;
                decimal notificationFeeAmount;
                decimal notificationReminderFeeAmount;
                decimal balance;
                DateTime paymentDateUtc;
                {
                    support.Now = support.Now.AddMonths(1);

                    //Fully pay one notification to get some fee/interest revenue
                    support.MoveToNextDayOfMonth(14);
                    Credits.NotifyCredits(support, (credit.CreditNr, credit.CreditCustomers.Single(x => x.ApplicantNr == 1).CustomerId, NotificationExpectedResultCode.NotificationCreated));
                    using (var context = support.CreateCreditContextFactory().CreateContext())
                    {
                        var notification = CreditNotificationDomainModel.CreateForCredit(credit.CreditNr, context, support.PaymentOrder(), onlyFetchOpen: false).Values.Single();
                        notificationInterestAmount = notification.GetRemainingBalance(support.Clock.Today, CreditDomainModel.AmountType.Interest);
                        notificationCapitalAmount = notification.GetRemainingBalance(support.Clock.Today, CreditDomainModel.AmountType.Capital);
                        notificationFeeAmount = notification.GetRemainingBalance(support.Clock.Today, CreditDomainModel.AmountType.NotificationFee);
                        notificationReminderFeeAmount = notification.GetRemainingBalance(support.Clock.Today, CreditDomainModel.AmountType.ReminderFee);

                        paymentDateUtc = support.Now.UtcDateTime;
                        Credits.CreateAndImportPaymentFile(support, new Dictionary<string, decimal>
                        {
                            { credit.CreditNr, notification.GetRemainingBalance(support.Clock.Today) }
                        });

                        var creditInfo = CreditDomainModel.PreFetchForSingleCredit(credit.CreditNr, context, support.CreditEnvSettings);
                        balance = creditInfo.GetNotNotifiedCapitalBalance(support.Clock.Today);
                    }
                }

                support.MoveForwardNDays(1);


                var syncConverter = new ServiceClientSyncConverterCore();
                var service = support.GetRequiredService<PositiveCreditRegisterExportService>();
                var batch = SetupSendBatchObserver();
                try
                {
                    var (SuccessCount, Warnings) = service.ExportAllBatches();

                    var batchRequest = batch.FirstOrDefault();
                    var batchRequestContent = syncConverter.ToSync(() => batchRequest?.Content?.ReadAsStringAsync());

                    string expectedBatchReference = $"Batch_LoanRepayments_{support.Now:yyyyMMddHHmmss}_.{{4}}";
                    StringAssert.IsMatch(expectedBatchReference, batchRequest?.Headers.GetValues("BatchReference").FirstOrDefault());

                    Assert.That(batchRequestContent, Is.Not.Null);
                    var data = JsonConvert.DeserializeObject<LoanRepaymentsRequestModel>(batchRequestContent);

                    Assert.That(data?.Repayments, Is.Not.Null);

                    foreach (var repayment in data.Repayments)
                    {
                        Assert.Multiple(() =>
                        {
                            Assert.That(repayment.ReportCreationTimeUtc, Is.EqualTo(support.Now.UtcDateTime));
                            Assert.That(repayment.ReportType, Is.EqualTo(ReportType.NewReport));
                            Assert.That(repayment.ReportReference, Is.EqualTo("7"));
                            Assert.That(repayment.LoanNumber, Is.Not.Null);
                            Assert.That(repayment.LoanNumber.Number, Is.EqualTo(credit.CreditNr));
                            Assert.That(repayment.LoanNumber.Type, Is.EqualTo(LoanNumberType.Other));
                            Assert.That(repayment.LoanType, Is.EqualTo(LoanType.LumpSumLoan));
                            Assert.That(repayment.LumpSumLoanRepayment, Is.Not.Null);
                            Assert.That(repayment.LumpSumLoanRepayment.AmortizationPaid, Is.EqualTo(notificationCapitalAmount));
                            Assert.That(repayment.LumpSumLoanRepayment.InterestPaid, Is.EqualTo(notificationInterestAmount));
                            Assert.That(repayment.LumpSumLoanRepayment.OtherExpenses, Is.EqualTo(notificationFeeAmount + notificationReminderFeeAmount));
                            Assert.That(repayment.LumpSumLoanRepayment.PaymentDate, Is.EqualTo(paymentDateUtc.ToString("yyyy-MM-dd")));
                            Assert.That(repayment.LumpSumLoanRepayment.Balance, Is.EqualTo(balance));
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
