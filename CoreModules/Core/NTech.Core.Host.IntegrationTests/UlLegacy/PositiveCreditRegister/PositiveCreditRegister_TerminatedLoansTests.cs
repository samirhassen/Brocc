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
    internal class PositiveCreditRegister_TerminatedLoansTests
    {
        [Test]
        public void PositiveCreditRegister_TerminatedLoan()
        {
            UlLegacyTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                var credit = CreditsUlLegacy.CreateCredit(support, 1);

                var syncConverter = new ServiceClientSyncConverterCore();

                var m = new UlLegacyRunMonthTester(support);
                support.MoveToNextDayOfMonth(1);

                /// Run up until debt collection
                /// todo: do this in a better way
                m.RunOneMonth();
                m.RunOneMonth();
                m.RunOneMonth();
                m.RunOneMonth();
                m.RunOneMonth();

                support.Now = new DateTime(2022, 8, 10);
                var service = support.GetRequiredService<PositiveCreditRegisterExportService>();
                var batch = SetupSendBatchObserver();

                try
                {
                    var (SuccessCount, Warnings) = service.ExportAllBatches();

                    var batchRequest = batch.FirstOrDefault();
                    var batchRequestContent = syncConverter.ToSync(() => batchRequest?.Content?.ReadAsStringAsync());

                    string expectedBatchReference = $"Batch_LoanTerminations_{support.Clock.Now:yyyyMMddHHmmss}_.{{4}}";
                    StringAssert.IsMatch(expectedBatchReference, batchRequest?.Headers.GetValues("BatchReference").FirstOrDefault());

                    Assert.That(batchRequestContent, Is.Not.Null);
                    var data = JsonConvert.DeserializeObject<TerminatedLoansRequestModel>(batchRequestContent);

                    Assert.That(data?.LoanTerminations, Is.Not.Null);

                    foreach (var loanTermination in data.LoanTerminations)
                    {
                        Assert.Multiple(() =>
                        {
                            Assert.That(loanTermination.ReportReference, Is.EqualTo(credit.CreditNr));
                            Assert.That(loanTermination.LoanNumber.Type, Is.EqualTo(LoanNumberType.Other));
                            Assert.That(loanTermination.LoanNumber.Number, Is.EqualTo(credit.CreditNr));
                            Assert.That(loanTermination.ReportType, Is.EqualTo(ReportType.NewReport));
                            Assert.That(loanTermination.Termination?.IsTerminated, Is.EqualTo(true));
                            Assert.That(loanTermination.Termination?.EndDate, Is.EqualTo(new DateTime(2022, 8, 9).ToString("yyyy-MM-dd")));
                            Assert.That(loanTermination.Termination?.IsTransferredToAnotherLender, Is.EqualTo(false));
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
