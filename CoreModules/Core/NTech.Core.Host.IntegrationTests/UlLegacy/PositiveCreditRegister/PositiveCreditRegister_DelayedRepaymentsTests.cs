using NTech.Core.Host.IntegrationTests.UlLegacy.Utilities;
using NTech.Core.Credit.Shared.Services.PositiveCreditRegister;
using NTech.Core.Module.Infrastrucutre.HttpClient;
using Newtonsoft.Json;
using NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models;
using static NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models.BaseLoanExportRequestModel;
using NTech.Core.Host.IntegrationTests.UlLegacy.Termination;
using NTech.Core.Credit.Shared.Services;

namespace NTech.Core.Host.IntegrationTests.UlLegacy.PositiveCreditRegister
{
    internal class PositiveCreditRegister_DelayedRepaymentsTests
    {
        [Test]
        public void PositiveCreditRegister_DelayedRepayment() 
        {
            UlLegacyTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                var creditAmount = 4750m;
                var paidAmount = 250m;

                var credit = CreditsUlLegacy.CreateCredit(support, 1, creditAmount: creditAmount);
                Credits.CreateAndPlaceUnplacedPayment(support, creditNr: credit.CreditNr, amount: paidAmount);

                var syncConverter = new ServiceClientSyncConverterCore();

                support.MoveToNextDayOfMonth(14);
                var notification = Credits.NotifyCredits(support);
                support.MoveToNextDayOfMonth(1);

                //60 days late
                support.MoveForwardNDays(60);

                var paymentOrderService = support.GetRequiredService<PaymentOrderService>();
                var service = support.GetRequiredService<PositiveCreditRegisterExportService>();
                var batch = SetupSendBatchObserver();

                try
                {
                    var (SuccessCount, Warnings) = service.ExportAllBatches();

                    var batchRequest = batch.FirstOrDefault();
                    var batchRequestContent = syncConverter.ToSync(() => batchRequest?.Content?.ReadAsStringAsync());

                    string expectedBatchReference = $"Batch_DelayedPayments_{support.Clock.Now:yyyyMMddHHmmss}_.{{4}}";
                    StringAssert.IsMatch(expectedBatchReference, batchRequest?.Headers.GetValues("BatchReference").FirstOrDefault());

                    Assert.That(batchRequestContent, Is.Not.Null);
                    var data = JsonConvert.DeserializeObject<DelayedRepaymentsRequestModel>(batchRequestContent);

                    Assert.That(data?.DelayedRepayments, Is.Not.Null);

                    foreach (var repayment in data.DelayedRepayments)
                    {
                        Assert.Multiple(() =>
                        {
                            Assert.That(repayment.ReportReference, Is.EqualTo($"dr_{credit.CreditNr}"));
                            Assert.That(repayment.ReportType, Is.EqualTo(ReportType.NewReport));
                            Assert.That(repayment.LoanNumber, Is.Not.Null);
                            Assert.That(repayment.LoanNumber.Number, Is.EqualTo(credit.CreditNr));
                            Assert.That(repayment.LoanNumber.Type, Is.EqualTo(LoanNumberType.Other));
                            Assert.That(repayment.IsDelay, Is.EqualTo(true));
                            Assert.That(repayment.IsForeclosed, Is.EqualTo(false));
                            Assert.That(repayment.DelayedAmounts.First().DelayedInstalment, Is.EqualTo((decimal)195.77));
                            Assert.That(repayment.DelayedAmounts.First().OriginalDueDate, Is.EqualTo(new DateTime(2022,03,28).ToString("yyyy-MM-dd")));
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
        public void PositiveCreditRegister_DelayedRepaymentThenPaysTest() 
        {
            UlLegacyTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                var credit = CreditsUlLegacy.CreateCredit(support, 1, creditAmount: 4750m);
                Credits.CreateAndPlaceUnplacedPayment(support, creditNr: credit.CreditNr, amount: 250m);

                var personData = TestPersons.GetTestPersonDataBySeed(support, 1);
                var customerClient = TestPersons.CreateRealisticCustomerClient(support);
                var syncConverter = new ServiceClientSyncConverterCore();
                var settings = support.CreditEnvSettings.PositiveCreditRegisterSettings;
                var m = new UlLegacyRunMonthTester(support);

                support.MoveToNextDayOfMonth(14);
                var notification = Credits.NotifyCredits(support);
                support.MoveToNextDayOfMonth(1);

                //60 days late 
                support.MoveForwardNDays(61);

                //Then pays
                Credits.PayOverdueNotifications(support);
                support.MoveForwardNDays(1);

                var paymentOrderService = support.GetRequiredService<PaymentOrderService>();
                var service = support.GetRequiredService<PositiveCreditRegisterExportService>();
                var batch = SetupSendBatchObserver();

                try
                {
                    var (SuccessCount, Warnings) = service.ExportAllBatches();

                    var batchRequest = batch.ElementAt(1); 
                    var batchRequestContent = syncConverter.ToSync(() => batchRequest?.Content?.ReadAsStringAsync());

                    string expectedBatchReference = $"Batch_DelayedPayments_{support.Clock.Now:yyyyMMddHHmmss}_.{{4}}";
                    StringAssert.IsMatch(expectedBatchReference, batchRequest?.Headers.GetValues("BatchReference").FirstOrDefault());

                    Assert.That(batchRequestContent, Is.Not.Null);
                    var data = JsonConvert.DeserializeObject<DelayedRepaymentsRequestModel>(batchRequestContent);

                    Assert.That(data?.DelayedRepayments, Is.Not.Null);

                    foreach (var repayment in data.DelayedRepayments)
                    {
                        Assert.Multiple(() =>
                        {
                            Assert.That(repayment.ReportReference, Is.EqualTo($"drf_{credit.CreditNr}"));
                            Assert.That(repayment.ReportType, Is.EqualTo(ReportType.NewReport));
                            Assert.That(repayment.LoanNumber, Is.Not.Null);
                            Assert.That(repayment.LoanNumber.Number, Is.EqualTo(credit.CreditNr));
                            Assert.That(repayment.LoanNumber.Type, Is.EqualTo(LoanNumberType.Other));
                            Assert.That(repayment.IsDelay, Is.EqualTo(false));
                            Assert.That(repayment.IsForeclosed, Is.EqualTo(false));
                            Assert.That(repayment.DelayedAmounts, Is.Null);
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
        public void PositiveCreditRegister_DelayedRepaymentThenPaysWithMoreUnpadiInvoicesTest()
        {
            UlLegacyTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                var credit = CreditsUlLegacy.CreateCredit(support, 1, creditAmount: 4750m);
                Credits.CreateAndPlaceUnplacedPayment(support, creditNr: credit.CreditNr, amount: 250m);

                var personData = TestPersons.GetTestPersonDataBySeed(support, 1);
                var customerClient = TestPersons.CreateRealisticCustomerClient(support);
                var syncConverter = new ServiceClientSyncConverterCore();
                var settings = support.CreditEnvSettings.PositiveCreditRegisterSettings;
                var m = new UlLegacyRunMonthTester(support);

                support.MoveToNextDayOfMonth(14);
                var notification = Credits.NotifyCredits(support);


                support.MoveToNextDayOfMonth(14);
                var notification2 = Credits.NotifyCredits(support);

                
                support.MoveToNextDayOfMonth(1);

                //60 days late 
                support.MoveForwardNDays(51);

                //Then pays first notification
                Credits.CreateAndPlaceUnplacedPayment(support, creditNr: credit.CreditNr, amount: 250m);
                support.MoveForwardNDays(1);

                var paymentOrderService = support.GetRequiredService<PaymentOrderService>();
                var service = support.GetRequiredService<PositiveCreditRegisterExportService>();
                var batch = SetupSendBatchObserver();

                try
                {
                    var (SuccessCount, Warnings) = service.ExportAllBatches();

                    var batchRequest = batch.ElementAt(1);
                    var batchRequestContent = syncConverter.ToSync(() => batchRequest?.Content?.ReadAsStringAsync());

                    string expectedBatchReference = $"Batch_DelayedPayments_{support.Clock.Now:yyyyMMddHHmmss}_.{{4}}";
                    StringAssert.IsMatch(expectedBatchReference, batchRequest?.Headers.GetValues("BatchReference").FirstOrDefault());

                    Assert.That(batchRequestContent, Is.Not.Null);
                    var data = JsonConvert.DeserializeObject<DelayedRepaymentsRequestModel>(batchRequestContent);

                    Assert.That(data?.DelayedRepayments, Is.Not.Null);

                    foreach (var repayment in data.DelayedRepayments)
                    {
                        Assert.Multiple(() =>
                        {
                            Assert.That(repayment.ReportReference, Is.EqualTo($"drf_{credit.CreditNr}"));
                            Assert.That(repayment.ReportType, Is.EqualTo(ReportType.NewReport));
                            Assert.That(repayment.LoanNumber, Is.Not.Null);
                            Assert.That(repayment.LoanNumber.Number, Is.EqualTo(credit.CreditNr));
                            Assert.That(repayment.LoanNumber.Type, Is.EqualTo(LoanNumberType.Other));
                            Assert.That(repayment.IsDelay, Is.EqualTo(false));
                            Assert.That(repayment.IsForeclosed, Is.EqualTo(false));
                            Assert.That(repayment.DelayedAmounts, Is.Null);
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
